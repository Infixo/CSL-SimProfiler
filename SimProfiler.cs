using System;
using System.IO;
using System.Diagnostics;
using ColossalFramework.Plugins;

namespace SimProfiler
{
    public static class SimProfiler
    {
        //private const long TICKS_PER_SEC = 3906396; // Stopwatch.Frequency 3906396

        // performance counters
        static Stopwatch[] watches = new Stopwatch[50];
        static string[] names = new string[50];
        static int[] calls = new int[50];
        // real time
        //static bool started = false;
        //static DateTime realStart;
        //static TimeSpan realTime;

        public static void SetName(int i, string name)
        {
            names[i] = name;
        }

        public static void Begin(int index)
        {
            watches[index].Start();
        }

        public static void End(int index)
        {
            watches[index].Stop();
            ++calls[index];
        }

        public static void StartMeasuring()
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SimProfiler: profiling started");
            //watches = new Stopwatch[50];
            //names = new string[50];
            //calls = new int[50];
            for (int i = 0; i < 50; i++)
            {
                watches[i] = new Stopwatch();
                watches[i].Reset();
                if (names[i] != null) names[i] = $"Counter{i}";
                calls[i] = 0;
            }
            // static names
            names[40] = "VM-total";
            names[41] = "VM-extra-step";
            names[42] = "VM-sim-step";
        }

        public static void StopMeasuring()
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SimProfiler: profiling finished, logging results");
            // calculate total
            long totalTicks = 0;
            for (int i = 0; i < 50; i++)
                if (calls[i] > 0)
                    totalTicks += watches[i].ElapsedTicks;
            // open the file and write results
            string outputFile = ColossalFramework.IO.DataLocation.localApplicationData + Path.DirectorySeparatorChar + "SimProfiler.txt";
            try
            {
                using (var streamWriter = new System.IO.StreamWriter(outputFile))
                {
                    // header
                    //streamWriter.WriteLine("Profiling started at {0}.", realStart);
                    //streamWriter.WriteLine("Real time consumed was {0} miliseconds.", (int)realTime.TotalMilliseconds);
                    streamWriter.WriteLine("Profiling report generated at {0}.", DateTime.Now);
                    streamWriter.WriteLine("Time consumed: {0} ticks", totalTicks); //=> {1} miliseconds.", totalTicks, 1000 * totalTicks / TICKS_PER_SEC);
                    for (int i = 0; i < 50; i++)
                        if (calls[i] > 0)
                            streamWriter.WriteLine("{0,-30}{1,10} ticks ({2,8:P2}), {3,10} calls => {4,8} ticks/call",
                                names[i], // counter
                                watches[i].ElapsedTicks, // total elapsed time
                                (double)watches[i].ElapsedTicks / (double)totalTicks, // percent of time
                                calls[i], // no of calls
                                watches[i].ElapsedTicks / calls[i]); // ticks per call
                }
            }
            catch (Exception e)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "SimProfiler: ERROR " + e.Message);
            }
        }

    } // class

} // namespace
