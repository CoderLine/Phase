using System;
using System.Diagnostics;
using NLog;

namespace Phase.Translator
{
    class ProfilingHelper: IDisposable
    {
        private static int _indent = 0;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly string _phase;
        private Stopwatch _sw;
        private string _indentStr;

        public ProfilingHelper(string phase)
        {
            _phase = phase;
            _indentStr = "".PadLeft(_indent * 2, ' ');

            _sw = new Stopwatch();
            Log.Info($"{_indentStr}Begin '{phase}'");
            _sw.Start();
            _indent++;
        }

        public void Dispose()
        {
            _indent--;
            _sw.Stop();
            Log.Info($"{_indentStr}End '{_phase}' {_sw.Elapsed.TotalMilliseconds}");
            _sw.Start();
        }
    }
}
