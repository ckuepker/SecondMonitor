﻿namespace SecondMonitor.PluginManager.GameConnector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    using SecondMonitor.DataModel.Snapshot;

    public abstract class AbstractGameConnector : IGameConnector
    {
        public event EventHandler<DataEventArgs> DataLoaded;

        public event EventHandler<EventArgs> ConnectedEvent;

        public event EventHandler<EventArgs> Disconnected;

        public event EventHandler<DataEventArgs> SessionStarted;

        public abstract bool IsConnected { get; }

        public int TickTime { get; set; }

        private readonly string[] executables;
               
        private readonly Queue<SimulatorDataSet> _queue = new Queue<SimulatorDataSet>();

        private Thread _daemonThread;
        private Process _process;

        protected AbstractGameConnector(string[] executables)
        {
            this.executables = executables;
            TickTime = 10;
        }

        protected bool ShouldDisconnect
        {
            get;
            set;
        }

        public bool IsProcessRunning()
        {
            if (_process != null)
            {
                if (!_process.HasExited)
                {
                    return true;
                }

                _process = null;
                return false;
            }

            foreach (var processName in this.executables)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length <= 0)
                {
                    continue;
                }

                _process = processes[0];
                return true;
            }

            return false;
        }

        public void ASyncConnect()
        {
            Thread asyncConnectThread = new Thread(AsyncConnector) { IsBackground = true };
            asyncConnectThread.Start();
        }

        public bool TryConnect()
        {
            return Connect();
        }

        private bool Connect()
        {
            if (!this.IsProcessRunning())
            {
                return false;
            }

            try
            {
                this.OnConnection();
                RaiseConnectedEvent();
                StartDaemon();
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }


        protected abstract void OnConnection();

        protected abstract void ResetConnector();

        protected abstract void DaemonMethod();

        protected void AddToQueue(SimulatorDataSet set)
        {
            lock (this._queue)
            {
                this._queue.Enqueue(set);
            }
        }

        private void AsyncConnector()
        {
            while (!TryConnect())
            {
                Thread.Sleep(10);
            }
        }

        private void StartDaemon()
        {
            if (_daemonThread != null && _daemonThread.IsAlive)
            {
                throw new InvalidOperationException("Daemon is already running");
            }

            ResetConnector();
            this.ShouldDisconnect = false;
            _queue.Clear();
            _daemonThread = new Thread(DaemonMethod) { IsBackground = true };
            _daemonThread.Start();

            Thread queueProcessorThread = new Thread(QueueProcessor) { IsBackground = true };
            queueProcessorThread.Start();

        }

        private void QueueProcessor()
        {
            while (this.ShouldDisconnect == false)
            {
                SimulatorDataSet set;
                while (_queue.Count != 0)
                {
                    lock (_queue)
                    {
                        set = _queue.Dequeue();
                    }

                    RaiseDataLoadedEvent(set);
                }

                Thread.Sleep(TickTime);
            }

            lock (this._queue)
            {
                this._queue.Clear();
            }
        }

        protected void RaiseDataLoadedEvent(SimulatorDataSet data)
        {
            DataEventArgs args = new DataEventArgs(data);
            DataLoaded?.Invoke(this, args);
        }

        protected void RaiseConnectedEvent()
        {
            EventArgs args = new EventArgs();
            ConnectedEvent?.Invoke(this, args);
        }

        protected void RaiseDisconnectedEvent()
        {
            EventArgs args = new EventArgs();
            Disconnected?.Invoke(this, args);
        }

        protected void RaiseSessionStartedEvent(SimulatorDataSet data)
        {
            DataEventArgs args = new DataEventArgs(data);
            EventHandler<DataEventArgs> handler = SessionStarted;
            handler?.Invoke(this, args);
        }
    }
}