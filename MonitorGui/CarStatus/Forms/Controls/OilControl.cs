﻿using System.Windows.Forms;
using SecondMonitor.DataModel;

namespace SecondMonitor.CarStatus.Forms.Controls
{
    public partial class OilControl : UserControl
    {
        public OilControl()
        {
            InitializeComponent();
        }

        private void gPressure_ValueInRangeChanged(object sender, ValueInRangeChangedEventArgs e)
        {

        }

        public void UpdateControl(SimulatorDataSet data)
        {
            gTemperature.Value = (float) data.PlayerCarInfo.OilSystemInfo.OilTemperature.InCelsius;
            gPressure.Value = (float) data.PlayerCarInfo.OilSystemInfo.OilPressure.InAtmospheres;
            lblPressure.Text = data.PlayerCarInfo.OilSystemInfo.OilPressure.InAtmospheres.ToString("0.0");
        }
    }
}
