using Colossal.Logging;

namespace RoadSpeedAdjuster.Utils
{
    internal class PrefixedLogger
    {
        private readonly string m_Prefix;
        private readonly ILog m_Log;

        public PrefixedLogger(string prefix)
        {
            m_Prefix = prefix;

            // FIX: Mod.log (lowercase), not Mod.Log
            m_Log = RoadSpeedAdjuster.Mod.log;
        }

        public void Info(string message)
        {
            LogInternal("INFO", message);
        }

        public void Warn(string message)
        {
            LogInternal("WARN", message);
        }

        public void Error(string message)
        {
            LogInternal("ERROR", message);
        }

        public void Debug(string message)
        {
            LogInternal("DEBUG", message);
        }

        private void LogInternal(string level, string message)
        {
            var formatted = $"[{m_Prefix}] {message}";

            switch (level)
            {
                case "ERROR":
                    m_Log.Error(formatted);
                    break;
                case "WARN":
                    m_Log.Warn(formatted);
                    break;
                case "DEBUG":
                    m_Log.Debug(formatted);
                    break;
                default:
                    m_Log.Info(formatted);
                    break;
            }
        }
    }
}
