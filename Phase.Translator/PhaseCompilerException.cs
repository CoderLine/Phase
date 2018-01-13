using System;
using System.Runtime.Serialization;

namespace Phase.Translator
{
    [Serializable]
    public class PhaseCompilerException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public PhaseCompilerException()
        {
        }

        public PhaseCompilerException(string message) : base(message)
        {
        }

        public PhaseCompilerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PhaseCompilerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}