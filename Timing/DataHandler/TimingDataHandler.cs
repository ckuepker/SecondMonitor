﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecondMonitor.PluginManager.Core;
using SecondMonitor.DataModel;
using SecondMonitor.PluginManager.GameConnector;
using SecondMonitor.Timing.Model;
using SecondMonitor.Timing.GUI;
using SecondMonitor.Timing.Model.Drivers;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SecondMonitor.Timing.DataHandler.Commands;

namespace SecondMonitor.Timing.DataHandler
{

    public class TimingDataHandler : ISecondMonitorPlugin, INotifyPropertyChanged
    {
        private enum ResetModeEnum {  NO_RESET, MANUAL, AUTOMATIC}
        private TimingGUI gui;
        private PluginsManager pluginsManager;
        private SessionTiming timing;
        private SessionInfo.SessionTypeEnum sessionType = SessionInfo.SessionTypeEnum.NA;
        private int paceLaps = 3;
        private bool scrollToPlayer = true;
        ResetModeEnum shouldReset = ResetModeEnum.NO_RESET;
        public event PropertyChangedEventHandler PropertyChanged;


        private DateTime lastRefreshTiming;
        private DateTime lastRefreshCarInfo;

        // Gets or sets the CollectionViewSource
        public CollectionViewSource ViewSource { get; set; }    

        // Gets or sets the ObservableCollection
        public ObservableCollection<Driver> Collection { get; set; }

        private LapInfo bestSessionLap;
        public string BestLapFormatted { get => bestSessionLap != null ? bestSessionLap.Driver.DriverInfo.DriverName +"-"+ Driver.FormatTimeSpan(bestSessionLap.LapTime) : "Best Session Lap"; }

        public PluginsManager PluginManager
        {
            get => pluginsManager;
            set
            {
                pluginsManager = value;
                pluginsManager.DataLoaded += OnDataLoaded;
                pluginsManager.SessionStarted += OnSessionStarted;
            }
        }


        public bool IsDaemon => false;

        public void RunPlugin()
        {
            gui = new TimingGUI();
            gui.Show();
            gui.upDownPaceLaps.Value = paceLaps;
            gui.Closed += Gui_Closed;
            gui.btnReset.DataContext = this;
            gui.btnResetFuel.DataContext = this;
            gui.rbtAbsolute.DataContext = this;
            gui.rbtAutomatic.DataContext = this;
            gui.rbtRelative.DataContext = this;
            gui.upDownPaceLaps.DataContext = this;
            gui.chkScrollToPlayer.DataContext = this;
            gui.chkScrollToPlayer.IsChecked = scrollToPlayer;
            lastRefreshTiming = DateTime.Now;
            lastRefreshCarInfo = DateTime.Now;
            shouldReset = ResetModeEnum.NO_RESET;
        }

        private NoArgumentCommand _resetCommand;
        public NoArgumentCommand ResetCommand
        {
            get
            {
                if(_resetCommand==null)
                {
                    _resetCommand = new NoArgumentCommand(ScheduleReset);
                }
                return _resetCommand;
            }
        }

        private NoArgumentCommand _resetFuelCommand;
        public NoArgumentCommand ResetFuelCommand
        {
            get
            {
                if (_resetFuelCommand == null)
                {
                    _resetFuelCommand = new NoArgumentCommand(ResetFuel);
                }
                return _resetFuelCommand;
            }
        }

        private NoArgumentCommand _paceLapsCommand;
        public NoArgumentCommand PaceLapsCommand
        {
            get
            {
                if (_paceLapsCommand == null)
                {
                    _paceLapsCommand = new NoArgumentCommand(PaceLapsChanged);
                }
                return _paceLapsCommand;
            }
        }

        private NoArgumentCommand _scrollToPlayerCommand;
        public NoArgumentCommand ScrollToPlayerCommand
        {
            get
            {
                if (_scrollToPlayerCommand == null)
                {
                    _scrollToPlayerCommand = new NoArgumentCommand(ScrollToPlayerChanged);
                }
                return _scrollToPlayerCommand;
            }
        }

