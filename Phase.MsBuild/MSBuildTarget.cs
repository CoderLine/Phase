using Microsoft.Build.Framework;
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
            var formattedMessage = logEvent.FormattedMessage;
            if (formattedMessage.Contains(".cs") && formattedMessage.Contains("):"))
            {
                _log.LogMessage(MessageImportance.High, formattedMessage);
            }
            else
            {
                var message = RenderLogEvent(Layout, logEvent);
                _log.LogMessage(MessageImportance.High, message);
            }
        }
    }
}