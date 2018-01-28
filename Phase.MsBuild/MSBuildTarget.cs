using System.Linq;
using System.Text.RegularExpressions;
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

        private static readonly Regex ErrorPattern = new Regex(@"(?<File>[^(]+)\((?<Location>[^)]+)\): (?<Identifier>[^:]+): (?<Message>.*)");
        protected override void Write(LogEventInfo logEvent)
        {
            var formattedMessage = logEvent.FormattedMessage;
            var match = ErrorPattern.Match(formattedMessage);   
            if(match.Success)
            {
                var location = match.Groups["Location"].Value.Split(',').Select(int.Parse).ToArray();

                if (match.Groups["Identifier"].Value.Contains("warning PH000"))
                {
                    _log.LogWarning(null, null, null, match.Groups["File"].Value,
                        location[0], location[1], 
                        location.Length > 2 ? location[2] : location[0],
                        location.Length > 3 ? location[3] : location[0],
                        match.Groups["Message"].Value
                    );
                }
                else if (match.Groups["Identifier"].Value.Contains("error PH000"))
                {
                    _log.LogError(null, null, null, match.Groups["File"].Value,
                        location[0], location[1],
                        location.Length > 2 ? location[2] : location[0],
                        location.Length > 3 ? location[3] : location[0],
                        match.Groups["Message"].Value
                    );
                }
                else
                {
                    _log.LogMessage(MessageImportance.High, formattedMessage);
                }
            }
            else
            {
                var message = RenderLogEvent(Layout, logEvent);
                _log.LogMessage(MessageImportance.High, message);
            }
        }
    }
}