        private TimingModeChangedCommand _timingModeChangedCommand;
        public TimingModeChangedCommand TimingModeChangedCommand
        {
            get
            {
                if(_timingModeChangedCommand == null)
                {
                    _timingModeChangedCommand = new TimingModeChangedCommand(ChangeTimingMode, () => {
                        return ViewSource != null;
                    });
                }
                return _timingModeChangedCommand;
            }
        }


        private void ScrollToPlayerChanged()
        {
            scrollToPlayer = (bool)gui.chkScrollToPlayer.IsChecked;
        }

        private void PaceLapsChanged()
        {
            paceLaps = (int)gui.upDownPaceLaps.Value;
            if (timing != null)
                timing.PaceLaps = paceLaps;

        }
        private void ResetFuel()
        {
            gui.fuelMonitor.ResetFuelMonitor();
        }
        private void ScheduleReset()
        {
            shouldReset = ResetModeEnum.MANUAL;
        }

        private void ChangeTimingMode()
        {
            gui.Dispatcher.Invoke(() =>
            {
                var mode = gui.TimingMode;
                if (mode == TimingGUI.TimingModeOptions.Absolute || (mode == TimingGUI.TimingModeOptions.Automatic && timing.SessionType != SessionInfo.SessionTypeEnum.Race))
                {
                    ViewSource.SortDescriptions.Clear();
                    ViewSource.SortDescriptions.Add(new SortDescription("Position", ListSortDirection.Ascending));
                }
                else
                {
                    ViewSource.SortDescriptions.Clear();
                    ViewSource.SortDescriptions.Add(new SortDescription("DistanceToPlayer", ListSortDirection.Ascending));
                }
            });
        }

        private void Gui_Closed(object sender, EventArgs e)
        {
            pluginsManager.DeletePlugin(this);
        }

        private void OnDataLoaded(object sender, DataEventArgs args)
        {
            SimulatorDataSet data = args.Data;            
            if (ViewSource == null)
                return;
            if(shouldReset!=ResetModeEnum.NO_RESET)
            {
                bool invalidateLap = shouldReset == ResetModeEnum.MANUAL || data.SessionInfo.SessionType != SessionInfo.SessionTypeEnum.Race;
                timing = SessionTiming.FromSimulatorData(args.Data, invalidateLap);
                timing.PaceLaps = paceLaps;
                timing.BestLapChangedEvent += BestLapChangedHandler;
                InitializeGui(data);
                ChangeTimingMode();
                shouldReset = ResetModeEnum.NO_RESET;
            }
            try
            {
                if (timing != null)
                    timing.UpdateTiming(data);
            }catch(SessionTiming.DriverNotFoundException)
            {
                shouldReset = ResetModeEnum.AUTOMATIC;
                return;
            }
            TimeSpan timeSpan = DateTime.Now.Subtract(lastRefreshTiming);
           
            if(sessionType!= timing.SessionType)
            {
                shouldReset = ResetModeEnum.AUTOMATIC;
                sessionType = timing.SessionType;
                return;
            }
            if (timeSpan.TotalMilliseconds >500)
            {
                lastRefreshTiming = DateTime.Now;
                gui.Dispatcher.Invoke((Action)(() =>
                {
                    gui.lblTime.Content = "Session Time: " + data.SessionInfo.SessionTime.ToString("mm\\:ss\\.fff");
                    gui.pedalControl.UpdateControl(data);
                    gui.whLeftFront.UpdateControl(data);
                    gui.whRightFront.UpdateControl(data);
                    gui.whLeftRear.UpdateControl(data);
                    gui.whRightRear.UpdateControl(data);
                    //gui.gMeter.VertG = -data.PlayerCarInfo.Acceleration.ZInG;
                    //gui.gMeter.HorizG = data.PlayerCarInfo.Acceleration.XInG;
                    //gui.gMeter.Refresh();
                    gui.timingCircle.RefreshSession(data);
                    

                    gui.lblWeather.Content = "Air: " + data.SessionInfo.WeatherInfo.airTemperature.InCelsius.ToString("n1") + " |Track: " + data.SessionInfo.WeatherInfo.trackTemperature.InCelsius.ToString("n1")
                    +"| Rain Intensity: "+data.SessionInfo.WeatherInfo.rainIntensity+"%";
                    gui.lblRemainig.Content = GetSessionRemainig(data);
                    ViewSource.View.Refresh();
                    if (scrollToPlayer && timing.Player != null)
                    {                        
                        gui.dtTimig.ScrollIntoView(timing.Player);
                    }
                }));
            }
            TimeSpan timeSpanCarIno = DateTime.Now.Subtract(lastRefreshCarInfo);
            if (timeSpanCarIno.TotalMilliseconds > 33)
            {
                gui.Dispatcher.Invoke((Action)(() =>
                {
                    gui.lblTime.Content = "Session Time: " + data.SessionInfo.SessionTime.ToString("mm\\:ss\\.fff");
                    gui.pedalControl.UpdateControl(data);
                    gui.whLeftFront.UpdateControl(data);
                    gui.whRightFront.UpdateControl(data);
                    gui.whLeftRear.UpdateControl(data);
                    gui.whRightRear.UpdateControl(data);  
                    gui.fuelMonitor.ProcessDataSet(data);
                    //gui.gMeter.VertG = -data.PlayerCarInfo.Acceleration.ZInG;
                    //gui.gMeter.HorizG = data.PlayerCarInfo.Acceleration.XInG;
                    //gui.gMeter.Refresh();
                }));
                lastRefreshCarInfo = DateTime.Now;
            }
        }

