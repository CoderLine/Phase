using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class PrefixUnaryExpressionBlock : AbstractJavaEmitterBlock<PrefixUnaryExpressionSyntax>
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
                        // (new system.Func<int> 
                        //  {
                        //      public int invoke() 
                        //      {
                        //          int __value = getValue();
                        //          SetValue(__value + 1);
                        //          return val;
                        //      }
                        //  }).invoke()

                        var property = ((IPropertySymbol)operandSymbol.Symbol);
                        if (!string.IsNullOrEmpty(op))
                        {
                            //  (new system.Func<int> 
                            var typeName = Emitter.GetTypeName(type.Type, false, false);
                            Write("(new system.Func<", typeName, ">()");
                            WriteNewLine();
                            BeginBlock();

                            //  public int invoke() 
                            Write("public ", typeName, " invoke()");
                            WriteNewLine();
                            BeginBlock();

                            // int __value = getValue();
                            Write(Emitter.GetTypeName(type.Type, false, false));
                            Write(" __value = ");

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

                            if (type.Type.TypeKind == TypeKind.Enum)
                            {
                                Write(Emitter.GetTypeName(type.Type, false, false));
                                Write(".fromValue(__value.getValue() ", op, " 1)");
                            }
                            else
                            {
                                Write("__value ", op, " 1");
                            }

                            WriteCloseParentheses();
                            WriteSemiColon(true);

                            Write("return __value");
                            WriteSemiColon(true);

                            EndBlock();
                            EndBlock(false);
                            Write(").invoke()");
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