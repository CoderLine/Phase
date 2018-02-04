using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Phase.Attributes;
using Phase.Translator.Haxe.Expressions;

namespace Phase.Translator.Haxe
{
    public abstract class AbstractHaxeScriptEmitterBlock : AbstractEmitterBlock
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public HaxeEmitterContext EmitterContext { get; set; }

        protected override IWriter Writer => EmitterContext.Writer;
        public HaxeEmitter Emitter => EmitterContext.Emitter;

        public void PushWriter()
        {
            EmitterContext.PushWriter();
        }

        public string PopWriter()
        {
            return EmitterContext.PopWriter();
        }

        protected AbstractHaxeScriptEmitterBlock(HaxeEmitterContext context)
        {
            EmitterContext = context;
        }

        protected AbstractHaxeScriptEmitterBlock EmitTree(SyntaxNode value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expressionBlock = new VisitorBlock(EmitterContext, value);
            expressionBlock.DoEmit(cancellationToken);
            return expressionBlock.FirstBlock;
        }

        private static readonly Regex NewLine = new Regex("\r?\n", RegexOptions.Compiled);

        protected void WriteComments(SyntaxNode node, bool leading = true)
        {
            WriteComments(node, null, leading);
        }

        protected void WriteComments(SyntaxNode node, string documentation, bool leading)
        {
            var trivia = leading
                    ? (node.HasLeadingTrivia ? node.GetLeadingTrivia() : default(SyntaxTriviaList))
                    : (node.HasTrailingTrivia ? node.GetTrailingTrivia() : default(SyntaxTriviaList))
                ;

            var documentationWritten = false;

            if (trivia.Any())
            {
                foreach (var t in trivia)
                {
                    var s = t.ToFullString();

                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        var lines = NewLine.Split(s.Trim());
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("///"))
                            {
                                if (!documentationWritten)
                                {
                                    WriteDocumentation(node, documentation);
                                    documentationWritten = true;
                                }
                            }
                            else if (trimmed.StartsWith("//"))
                            {
                                Write(trimmed);
                                WriteNewLine();
                            }
                        }
                    }
                }
                IsNewLine = true;
            }
        }

        private void WriteDocumentation(SyntaxNode node, string documentation)
        {
            if (string.IsNullOrEmpty(documentation)) return;

            var crefs = new Dictionary<string, ISymbol>();
            foreach (var crefSyntax in node.DescendantNodes(descendIntoTrivia: true).OfType<CrefSyntax>())
            {
                crefs[crefSyntax.ToFullString()] = Emitter.GetSymbolInfo(crefSyntax).Symbol;
            }

            try
            {
                var xml = XDocument.Parse("<doc>" + documentation + "</doc>");
                var doc = xml.Root.FirstNode;

                Write("/**");
                WriteNewLine();

                WriteDocumentation(crefs, doc);

                Write(" */");
                WriteNewLine();
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to process XML documentation '{documentation}'");
            }
        }

        private void WriteDocumentation(Dictionary<string, ISymbol> crefs, XNode node)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Comment:
                    Write(((XComment)node).Value);
                    break;
                case XmlNodeType.Element:
                    var element = (XElement)node;

                    void WriteChildren()
                    {
                        foreach (var child in element.Nodes())
                        {
                            WriteDocumentation(crefs, child);
                        }
                        WriteNewLine();
                    }

                    void WriteDocLines(string text)
                    {
                        var lines = NewLine.Split(text.Trim());
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                            {
                                WriteNewLine();
                                Write(" * ");
                            }
                            Write(lines[i].Trim());
                        }
                    }

                    switch (element.Name.LocalName.ToLowerInvariant())
                    {
                        case "member":
                            foreach (var child in element.Elements())
                            {
                                WriteDocumentation(crefs, child);
                            }
                            break;

                        case "c":
                            Write("`");
                            WriteDocLines(element.Value);
                            Write("`");
                            break;
                        case "code":
                        case "example":
                            Write("```");

                            WriteNewLine();
                            WriteDocLines(element.Value);
                            WriteNewLine();

                            Write("```");
                            break;
                        case "exception":
                            WriteNewLine();
                            Write(" * @throws ");

                            var exceptionType = element.Attribute("cref")?.Value ?? string.Empty;

                            if (crefs.TryGetValue(exceptionType, out var exceptionTypeCref) && exceptionTypeCref != null)
                            {
                                WriteCref(exceptionTypeCref);
                            }
                            else
                            {
                                Write(exceptionType);
                            }

                            WriteSpace();
                            WriteChildren();
                            WriteNewLine();

                            break;
                        case "include":
                            // not supported for now
                            break;
                        case "list":
                            // not supported for now
                            break;
                        case "para":
                            Write("<p>");
                            WriteChildren();
                            Write("</p>");
                            WriteNewLine();
                            break;
                        case "param":
                            Write(" * @param ");

                            var name = element.Attribute("name")?.Value;
                            Write(name);
                            WriteSpace();

                            WriteChildren();

                            break;
                        case "paramref":
                            Write("{@link ");
                            WriteDocLines(element.Value);
                            Write("}");
                            break;
                        case "permission":
                            // not supported for now
                            break;
                        case "remarks":
                            // printed as part of summary
                            break;
                        case "returns":
                            Write(" * @returns ");
                            WriteDocLines(element.Value);
                            WriteNewLine();
                            break;
                        case "see":
                            Write("{@link ");

                            var see = element.Attribute("cref")?.Value ?? string.Empty;
                            if (crefs.TryGetValue(see, out var seeName) && seeName != null)
                            {
                                WriteCref(seeName);
                            }
                            else
                            {
                                WriteDocLines(element.Value);
                            }

                            Write("}");
                            break;
                        case "seealso":
                            Write(" * @see ");

                            var seeAlso = element.Attribute("cref")?.Value ?? string.Empty;
                            if (crefs.TryGetValue(seeAlso, out var seeAlsoName) && seeAlsoName != null)
                            {
                                Write(seeAlsoName);
                            }
                            else
                            {
                                WriteDocLines(element.Value);
                            }

                            WriteNewLine();
                            break;
                        case "summary":
                            Write(" * ");
                            WriteChildren();

                            var remarks = element.Parent.Elements("remarks");
                            foreach (var remark in remarks)
                            {
                                WriteDocLines(remark.Value);
                            }

                            WriteNewLine();
                            break;
                        case "typeparam":

                            Write(" * @param ");

                            var typeParamName = element.Attribute("name")?.Value;
                            Write("<");
                            Write(typeParamName);
                            Write("> ");

                            WriteChildren();
                            WriteNewLine();

                            break;
                        case "typeparamref":
                            Write("{@link ");

                            var typeparamref = element.Attribute("name")?.Value;
                            Write(typeparamref);

                            Write("}"); break;
                        case "value":
                            break;
                        default:
                            Write("<" + element.Name.LocalName + ">");
                            WriteNewLine();
                            WriteChildren();

                            Write("</" + element.Name.LocalName + ">");
                            break;
                    }

                    break;
                case XmlNodeType.Text:
                    WriteDocLines(((XText)node).Value);
                    break;
            }
        }

        private void WriteCref(ISymbol value)
        {
            switch (value.Kind)
            {
                case SymbolKind.Alias:
                case SymbolKind.Assembly:
                case SymbolKind.Label:
                case SymbolKind.NetModule:
                case SymbolKind.NamedType:
                case SymbolKind.Namespace:
                case SymbolKind.RangeVariable:
                case SymbolKind.Discard:
                case SymbolKind.Preprocessing:
                    break;
                case SymbolKind.DynamicType:
                case SymbolKind.ArrayType:
                case SymbolKind.ErrorType:
                case SymbolKind.PointerType:
                    WriteType((ITypeSymbol)value);
                    break;
                case SymbolKind.Event:
                    Write(Emitter.GetEventName((IEventSymbol)value));
                    break;
                case SymbolKind.Field:
                    Write(Emitter.GetFieldName((IFieldSymbol)value));
                    break;
                case SymbolKind.Local:
                    Write(value.Name);
                    break;
                case SymbolKind.Method:
                    Write(Emitter.GetMethodName((IMethodSymbol)value));
                    break;
                case SymbolKind.Parameter:
                    Write(Emitter.GetNameFromAttribute(value) ?? value.Name);
                    break;
                case SymbolKind.Property:
                    Write(Emitter.GetPropertyName((IPropertySymbol)value));
                    break;
                case SymbolKind.TypeParameter:
                    Write("<");
                    Write(value.Name);
                    Write(">");
                    break;
            }
        }

        protected void WriteComments(ISymbol node, bool leading, CancellationToken cancellationToken)
        {
            var documentation = node.GetDocumentationCommentXml(cancellationToken: cancellationToken);

            foreach (var declaration in node.DeclaringSyntaxReferences)
            {
                var syntax = declaration.GetSyntax(cancellationToken);
                WriteComments(syntax, documentation, leading);
                documentation = null;
            }
        }



        protected void WriteMeta(ISymbol node, CancellationToken cancellationToken)
        {
            foreach (var attribute in node.GetAttributes())
            {
                var meta = Emitter.GetHaxeMeta(attribute.AttributeClass);
                if (!string.IsNullOrEmpty(meta))
                {
                    Write(meta);
                    if (!meta.Contains("(") && attribute.ConstructorArguments.Length > 0)
                    {
                        Write("(");

                        for (int i = 0; i < attribute.ConstructorArguments.Length; i++)
                        {
                            if (i > 0) WriteComma();
                            Write(attribute.ConstructorArguments[0].Value);
                        }

                        Write(")");
                    }
                    WriteNewLine();
                }
            }
        }

        protected void WriteComments(ISymbol node, CancellationToken cancellationToken)
        {
            WriteComments(node, true, cancellationToken);
        }

        protected void WriteType(TypeSyntax syntax)
        {
            WriteType(Emitter.GetTypeSymbol(syntax));
        }

        public void WriteWithAutoCast(AutoCastMode mode, ITypeSymbol convertedType, ITypeSymbol type, string result)
        {
            if (mode == AutoCastMode.SkipCast)
            {
                Write(result);
                return;
            }

            // implicit cast
            if (convertedType != null && type != null && !type.Equals(convertedType))
            {
                switch (convertedType.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                        switch (mode)
                        {
                            case AutoCastMode.AddParenthesis:
                                Write("(");
                                Write(result);
                                Write(")");
                                break;
                            default:
                                Write(result);
                                break;
                        }

                        if (Emitter.IsIConvertible(type))
                        {
                            WriteDot();
                            Write("To" + convertedType.Name + "_IFormatProvider");
                            WriteOpenParentheses();
                            Write("null");
                            WriteCloseParentheses();
                        }
                        return;
                }

                if (convertedType.Equals(Emitter.GetPhaseType("Haxe.HaxeInt")))
                {
                    switch (type.SpecialType)
                    {
                        case SpecialType.System_Char:
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                            switch (mode)
                            {
                                case AutoCastMode.AddParenthesis:
                                    Write("(");
                                    Write(result);
                                    Write(")");
                                    break;
                                default:
                                    Write(result);
                                    break;
                            }

                            WriteDot();
                            Write("ToHaxeInt()");
                            return;
                    }

                }

                if (convertedType.Equals(Emitter.GetPhaseType("Haxe.HaxeFloat")))
                {
                    switch (type.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                            switch (mode)
                            {
                                case AutoCastMode.AddParenthesis:
                                    Write("(");
                                    Write(result);
                                    Write(")");
                                    break;
                                default:
                                    Write(result);
                                    break;
                            }

                            WriteDot();
                            Write("ToHaxeFloat()");
                            return;
                    }
                }

                if (type.SpecialType == SpecialType.System_String &&
                    convertedType.Equals(Emitter.GetPhaseType("Haxe.HaxeString")))
                {
                    switch (mode)
                    {
                        case AutoCastMode.AddParenthesis:
                            Write("(");
                            Write(result);
                            Write(")");
                            break;
                        default:
                            Write(result);
                            break;
                    }

                    WriteDot();
                    Write("ToHaxeString()");
                    return;
                }

                if (convertedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    bool needsToEnumerable = false;
                    ForeachMode? foreachMode = null;

                    if (type.Kind == SymbolKind.ArrayType)
                    {
                        needsToEnumerable = true;
                    }

                    if ((foreachMode = Emitter.GetForeachMode(type)) != null)
                    {
                        needsToEnumerable = true;
                    }

                    if (needsToEnumerable)
                    {
                        switch (mode)
                        {
                            case AutoCastMode.AddParenthesis:
                                Write("(");
                                Write(result);
                                Write(")");
                                break;
                            default:
                                Write(result);
                                break;
                        }

                        WriteDot();
                        Write("ToEnumerable()");
                        return;
                    }
                }
            }

            Write(result);
        }


        protected void WriteType(ITypeSymbol type)
        {
            Write(Emitter.GetTypeName(type));
        }

        protected void WriteEventType(INamedTypeSymbol delegateType)
        {
            var delegateMethod = delegateType.DelegateInvokeMethod;

            Write("system.Event");
            if (delegateMethod.ReturnsVoid)
            {
                Write("Action");
            }
            else
            {
                Write("Func");
            }

            if (delegateMethod.Parameters.Length > 0 || !delegateMethod.ReturnsVoid)
            {
                Write(delegateMethod.Parameters.Length, "<");

                for (int i = 0; i < delegateMethod.Parameters.Length; i++)
                {
                    if (i > 0) WriteComma();
                    WriteType(delegateMethod.Parameters[i].Type);
                }

                if (!delegateMethod.ReturnsVoid)
                {
                    if (delegateMethod.Parameters.Length > 0) WriteComma();
                    WriteType(delegateMethod.ReturnType);
                }

                Write(">");
            }
        }


        protected void WriteAccessibility(Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                case Accessibility.Protected:
                    Write("private ");
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Public:
                    Write("public ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        protected void WriteMethodInvocation(IMethodSymbol method,
            ArgumentListSyntax argumentList,
            SyntaxNode callerNode = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteMethodInvocation(method, argumentList?.Arguments.Select(a => new ParameterInvocationInfo(a)), callerNode, cancellationToken);
        }

        protected void WriteMethodInvocation(IMethodSymbol method, IEnumerable<ParameterInvocationInfo> argumentList, SyntaxNode callerNode = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext.IsMethodInvocation = true;
            WriteOpenParentheses();

            if (argumentList != null)
            {
                if (method == null)
                {
                    var args = argumentList.ToArray();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i > 0) WriteComma();
                        EmitTree(args[i].Expression, cancellationToken);
                    }
                }
                else
                {
                    BaseMethodDeclarationSyntax methodDeclaration = null;
                    foreach (var reference in method.DeclaringSyntaxReferences)
                    {
                        methodDeclaration = reference.GetSyntax(cancellationToken) as BaseMethodDeclarationSyntax;
                        if (methodDeclaration != null)
                        {
                            break;
                        }
                    }

                    var arguments = BuildMethodInvocation(method, argumentList);
                    var isFirstParam = true;
                    foreach (var argument in argumentList.Where(a => a.InjectAtBeginning))
                    {
                        if (!isFirstParam) WriteComma();
                        isFirstParam = false;
                        EmitTree(argument.Expression, cancellationToken);
                    }

                    var isRawParams = Emitter.IsRawParams(method);

                    // print expressions
                    for (int i = 0; i < method.Parameters.Length; i++)
                    {
                        if (!isFirstParam) WriteComma();
                        isFirstParam = false;

                        var param = method.Parameters[i];
                        var value = arguments[param.Name].ToArray();
                        if (param.IsParams)
                        {
                            if (value.Length == 1)
                            {
                                var singleParamType = Emitter.GetTypeInfo(value[0]);
                                if (singleParamType.ConvertedType.Equals(param.Type))
                                {
                                    EmitTree(value[0], cancellationToken);
                                }
                                else
                                {
                                    if (!isRawParams) Write("[");
                                    EmitTree(value[0], cancellationToken);
                                    if (!isRawParams) Write("]");
                                }
                            }
                            else
                            {
                                if (!isRawParams) Write("[");
                                for (var j = 0; j < value.Length; j++)
                                {
                                    if (j > 0) WriteComma();
                                    EmitTree(value[j], cancellationToken);
                                }
                                if (!isRawParams) Write("]");
                            }
                        }
                        else
                        {
                            if (value.Length == 1)
                            {
                                EmitTree(value[0], cancellationToken);
                            }
                            else if (param.IsOptional)
                            {
                                if (Emitter.TryGetCallerMemberInfo(param, EmitterContext.CurrentMember, callerNode, out var callerValue))
                                {
                                    Write(callerValue);
                                }
                                else if (methodDeclaration != null)
                                {
                                    var parameterDeclaration = methodDeclaration.ParameterList.Parameters[i].Default.Value;
                                    EmitTree(parameterDeclaration, cancellationToken);
                                }
                                else if (param.HasExplicitDefaultValue && param.ExplicitDefaultValue != null)
                                {
                                    Write(param.ExplicitDefaultValue);
                                }
                                else
                                {
                                    Write("null");
                                }
                            }
                        }
                    }
                }
            }

            WriteCloseParentheses();

            EmitterContext.IsMethodInvocation = false;
        }

        protected static Dictionary<string, IEnumerable<ExpressionSyntax>> BuildMethodInvocation(IMethodSymbol method, IEnumerable<ParameterInvocationInfo> argumentList)
        {
            var arguments = new Dictionary<string, IEnumerable<ExpressionSyntax>>();
            var varArgs = new List<ExpressionSyntax>();
            var varArgsName = string.Empty;
            // fill expected parameters
            foreach (var param in method.Parameters)
            {
                arguments[param.Name] = Enumerable.Empty<ExpressionSyntax>();
            }

            // iterate all actual parameters and fit the into the arguments lookup
            var parameterIndex = 0;
            var isVarArgs = false;
            foreach (var argument in argumentList)
            {
                if (argument.InjectAtBeginning)
                {
                    continue;
                }

                if (argument.Name != null && argument.Name != varArgsName)
                {
                    arguments[argument.Name] = new[] { argument.Expression };
                }
                else if (isVarArgs)
                {
                    varArgs.Add(argument.Expression);
                }
                else if (parameterIndex < method.Parameters.Length)
                {
                    var param = method.Parameters[parameterIndex];
                    if (param.IsParams)
                    {
                        isVarArgs = true;
                        varArgsName = param.Name;
                        arguments[param.Name] = varArgs;
                        varArgs.Add(argument.Expression);
                    }
                    else
                    {
                        arguments[param.Name] = new[] { argument.Expression };
                        parameterIndex++;
                    }
                }
            }

            return arguments;
        }

        protected async Task WriteParameterDeclarations(ImmutableArray<IParameterSymbol> methodParameters, CancellationToken cancellationToken)
        {
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var param = methodParameters[i];

                Write(param.Name);
                WriteSpace();
                WriteColon();
                if (param.RefKind != RefKind.None)
                {
                    throw new PhaseCompilerException("ref parameters are not supported");
                    Write("CsRef<");
                }
                WriteType(methodParameters[i].Type);
                if (param.RefKind != RefKind.None)
                {
                    Write(">");
                }

                if (param.IsOptional)
                {
                    Write(" = ");

                    var parameterSyntax = (ParameterSyntax)await methodParameters[i].DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);
                    EmitTree(parameterSyntax.Default.Value, cancellationToken);
                }
            }
        }

    }

    public class ParameterInvocationInfo
    {
        public bool InjectAtBeginning { get; set; }
        public string Name { get; set; }
        public ExpressionSyntax Expression { get; set; }

        public ParameterInvocationInfo(ArgumentSyntax syntax)
        {
            if (syntax.NameColon != null)
            {
                Name = syntax.NameColon.Name.Identifier.ValueText;
            }
            Expression = syntax.Expression;
        }
        public ParameterInvocationInfo(ExpressionSyntax expression, bool injected = false)
        {
            Expression = expression;
            InjectAtBeginning = injected;
        }
    }

    public abstract class AbstractHaxeScriptEmitterBlock<T> : AbstractHaxeScriptEmitterBlock where T : SyntaxNode
    {
        public T Node { get; set; }

        protected AbstractHaxeScriptEmitterBlock() : base(null)
        {
        }

        public virtual void Emit(HaxeEmitterContext context, T node, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext = context;
            Node = node;
            Emit(cancellationToken);
        }
    }
}