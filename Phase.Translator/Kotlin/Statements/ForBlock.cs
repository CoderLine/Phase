using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class ForBlock : CommentedNodeEmitBlock<ForStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (DetectSimpleFor(out var type, out var step))
            {
                Write("for");
                WriteOpenParentheses();
                
                Write(Node.Declaration.Variables[0].Identifier.Text);
                
                Write(" in ");

                EmitTree(Node.Declaration.Variables[0].Initializer, cancellationToken);

                switch (type)
                {
                    case SyntaxKind.LessThanExpression:
                        Write(" until ");
                        break;          
                    case SyntaxKind.LessThanOrEqualExpression:
                        Write("..");
                        break;          
                }
                
                EmitTree(((BinaryExpressionSyntax)Node.Condition).Right, cancellationToken);

                if (step != null)
                {
                    Write(" step ");
                    EmitTree(step, cancellationToken);
                }

                
                WriteCloseParentheses();

                if (Node.Statement is BlockSyntax)
                {
                    WriteNewLine();
                }

                EmitTree(Node.Statement, cancellationToken);
            }
            else
            {
                    
                Write("run ");
                BeginBlock();

                if (Node.Declaration != null)
                {
                    EmitTree(Node.Declaration, cancellationToken);
                }
                else if (Node.Initializers.Count > 0)
                {
                    foreach (var initializer in Node.Initializers)
                    {
                        EmitTree(initializer, cancellationToken);
                        WriteSemiColon(true);
                    }
                }


                PushWriter();
                BeginBlock();

                EmitterContext.CurrentForIncrementors.Push(Node.Incrementors);

                if (Node.Statement.Kind() == SyntaxKind.Block)
                {
                    foreach (var statement in ((BlockSyntax)Node.Statement).Statements)
                    {
                        EmitTree(statement, cancellationToken);
                    }
                }
                else
                {
                    // TODO: continue statements can spoil incrementors!
                    EmitTree(Node.Statement, cancellationToken);
                }

                foreach (var incrementor in Node.Incrementors)
                {
                    EmitTree(incrementor, cancellationToken);
                    WriteSemiColon(true);
                }

                EmitterContext.CurrentForIncrementors.Pop();

                EndBlock();
                var body = PopWriter();


                if (EmitterContext.LoopNames.TryGetValue(Node, out var name))
                {
                    Write(name, "@ ");
                    EmitterContext.LoopNames.Remove(Node);
                }

                WriteWhile();
                WriteOpenParentheses();
                EmitTree(Node.Condition, cancellationToken);
                WriteCloseParentheses();

                WriteNewLine();

                Write(body);

                EndBlock();
            }
            
        
        }

        private bool DetectSimpleFor(out SyntaxKind expressionKind, out ExpressionSyntax step)
        {
            expressionKind = SyntaxKind.None;
            step = null;
            if (Node.Declaration == null || Node.Declaration.Variables.Count != 1 || Node.Declaration.Variables[0].Initializer == null)
            {
                return false;
            }

            if (!(Node.Condition is BinaryExpressionSyntax binary))
            {
                return false;
            }

            var binaryKind = binary.Kind();
            switch (binaryKind)
            {
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                    if (!(binary.Left is IdentifierNameSyntax leftIdent))
                    {
                        return false;
                    }

                    if (leftIdent.Identifier.Text != Node.Declaration.Variables[0].Identifier.Text)
                    {
                        return false;
                    }
                        
                    expressionKind = binaryKind;
                    break;
                default:
                    return false;
            }

            
            if (Node.Incrementors.Count > 1)
            {
                return false;
            }

            switch (Node.Incrementors[0].Kind())
            {
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PostIncrementExpression:
                    return binaryKind == SyntaxKind.LessThanExpression || binaryKind == SyntaxKind.LessThanOrEqualExpression;
                case SyntaxKind.AddAssignmentExpression:
                    var addIncr = ((AssignmentExpressionSyntax) Node.Incrementors[0]).Right;
                    var addAssignmentValue = Emitter.GetConstantValue(addIncr);
                    if (addAssignmentValue.HasValue && 1.Equals(addAssignmentValue.Value))
                    {
                        step = null;
                    }
                    else
                    {
                        step = addIncr;
                    }
                    return binaryKind == SyntaxKind.LessThanExpression || binaryKind == SyntaxKind.LessThanOrEqualExpression;
            }

            return false;
        }
    }
}