using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            Writer = new InMemoryWriter();
        }

        public string PopWriter()
        {
            var result = Writer.ToString();
            Writer = _writerStack.Pop();
            return result;
        }

        public abstract void Emit(CancellationToken cancellationToken);
    }

    public abstract class BaseEmitterContext<TEmitter> : BaseEmitterContext
        where TEmitter : BaseEmitter
    {
        public TEmitter Emitter { get; }
       
        protected BaseEmitterContext(TEmitter emitter, PhaseType type) : base(type)
        {
            Emitter = emitter;
        }
    }
}
