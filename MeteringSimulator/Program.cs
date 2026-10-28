using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace MeteringSimulator
{
    internal class Program
    {
        private const string PipeName = "NetworkServiceMeasurementsPipe";

        private static void Main()
        {
            Console.Title = "MeteringSimulator";
            Random random = new Random();

            try
            {
                using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
                {
                    Console.WriteLine("Connecting to NetworkService...");
                    pipe.Connect(5000);
                    Console.WriteLine("Connected.");

                    using (StreamReader reader = new StreamReader(pipe))
                    using (StreamWriter writer = new StreamWriter(pipe) { AutoFlush = true })
                    {
                        writer.WriteLine("OBJECT_COUNT?");
                        string countLine = reader.ReadLine();

                        if (!int.TryParse(countLine, out int objectCount) || objectCount <= 0)
                        {
                            Console.WriteLine("NetworkService reports no entities to simulate. Exiting.");
                            return;
                        }

                        Console.WriteLine("NetworkService has " + objectCount + " entities. Starting simulation...");

                        while (pipe.IsConnected)
                        {
                            int waitMs = random.Next(1500, 4000);
                            Thread.Sleep(waitMs);

                            int entityIndex = random.Next(objectCount);
                            double value = Math.Round(20 + random.NextDouble() * 75, 1);

                            string message = entityIndex.ToString(CultureInfo.InvariantCulture) + ":" +
                                              value.ToString(CultureInfo.InvariantCulture);

                            writer.WriteLine(message);
                            Console.WriteLine("Sent " + message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection to NetworkService lost or unavailable: " + ex.Message);
            }
        }
    }
}
