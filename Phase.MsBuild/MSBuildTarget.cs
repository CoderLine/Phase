using Microsoft.Build.Utilities;
using NLog;
using NLog.Targets;

namespace Phase.MsBuild
{
    public class MSBuildTarget : TargetWithLayout
    {
        private readonly TaskLoggingHelper _log;

        public MSBuildTarget(TaskLoggingHelper log)
        {
            _log = log;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = RenderLogEvent(Layout, logEvent);
            if (logEvent.Level == LogLevel.Trace)
            {
                _log.LogMessage(message);
            }
            if (logEvent.Level == LogLevel.Debug)
            {
                _log.LogMessage(message);
            }
            if (logEvent.Level == LogLevel.Info)
            {
                _log.LogMessage(message);
            }
            if (logEvent.Level == LogLevel.Warn)
            {
                _log.LogWarning(message);
            }
            if (logEvent.Level == LogLevel.Error)
            {
                _log.LogError(message);
            }
            if (logEvent.Level == LogLevel.Fatal)
            {
                _log.LogError(message);
            }
        }
    }
}