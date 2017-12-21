﻿namespace SecondMonitor.DataModel
{
    using SecondMonitor.DataModel.BasicProperties;

    public class WeatherInfo
    {
        public Temperature AirTemperature = Temperature.FromCelsius(-1);
        public Temperature TrackTemperature = Temperature.FromCelsius(-1);
        public int RainIntensity = 0;
    }
}
