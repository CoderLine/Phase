using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Haxe.Expressions;

namespace Phase.Translator.Haxe
{
    public class VisitorBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly SyntaxNode _node;
        public AbstractHaxeScriptEmitterBlock FirstBlock { get; set; }

        public VisitorBlock(HaxeEmitterContext context, SyntaxNode node)
            : base(context)
        {
            _node = node;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var visitor = new Visitor(EmitterContext, cancellationToken);
            visitor.Visit(_node);
            FirstBlock = visitor.FirstBlock;
        }

        private class Visitor : CSharpSyntaxWalker
        {
            private readonly HaxeEmitterContext _context;
            private readonly CancellationToken _cancellationToken;

            public AbstractHaxeScriptEmitterBlock FirstBlock { get; set; }

            public Visitor(HaxeEmitterContext context, CancellationToken cancellationToken)
            {
                _context = context;
                _cancellationToken = cancellationToken;
            }

            private void Emit<TBlock, TSyntax>(TSyntax syntax)
                where TBlock : AbstractHaxeScriptEmitterBlock<TSyntax>, new()
                where TSyntax : SyntaxNode
            {
                var block = new TBlock();
                if (FirstBlock == null) FirstBlock = block;
                block.Emit(_context, syntax, _cancellationToken);
            }

            #region Statements

            public override void VisitBlock(BlockSyntax node)
            {
                Emit<Block, BlockSyntax>(node);
            }

            public override void VisitBreakStatement(BreakStatementSyntax node)
            {
                Emit<BreakBlock, BreakStatementSyntax>(node);
            }

            public override void VisitCheckedStatement(CheckedStatementSyntax node)
            {
                Emit<CheckedBlock, CheckedStatementSyntax>(node);
            }

            public override void VisitContinueStatement(ContinueStatementSyntax node)
            {
                Emit<ContinueBlock, ContinueStatementSyntax>(node);
            }

            public override void VisitDoStatement(DoStatementSyntax node)
            {
                Emit<DoWhileBlock, DoStatementSyntax>(node);
            }

            public override void VisitEmptyStatement(EmptyStatementSyntax node)
            {
                Emit<EmptyBlock, EmptyStatementSyntax>(node);
            }

            public override void VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                Emit<ExpressionBlock, ExpressionStatementSyntax>(node);
            }

