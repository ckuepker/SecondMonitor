﻿namespace ControlTestingApp.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Input;

    using ControlTestingApp.Annotations;

    using SecondMonitor.DataModel.BasicProperties;

    public class TemperatureTestViewModel : DependencyObject, INotifyPropertyChanged
    {
        private class RelayCommand : ICommand
        {
            private readonly Action _relayAction;

            public RelayCommand(Action action)
            {
                _relayAction = action;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                _relayAction();
            }

            public event EventHandler CanExecuteChanged;
        }

        private int _valueInNumber = 0;
        private Temperature _valueInTemperature;

        private TemperatureUnits _temperatureUnits = TemperatureUnits.Celsius;

        public event PropertyChangedEventHandler PropertyChanged;

        public int ValueInNumber
        {
            get => _valueInNumber;
            set
            {
                _valueInNumber = value;
                ValueInTemperature = Temperature.FromCelsius(_valueInNumber);
            }
        }

        public Temperature ValueInTemperature
        {
            get => _valueInTemperature;
            set
            {
                _valueInTemperature = value;
                OnPropertyChanged();
            }
        }

        public Temperature MinimumInTemperature => Temperature.FromCelsius(2);
        public Temperature MaximumInTemperature  => Temperature.FromCelsius(135);

        public ICommand ChangeUnitCommand => new RelayCommand(ChangeTemperatureUnits);

        public TemperatureUnits TemperatureUnits
        {
            get => _temperatureUnits;
            private set
            {
                _temperatureUnits = value;
                OnPropertyChanged();
            }
        }

        private void ChangeTemperatureUnits()
        {
            if (TemperatureUnits == TemperatureUnits.Celsius)
            {
                TemperatureUnits = TemperatureUnits.Fahrenheit;
                return;
            }

            if (TemperatureUnits == TemperatureUnits.Fahrenheit)
            {
                TemperatureUnits = TemperatureUnits.Kelvin;
                return;
            }

            if (TemperatureUnits == TemperatureUnits.Kelvin)
            {
                TemperatureUnits = TemperatureUnits.Celsius;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}