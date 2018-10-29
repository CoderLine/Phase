using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;

namespace Phase.Translator
{
    public abstract class BaseEmitterContext
    {
        private readonly Stack<IWriter> _writerStack;
        private readonly Stack<SyntaxNode> _nodeStack;

        public PhaseType CurrentType { get; }
        public bool IsMethodInvocation { get; set; }
        public IWriter Writer { get; set; }
        public bool IsConstInitializer { get; set; }
        public Stack<string> CurrentExceptionName { get; }
        public Stack<IEnumerable<ExpressionSyntax>> CurrentForIncrementors { get; set; }
        public ISymbol CurrentMember { get; set; }
        public int RecursiveCatch { get; set; }
        public int RecursiveUsing { get; set; }
        public int RecursiveObjectCreation { get; set; }
        public IMethodSymbol SetterMethod { get; set; }
        public bool IsCaseLabel { get; set; }
        public bool IsAssignmentLeftHand { get; set; }
        public int InitializerCount { get; set; }

        public SyntaxNode LastKnownNode => _nodeStack.Count > 0 ? _nodeStack.Peek() : null;

        public abstract string FileName { get; }

        protected BaseEmitterContext(PhaseType type)
        {
            CurrentType = type;
            _writerStack = new Stack<IWriter>();
            _nodeStack = new Stack<SyntaxNode>();
            Writer = new InMemoryWriter();
            CurrentExceptionName = new Stack<string>();
            CurrentForIncrementors = new Stack<IEnumerable<ExpressionSyntax>>();
        }

        public void BeginEmit(SyntaxNode node)
        {
            _nodeStack.Push(node);
        }

        public void EndEmit(SyntaxNode node)
        {
            _nodeStack.Pop();
        }

        public void PushWriter()
        {
            _writerStack.Push(Writer);
            Writer = new InMemoryWriter() { Level = Writer.Level };
        }

        public string PopWriter()
        {
            var level = Writer.Level;
            var result = Writer.ToString();
            Writer = _writerStack.Pop();
            Writer.Level = level;
            return result;
        }

        public abstract void Emit(CancellationToken cancellationToken);
    }

    public abstract class BaseEmitterContext<TEmitter> : BaseEmitterContext
        where TEmitter : BaseEmitter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public TEmitter Emitter { get; }

        protected BaseEmitterContext(TEmitter emitter, PhaseType type) : base(type)
        {
            Emitter = emitter;
        }

        public string GetSymbolName(ISymbol symbol)
        {
            return Emitter.GetSymbolName(symbol, this);
        }




        public bool TryGetCallerMemberInfo(IParameterSymbol parameter, ISymbol callerMember, SyntaxNode callerNode, out string value)
        {
            var callerAttribute = parameter.GetAttributes().FirstOrDefault(
                a => a.AttributeClass.Equals(Emitter.GetPhaseType("System.Runtime.CompilerServices.CallerMemberNameAttribute"))
                    || a.AttributeClass.Equals(Emitter.GetPhaseType("System.Runtime.CompilerServices.CallerLineNumberAttribute"))
                    || a.AttributeClass.Equals(Emitter.GetPhaseType("System.Runtime.CompilerServices.CallerFilePathAttribute"))
            );
            if (callerAttribute == null)
            {
                value = null;
                return false;
            }
            switch (callerAttribute.AttributeClass.Name)
            {
                case "CallerMemberNameAttribute":
                    if (callerMember == null)
                    {
                        value = null;
                        Log.Warn("Could not get caller member name");
                        return false;
                    }
                    value = "\"" + GetSymbolName(callerMember) + "\"";
                    return true;
                case "CallerLineNumberAttribute":
                    if (callerNode == null)
                    {
                        value = null;
                        Log.Warn("Could not get caller line number");
                        return false;
                    }
                    value = callerNode.GetText().Lines[0].LineNumber.ToString();
                    return true;
                case "CallerFilePathAttribute":
                    if (callerNode == null)
                    {
                        value = null;
                        Log.Warn("Could not get caller file path");
                        return false;
                    }
                    value = "\"" + callerNode.SyntaxTree.FilePath.Replace("\\", "\\\\") + "\"";
                    return true;
            }

            value = null;
            return false;
        }
    }
}
