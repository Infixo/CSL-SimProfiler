using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using ColossalFramework.Plugins;

namespace SimProfiler
{
    public static class SimProfiler
    {
        private const long TICKS_PER_SEC = 3906396; // Stopwatch.Frequency

        // performance counters
        static Dictionary<string, Stopwatch> counters = new Dictionary<string, Stopwatch>();
        static long numSteps = 0;
        // real time
        static bool started = false;
        static DateTime realStart;
        static TimeSpan realTime;

        public static void BeginStep()
        { }
        public static void EndStep()
        {
            ++numSteps;
        }
        
        /// <summary>
        /// Begin measuring with a specified counter
        /// </summary>
        /// <param name="name">Counter name</param>
        public static void Begin(string name)
        {
            if (!started)
            {
                realStart = DateTime.Now;
                started = true;
            }
            // register a couter if doesn't exist yet
            if (!counters.ContainsKey(name))
                counters.Add(name, new Stopwatch());
            counters[name].Start();
        }
        /// <summary>
        /// End measuring with a specified counter
        /// </summary>
        /// <param name="name">Counter name</param>
        public static void End(string name)
        {
            if (!started) return;
            counters[name].Stop();
            realTime = DateTime.Now - realStart;
        }

        public static void StartMeasuring()
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SimProfiler: profiling started");
            started = false; // delayed start
            counters = new Dictionary<string, Stopwatch>();
            numSteps = 0;
        }

        public static void StopMeasuring()
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SimProfiler: profiling finished");
        }

        public static void LogResults()
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SimProfiler: logging results");
            // calculate total
            long totalTicks = 0;
            foreach (KeyValuePair<string, Stopwatch> kvp in counters)
                totalTicks += kvp.Value.ElapsedTicks;
            // open the file and write results
            string outputFile = ColossalFramework.IO.DataLocation.localApplicationData + Path.DirectorySeparatorChar + "SimProfiler.txt";
            try
            {
                using (var streamWriter = new System.IO.StreamWriter(outputFile))
                {
                    // header
                    streamWriter.WriteLine("Profiling started at {0}.", realStart);
                    streamWriter.WriteLine("Real time consumed was {0} miliseconds.", (int)realTime.TotalMilliseconds);
                    streamWriter.WriteLine("Time consumed: {0} ticks => {1} miliseconds.", totalTicks, 1000 * totalTicks / TICKS_PER_SEC);
                    streamWriter.WriteLine("Number of steps: {0} => {1} ticks per step.", numSteps, totalTicks/numSteps);
                    // iterate through the counters
                    foreach (KeyValuePair<string, Stopwatch> kvp in counters)
                        streamWriter.WriteLine("{0,-28};{1,10};{2,8:P2};{3,8} ticks/step",
                            kvp.Key, // manager name
                            kvp.Value.ElapsedTicks,
                            (double)kvp.Value.ElapsedTicks/(double)totalTicks, // percent of time
                            kvp.Value.ElapsedTicks/numSteps); // ticks per step
                }
            }
            catch (Exception e)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "SimProfiler: ERROR " + e.Message);
            }
        } // LogResults
    } // class
} // namespace
