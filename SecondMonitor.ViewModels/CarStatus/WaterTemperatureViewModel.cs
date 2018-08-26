﻿namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Base;
    public class WaterTemperatureViewModel : AbstractTemperatureViewModel
    {

        public WaterTemperatureViewModel()
        {
            BitmapImage logo = new BitmapImage();
            logo.BeginInit();
            logo.UriSource = new Uri("pack://application:,,,/SecondMonitor.ViewModels;component/Resources/water.png", UriKind.RelativeOrAbsolute);
            logo.EndInit();
            Icon = logo;
        }

        public override sealed ImageSource Icon { get; protected set; }

        public override Temperature MinimalTemperature { get; protected set; } = Temperature.FromCelsius(0);

        public override Temperature MaximumTemperature { get; protected set; }  = Temperature.FromCelsius(130);

        public override Temperature MaximumNormalTemperature { get; protected set; }  = Temperature.FromCelsius(110);

        protected override Temperature GetTemperatureFromDataSet(SimulatorDataSet dataSet)
        {
            if (dataSet.PlayerInfo == null)
            {
                return Temperature.Zero;
            }

            return dataSet.PlayerInfo.CarInfo.WaterSystemInfo.WaterTemperature;
        }
    }
}