using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using NLog;
using Phase.Translator.Utils;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Phase.Translator.Kotlin
{
    public class KotlinEmitterContext : BaseEmitterContext<KotlinEmitter>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override string FileName
        {
            get
            {
                var name = Emitter.GetTypeName(CurrentType.TypeSymbol);
                var p = name.IndexOf("<");
                if (p >= 0) name = name.Substring(0, p);
                name = name.Replace("?", "");
                return name.Replace('.', Path.DirectorySeparatorChar) + ".kt";
            }
        }

        public int RecursiveForeach { get; set; }
        public bool IsInMethodBody { get; set; }

        public Dictionary<SyntaxNode, string> LoopNames { get; set; }
        private Stack<(string name, bool used)> _lambdaNames;

        public KotlinEmitterContext(KotlinEmitter emitter, PhaseType type)
            : base(emitter, type)
        {
            LoopNames = new Dictionary<SyntaxNode, string>(SyntaxNodeEqualityComparer.Instance);
            ParameterNames = new Dictionary<IParameterSymbol, string>(SymbolEquivalenceComparer.Instance);
            _lambdaNames = new Stack<(string name, bool used)>();
        }


        public void AddLambdaNameForReturn()
        {
            _lambdaNames.Push(("lambda" + _lambdaNames.Count, false));
        }

        public void RemoveLambdaNameForReturn()
        {
            _lambdaNames.Pop();
        }

        public bool WasLambdaNameForReturnUsed(out string lambdaName)
        {
            var top = _lambdaNames.Peek();
            lambdaName = top.name;
            return top.used;
        }

        public string GetLambdaNameForReturn()
        {
            if (_lambdaNames.Count > 0)
            {
                var name = _lambdaNames.Pop();
                _lambdaNames.Push((name.name, true));
                return name.name;
            }
            return null;
        }

        public override void Emit(CancellationToken cancellationToken)
        {
            //Log.Trace($"\tEmitting Type {CurrentType.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
            switch (CurrentType.Kind)
            {
                case PhaseTypeKind.Class:
                    var classBlock = new ClassBlock(this);
                    classBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Struct:
                    var structBlock = new ClassBlock(this);
                    structBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Interface:
                    var interfaceBlock = new InterfaceBlock(this);
                    interfaceBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Enum:
                    var enumBlock = new EnumBlock(this);
                    enumBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Delegate:
                    var delegateBlock = new DelegateBlock(this);
                    delegateBlock.Emit(cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void WriteNestedType(INamedTypeSymbol member, CancellationToken cancellationToken)
        {
            if (Emitter.NestedTypes.TryGetValue(member, out var type))
            {
                switch (type.Kind)
                {
                    case PhaseTypeKind.Class:
                        var classBlock = new ClassBlock(type, this);
                        classBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Struct:
                        var structBlock = new ClassBlock(type, this);
                        structBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Interface:
                        var interfaceBlock = new InterfaceBlock(type, this);
                        interfaceBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Enum:
                        var enumBlock = new EnumBlock(type, this);
                        enumBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Delegate:
                        var delegateBlock = new DelegateBlock(type, this);
                        delegateBlock.EmitNested(cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void WriteTypeParameterArrayCast(ITypeSymbol definition, ITypeSymbol type)
        {
            if (definition.TypeKind != TypeKind.Array) return;
            if (((IArrayTypeSymbol)definition).ElementType.TypeKind != TypeKind.TypeParameter)
            {
                return;
            }

            switch (((IArrayTypeSymbol)type).ElementType.SpecialType)
            {
                case SpecialType.System_SByte:
                    Writer.Write("!!.toByteArray()");
                    break;
                case SpecialType.System_Int16:
                    Writer.Write("!!.toShortArray()");
                    break;
                case SpecialType.System_Int32:
                    Writer.Write("!!.toIntArray()");
                    break;
                case SpecialType.System_Int64:
                    Writer.Write("!!.toLongArray()");
                    break;

                case SpecialType.System_Byte:
                    Writer.Write("!!.toUByteArray()");
                    break;
                case SpecialType.System_UInt16:
                    Writer.Write("!!.toUShortArray()");
                    break;
                case SpecialType.System_UInt32:
                    Writer.Write("!!.toUIntArray()");
                    break;
                case SpecialType.System_UInt64:
                    Writer.Write("!!.toULongArray()");
                    break;

                case SpecialType.System_Single:
                    Writer.Write("!!.toFloatArray()");
                    break;
                case SpecialType.System_Double:
                    Writer.Write("!!.toDoubleArray()");
                    break;

                case SpecialType.System_Boolean:
                    Writer.Write("!!.toBooleanArray()");
                    break;
                case SpecialType.System_Char:
                    Writer.Write("!!.toCharArray()");
                    break;
            }
        }

        public Dictionary<IParameterSymbol, string> ParameterNames { get; private set; }

        public void BuildLocalParameters(IMethodSymbol method, BlockSyntax body, CancellationToken cancellationToken)
        {
            var walker = new ParameterFinderWalker(Emitter, method.Parameters);
            walker.Visit(body);
            ParameterNames = walker.ParameterNames;
        }
    }

    public class ParameterFinderWalker : CSharpSyntaxWalker
    {
        private readonly KotlinEmitter _emitter;
        private HashSet<IParameterSymbol> _parameters;
        public Dictionary<IParameterSymbol, string> ParameterNames { get; set; }

        public ParameterFinderWalker(KotlinEmitter emitter, IEnumerable<IParameterSymbol> parameters)
        {
            _emitter = emitter;
            _parameters = new HashSet<IParameterSymbol>(parameters, SymbolEquivalenceComparer.Instance);
            ParameterNames = new Dictionary<IParameterSymbol, string>();
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (node.Left.Kind() == SyntaxKind.IdentifierName)
            {
                ParameterFound(node.Left);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (node.Operand.Kind() == SyntaxKind.IdentifierName)
            {
                ParameterFound(node.Operand);
            }
            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (node.Operand.Kind() == SyntaxKind.IdentifierName)
            {
                ParameterFound(node.Operand);
            }
            base.VisitPrefixUnaryExpression(node);
        }

        private void ParameterFound(ExpressionSyntax syntax)
        {
            if (_emitter.GetSymbolInfo(syntax).Symbol is IParameterSymbol leftSymbol && _parameters.Contains(leftSymbol))
            {
                ParameterNames[leftSymbol] = "local" + leftSymbol.Name;
            }
        }
    }

    public class SyntaxNodeEqualityComparer : IEqualityComparer<SyntaxNode>
    {
        public static readonly SyntaxNodeEqualityComparer Instance = new SyntaxNodeEqualityComparer();


        public bool Equals(SyntaxNode x, SyntaxNode y)
        {
            if (x == null && y == null) return true;
            if (x == null) return false;
            if (y == null) return false;
            return x.GetLocation().Equals(y.GetLocation());
        }

        public int GetHashCode(SyntaxNode obj)
        {
            return obj.GetLocation().GetHashCode();
        }
    }
}
