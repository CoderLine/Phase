using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class PrefixUnaryExpressionBlock : AbstractCppEmitterBlock<PrefixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var operandSymbol = Emitter.GetSymbolInfo(Node.Operand, cancellationToken);
            var type = Emitter.GetTypeInfo(Node, cancellationToken);
            if (operandSymbol.Symbol?.Kind == SymbolKind.Property)
            {
                var op = GetOperator();
                switch (Node.Kind())
                {
                    case SyntaxKind.PreIncrementExpression:
                    case SyntaxKind.PreDecrementExpression:
                        // We need to wrap the whole statement like this: 
                        // std::function<System::Int32()>([&,this]()
                        // {
                        //     System::Int32 __value = GetValue();
                        //     SetValue(__value + 1);
                        //     return val;
                        // }();

                        var property = ((IPropertySymbol)operandSymbol.Symbol);
                        if (!string.IsNullOrEmpty(op))
                        {
                            //  std::function<System::Int32()>([&,this]()
                            Write("std::function<");
                            Write(Emitter.GetTypeName(type.Type, false, false,
                                CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
                            Write("()>(");
                            if (EmitterContext.CurrentMember.IsStatic)
                            {
                                Write("[&,this]");
                            }
                            else
                            {
                                Write("[&]");
                            }

                            Write("()");
                            WriteNewLine();
                            BeginBlock();

                            // System::Int32 __value = GetValue();
                            Write(Emitter.GetTypeName(type.Type, false, false,
                                CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
                            Write("__value = ");

                            if (Node.Operand is ElementAccessExpressionSyntax elementAccess)
                            {
                                EmitTree(elementAccess.Expression, cancellationToken);
                                WriteDot();
                                Write(Emitter.GetMethodName(property.GetMethod));
                                WriteOpenParentheses();

                                for (int i = 0; i < elementAccess.ArgumentList.Arguments.Count; i++)
                                {
                                    if (i > 0 && property.IsIndexer)
                                    {
                                        WriteComma();
                                    }

                                    EmitTree(elementAccess.ArgumentList.Arguments[i], cancellationToken);
                                }

                                WriteCloseParentheses();
                            }
                            else if (Node.Operand is IdentifierNameSyntax)
                            {
                                Write(Emitter.GetMethodName(property.GetMethod));
                                WriteOpenCloseParentheses();
                            }

                            WriteSemiColon(true);

                            // SetValue(__value + 1);
                            EmitTree(Node.Operand, cancellationToken);
                            Write("__value ", op, " 1");
                            WriteCloseParentheses();
                            WriteSemiColon(true);

                            Write("return __value;");
                            WriteSemiColon(true);

                            EndBlock(false);
                            Write("()");
                            return;
                        }
                        break;
                }
            }

            Write(Node.OperatorToken.Text);
            EmitTree(Node.Operand, cancellationToken);
        }

        private string GetOperator()
        {
            var op = "";
            switch (Node.Kind())
            {
                case SyntaxKind.PreIncrementExpression:
                    op = "+";
                    break;
                case SyntaxKind.PreDecrementExpression:
                    op = "-";
                    break;
            }

            return op;
        }
    }
}