using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace xgPlatform.xgCore.Common
{
    public class InstantProfilerEntry
    {
        public Stack<long> StartTimes = new Stack<long>();
        public List<long> Measurements = new List<long>();
    }

    public static class MiniProfiler
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly string[] _separators = new string[] { "," };
        private static HashSet<string> _suspectTypes;
        private static Dictionary<string, InstantProfilerEntry> _journal = new Dictionary<string, InstantProfilerEntry>();
        private static Stopwatch _stopper;

        [Conditional("DEBUG")]
        public static void Reset(string suspectTypes)
        {
            _stopper = Stopwatch.StartNew();
            _suspectTypes = new HashSet<string>(suspectTypes.Split(_separators, StringSplitOptions.RemoveEmptyEntries));
            _journal.Clear();
        }

        private static string CreateProfileKey()
        {
            StackTrace trace = new StackTrace(true);
            for (int i = 0; i < trace.FrameCount; ++i)
            {
                StackFrame frame = trace.GetFrame(i);
                MethodBase method = frame.GetMethod();
                string reflectedType = method.ReflectedType.Name;
                if (_suspectTypes.Contains(reflectedType))
                {
                    return $"{reflectedType}.{method.Name}";
                }
            }

            return "Unknown";
        }

        [Conditional("DEBUG")]
        public static void StartTiming()
        {
            string profileKey = CreateProfileKey();
            if (!_journal.ContainsKey(profileKey))
            {
                _journal.Add(profileKey, new InstantProfilerEntry());
            }
            _journal[profileKey].StartTimes.Push(_stopper.ElapsedMilliseconds);
        }

        [Conditional("DEBUG")]
        public static void StopTiming()
        {
            long stopTime = _stopper.ElapsedMilliseconds;
            string profileKey = CreateProfileKey();
            if (_journal.Count > 0)
            {
                long startTime = _journal[profileKey].StartTimes.Pop();
                _journal[profileKey].Measurements.Add(stopTime - startTime);
            }
        }

        [Conditional("DEBUG")]
        public static void Quote()
        {
            string profileKey = CreateProfileKey();
            _logger.ConditionalTrace($"Mini Profiler: {profileKey}: " + _stopper.ElapsedMilliseconds);
        }

        [Conditional("DEBUG")]
        public static void Report()
        {
            foreach (KeyValuePair<string, InstantProfilerEntry> entry in _journal)
            {
                _logger.ConditionalTrace($"Mini Profiler: {entry.Key} * {entry.Value.Measurements.Count} = {entry.Value.Measurements.Sum()}");
            }
        }
    }

    public class MiniProfilerAgent : IDisposable
    {
        public MiniProfilerAgent()
        {
            MiniProfiler.StartTiming();
        }

        [Conditional("DEBUG")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                MiniProfiler.StopTiming();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