            public override void VisitFixedStatement(FixedStatementSyntax node)
            {
                Emit<FixedBlock, FixedStatementSyntax>(node);
            }

            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                Emit<ForEachBlock, ForEachStatementSyntax>(node);
            }

            public override void VisitForStatement(ForStatementSyntax node)
            {
                Emit<ForBlock, ForStatementSyntax>(node);
            }

            public override void VisitGotoStatement(GotoStatementSyntax node)
            {
                Emit<GotoBlock, GotoStatementSyntax>(node);
            }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                Emit<IfBlock, IfStatementSyntax>(node);
            }

            public override void VisitLabeledStatement(LabeledStatementSyntax node)
            {
                Emit<LabeledBlock, LabeledStatementSyntax>(node);
            }

            public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                Emit<LocalDeclarationBlock, LocalDeclarationStatementSyntax>(node);
            }

            public override void VisitLockStatement(LockStatementSyntax node)
            {
                Emit<LockBlock, LockStatementSyntax>(node);
            }

            public override void VisitReturnStatement(ReturnStatementSyntax node)
            {
                Emit<ReturnBlock, ReturnStatementSyntax>(node);
            }

            public override void VisitSwitchStatement(SwitchStatementSyntax node)
            {
                Emit<SwitchBlock, SwitchStatementSyntax>(node);
            }

            public override void VisitThrowStatement(ThrowStatementSyntax node)
            {
                Emit<ThrowBlock, ThrowStatementSyntax>(node);
            }

            public override void VisitTryStatement(TryStatementSyntax node)
            {
                Emit<TryBlock, TryStatementSyntax>(node);
            }

            public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
            {
                Emit<UnsafeBlock, UnsafeStatementSyntax>(node);
            }

            public override void VisitUsingStatement(UsingStatementSyntax node)
            {
                Emit<UsingBlock, UsingStatementSyntax>(node);
            }

            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                Emit<WhileBlock, WhileStatementSyntax>(node);
            }

            public override void VisitYieldStatement(YieldStatementSyntax node)
            {
                Emit<YieldBlock, YieldStatementSyntax>(node);
            }

            #endregion

            #region Expressions

            public override void VisitQualifiedName(QualifiedNameSyntax node)
            {
                Emit<QualifiedNameBlock, QualifiedNameSyntax>(node);
            }

            public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
            {
                Emit<InterpolatedStringExpressionBlock, InterpolatedStringExpressionSyntax>(node);
            }

            public override void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            {
                Emit<ParenthesizedExpressionBlock, ParenthesizedExpressionSyntax>(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                Emit<BinaryExpressionBlock, BinaryExpressionSyntax>(node);
            }

            public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
            {
                Emit<PrefixUnaryExpressionBlock, PrefixUnaryExpressionSyntax>(node);
            }

            public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
            {
                Emit<PostfixUnaryExpressionBlock, PostfixUnaryExpressionSyntax>(node);
            }

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                Emit<LiteralExpressionBlock, LiteralExpressionSyntax>(node);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                Emit<AssignmentExpressionBlock, AssignmentExpressionSyntax>(node);
            }

            public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                Emit<VariableDeclarationBlock, VariableDeclarationSyntax>(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                Emit<InvocationExpressionBlock, InvocationExpressionSyntax>(node);
            }

            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                Emit<ObjectCreationExpressionBlock, ObjectCreationExpressionSyntax>(node);
            }

            public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
            {
                Emit<ArrayCreationExpressionBlock, ArrayCreationExpressionSyntax>(node);
            }

            public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
            {
                Emit<ImplicitArrayCreationExpressionBlock, ImplicitArrayCreationExpressionSyntax>(node);
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                Emit<IdentifierBlock, IdentifierNameSyntax>(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                Emit<MemberAccessExpressionBlock, MemberAccessExpressionSyntax>(node);
            }

            public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            {
                Emit<ConditionalAccessExpressionBlock, ConditionalAccessExpressionSyntax>(node);
            }

            public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
            {
                Emit<ElementAccessExpressionBlock, ElementAccessExpressionSyntax>(node);
            }

            public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                Emit<ConditionalExpressionBlock, ConditionalExpressionSyntax>(node);
            }

            public override void VisitThisExpression(ThisExpressionSyntax node)
            {
                Emit<ThisExpressionBlock, ThisExpressionSyntax>(node);
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                Emit<SimpleLambdaExpressionBlock, SimpleLambdaExpressionSyntax>(node);
            }

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                Emit<ParenthesizedLambdaExpressionBlock, ParenthesizedLambdaExpressionSyntax>(node);
            }

            public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
            {
                Emit<MemberBindingExpressionBlock, MemberBindingExpressionSyntax>(node);
            }

            public override void VisitCastExpression(CastExpressionSyntax node)
            {
                Emit<CastExpressionBlock, CastExpressionSyntax>(node);
            }

            public override void VisitInitializerExpression(InitializerExpressionSyntax node)
            {
                Emit<InitializerExpressionBlock, InitializerExpressionSyntax>(node);
            }

            public override void VisitDefaultExpression(DefaultExpressionSyntax node)
            {
                Emit<DefaultExpressionBlock, DefaultExpressionSyntax>(node);
            }

            public override void VisitBaseExpression(BaseExpressionSyntax node)
            {
                Emit<BaseExpressionBlock, BaseExpressionSyntax>(node);
            }

            public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
            {
                Emit<TypeOfExpressionBlock, TypeOfExpressionSyntax>(node);
            }

            public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
            {
                Emit<AnonymousObjectCreationExpressionBlock, AnonymousObjectCreationExpressionSyntax>(node);
            }

            #endregion
        }
    }
}