using System;
using System.Diagnostics;
using NLog;

namespace Phase.Translator.Utils
{
    public class LogHelper : IDisposable
    {
        private readonly string _action;
        private readonly Logger _log;
        private Stopwatch _sw;
        private string _indent;

        public LogHelper(string action, Logger log, int indent = 0)
        {
            _action = action;
            _log = log;

            _indent = "".PadLeft(indent * 2, ' ');

            _sw = new Stopwatch();
            _log.Trace($"{_indent}Start {action}");
            _sw.Start();
        }
        public void Dispose()
        {
            _sw.Stop();
            _log.Trace($"{_indent}Finished {_action} in {_sw.ElapsedMilliseconds}ms");
        }
    }
}
