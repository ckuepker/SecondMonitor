﻿using SecondMonitor.DataModel.BasicProperties;

namespace SecondMonitor.DataModel
{
    public class FuelInfo
    {
        public FuelInfo()
        {
            FuelCapacity = new Volume();
            FuelRemaining = new Volume();
            FuelPressure = new Pressure();
        }

        public Volume FuelCapacity { get; set; }

        public Volume FuelRemaining { get; set; }

        public Pressure FuelPressure { get; set; }
    }
}
