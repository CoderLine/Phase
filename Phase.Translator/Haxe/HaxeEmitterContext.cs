using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;

namespace Phase.Translator.Haxe
{
    public class HaxeEmitterContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Stack<IWriter> _writerStack;
        private Stack<SyntaxNode> _nodeStack;

        public HaxeEmitter Emitter { get; }
        public PhaseType CurrentType { get; }
        public bool IsMethodInvocation { get; set; }
        public IWriter Writer { get; set; }
        public bool IsConstInitializer { get; set; }
        public Stack<string> CurrentExceptionName { get; private set; }
        public Stack<IEnumerable<ExpressionSyntax>> CurrentForIncrementors { get; set; }
        public ISymbol CurrentMember { get; set; }
        public int RecursiveCatch { get; set; }
        public int RecursiveUsing { get; set; }
        public int RecursiveObjectCreation { get; set; }
        public IMethodSymbol SetterMethod { get; set; }
        public bool IsCaseLabel { get; set; }
        public bool IsAssignmentLeftHand { get; set; }
        public int InitializerCount { get; set; }

        public HaxeEmitterContext(HaxeEmitter emitter, PhaseType type)
        {
            Emitter = emitter;
            CurrentType = type;
            _writerStack = new Stack<IWriter>();
            _nodeStack = new Stack<SyntaxNode>();
            Writer = new InMemoryWriter();
            CurrentExceptionName = new Stack<string>();
            CurrentForIncrementors= new Stack<IEnumerable<ExpressionSyntax>>();
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

        public async Task<HaxeEmitterContext> EmitAsync(CancellationToken cancellationToken)
        {
            //Log.Trace($"\tEmitting Type {CurrentType.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
            try
            {
                switch (CurrentType.Kind)
                {
                    case PhaseTypeKind.Class:
                        var classBlock = new ClassBlock(this, (PhaseClass)CurrentType);
                        classBlock.Emit(cancellationToken);
                        break;
                    case PhaseTypeKind.Struct:
                        var structBlock = new ClassBlock(this, (PhaseStruct)CurrentType);
                        structBlock.Emit(cancellationToken);
                        break;
                    case PhaseTypeKind.Interface:
                        var interfaceBlock = new InterfaceBlock(this, (PhaseInterface)CurrentType);
                        interfaceBlock.Emit(cancellationToken);
                        break;
                    case PhaseTypeKind.Enum:
                        var enumBlock = new EnumBlock(this, (PhaseEnum)CurrentType);
                        enumBlock.Emit(cancellationToken);
                        break;
                    case PhaseTypeKind.Delegate:
                        var delegateBlock = new DelegateBlock(this, (PhaseDelegate)CurrentType);
                        delegateBlock.Emit(cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                var location = _nodeStack.Count > 0 ? _nodeStack.Peek()?.GetLocation() : null;
                Log.Error(CultureInfo.InvariantCulture, Diagnostic.Create(PhaseErrors.PH017, location, e.ToString()));
                throw;
            }

            return this;
        }
    }
}
