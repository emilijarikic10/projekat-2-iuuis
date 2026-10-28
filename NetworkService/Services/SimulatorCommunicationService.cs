using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace NetworkService.Services
{
    public class MeasurementReceivedEventArgs : EventArgs
    {
        public int EntityIndex { get; }
        public double Value { get; }

        public MeasurementReceivedEventArgs(int entityIndex, double value)
        {
            EntityIndex = entityIndex;
            Value = value;
        }
    }

    public class SimulatorCommunicationService
    {
        private const string PipeName = "NetworkServiceMeasurementsPipe";

        private readonly Func<int> getEntityCount;
        private volatile bool isRunning;
        private Thread listenerThread;

        public event EventHandler<MeasurementReceivedEventArgs> MeasurementReceived;

        public SimulatorCommunicationService(Func<int> getEntityCount)
        {
            this.getEntityCount = getEntityCount;
        }

        public void Start()
        {
            if (isRunning)
                return;

            isRunning = true;
            listenerThread = new Thread(ListenLoop) { IsBackground = true };
            listenerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    using (NamedPipeServerStream server = new NamedPipeServerStream(
                        PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None))
                    {
                        server.WaitForConnection();
                        HandleClient(server);
                    }
                }
                catch
                {
                    Thread.Sleep(250);
                }
            }
        }

        private void HandleClient(NamedPipeServerStream server)
        {
            using (StreamReader reader = new StreamReader(server))
            using (StreamWriter writer = new StreamWriter(server) { AutoFlush = true })
            {
                while (isRunning && server.IsConnected)
                {
                    string line = reader.ReadLine();

                    if (line == null)
                        break;

                    if (line == "OBJECT_COUNT?")
                    {
                        writer.WriteLine(getEntityCount());
                        continue;
                    }

                    string[] parts = line.Split(':');

                    if (parts.Length != 2)
                        continue;

                    if (!int.TryParse(parts[0], out int index))
                        continue;

                    if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                        continue;

                    MeasurementReceived?.Invoke(this, new MeasurementReceivedEventArgs(index, value));
                }
            }
        }
    }
}