        private string GetSessionRemainig(SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SessionLengthType == SessionInfo.SessionLengthTypeEnum.NA)
                return "NA";
            if (dataSet.SessionInfo.SessionLengthType == SessionInfo.SessionLengthTypeEnum.Time)
                return "Time Remaining: "+((int)(dataSet.SessionInfo.SessionTimeRemaining / 60)) + ":" + ((int)dataSet.SessionInfo.SessionTimeRemaining % 60);
            if (dataSet.SessionInfo.SessionLengthType == SessionInfo.SessionLengthTypeEnum.Laps)
                return "Laps: "+(dataSet.SessionInfo.LeaderCurrentLap + "/" + dataSet.SessionInfo.TotalNumberOfLaps);
            return "NA";
        }

        private void OnSessionStarted(object sender, DataEventArgs args)
        {            
            timing = SessionTiming.FromSimulatorData(args.Data, args.Data.SessionInfo.SessionType != SessionInfo.SessionTypeEnum.Race);
            timing.BestLapChangedEvent += BestLapChangedHandler;
            timing.PaceLaps = paceLaps;
            InitializeGui(args.Data);
            ChangeTimingMode();            
        }

        public ICollectionView TimingInfo { get => ViewSource.View; }

        private void InitializeGui(SimulatorDataSet data)
        {

            gui.Dispatcher.Invoke(() =>
            {
                if (Collection == null)
                {
                    Collection = new ObservableCollection<Driver>();
                    ViewSource = new CollectionViewSource();
                    ViewSource.Source = Collection;
                    ChangeTimingMode();                    
                    
                    gui.dtTimig.DataContext = this;
                    gui.lblBestLap.DataContext = this;
                }
                Collection.Clear();
                foreach (Driver d in timing.Drivers.Values)
                {
                    Collection.Add(d);
                }                

                StringBuilder sb = new StringBuilder();
                sb.Append(data.SessionInfo.TrackName);
                sb.Append(" (");
                sb.Append(data.SessionInfo.TrackLayoutName);

                sb.Append(") - ");
                sb.Append(data.SessionInfo.SessionType);
                gui.lblTrack.Content = sb.ToString();

                gui.timingCircle.SetSessionInfo(data);
                gui.fuelMonitor.ResetFuelMonitor();
            });
            NotifyPropertyChanged();
        }

        private void BestLapChangedHandler(object sender, SessionTiming.BestLapChangedArgs args)
        {
            bestSessionLap = args.Lap;
            NotifyPropertyChanged();
        }

        protected virtual void NotifyPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BestLapFormatted"));
        }
    }
}
