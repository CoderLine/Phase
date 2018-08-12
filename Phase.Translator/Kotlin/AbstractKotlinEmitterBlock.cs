using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Phase.Attributes;
using Phase.Translator.Kotlin.Expressions;

namespace Phase.Translator.Kotlin
{
    public abstract class AbstractKotlinEmitterBlock : AbstractEmitterBlock
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public KotlinEmitterContext EmitterContext { get; set; }

        protected override IWriter Writer => EmitterContext.Writer;
        public KotlinEmitter Emitter => EmitterContext.Emitter;


        public void PushWriter()
        {
            EmitterContext.PushWriter();
        }

        public string PopWriter()
        {
            return EmitterContext.PopWriter();
        }

        protected AbstractKotlinEmitterBlock(KotlinEmitterContext context)
        {
            EmitterContext = context;
        }


        protected AbstractKotlinEmitterBlock EmitTree(SyntaxNode value, CancellationToken cancellationToken = default(CancellationToken))
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


            // Automatic Delegate wrapping
            if (type == null && convertedType?.TypeKind == TypeKind.Delegate)
            {
                // TODO: can be changed to this::Method or Class::Method
                WriteOpenParentheses();

                var invoke = ((INamedTypeSymbol)convertedType).DelegateInvokeMethod;
                var isFirst = true;
                foreach (var param in invoke.Parameters)
                {
                    if (!isFirst) WriteComma();
                    isFirst = false;
                    Write(param.Name);
                }

                WriteCloseParentheses();

                Write("->");

                Write(result);

                WriteOpenParentheses();

                isFirst = true;
                foreach (var param in invoke.Parameters)
                {
                    if (!isFirst) WriteComma();
                    isFirst = false;
                    Write(param.Name);
                }

                WriteCloseParentheses();

                return;
            }

            // implicit cast
            if (convertedType != null && type != null && !type.Equals(convertedType))
            {
                convertedType = convertedType.TypeKind == TypeKind.Array
                    ? ((IArrayTypeSymbol)convertedType).ElementType
                    : convertedType;
                type = type.TypeKind == TypeKind.Array
                    ? ((IArrayTypeSymbol)type).ElementType
                    : type;

                if (convertedType.TypeKind == TypeKind.Enum)
                {
                    switch (convertedType.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                            WriteType(convertedType);
                            WriteDot();
                            Write("fromValue(", result, ")");
                            return;
                    }
                }

                if (type.SpecialType == SpecialType.System_Int32)
                {
                    switch (convertedType.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt16:
                            Write(result);
                            Write(".to");
                            Write(Emitter.GetTypeName(convertedType, true, true, false));
                            WriteOpenCloseParentheses();
                            return;
                    }
                }

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

                        if (Emitter.IsIConvertible(type))
                        {
                            WriteDot();
                            Write("to");
                            Write(Emitter.GetTypeName(convertedType, true, true, false));
                            WriteOpenCloseParentheses();
                        }
                        else if (type.TypeKind == TypeKind.Enum)
                        {
                            WriteDot();
                            Write("getValue()");
                        }
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
                        switch (foreachMode)
                        {
                            case ForeachMode.Native:
                                needsToEnumerable = false;
                                break;
                            default:
                                needsToEnumerable = true;
                                break;
                        }
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

        protected void WriteEventType(INamedTypeSymbol delegateType, bool includeTypeParameters = true)
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
                Write(delegateMethod.Parameters.Length);
                if (includeTypeParameters)
                {
                    Write("<");
                    for (int i = 0; i < delegateMethod.Parameters.Length; i++)
                    {
                        if (i > 0) WriteComma();
                        var typeName = Emitter.GetTypeName(delegateMethod.Parameters[i].Type, false, false);
                        Write(typeName);
                    }

                    if (!delegateMethod.ReturnsVoid)
                    {
                        if (delegateMethod.Parameters.Length > 0) WriteComma();
                        var typeName = Emitter.GetTypeName(delegateMethod.ReturnType, false, false);
                        Write(typeName);
                    }

                    Write(">");
                }
            }

        }

        protected void WriteDefaultFileHeader()
        {
        }

        protected void WriteImport(string currentPackage, ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Void) return;
            var fullName = Emitter.GetTypeName(type, false, true);
            var ns = Emitter.GetNamespace(type);
            if (ns == currentPackage) return;
            Write("import ", fullName, ";");
            WriteNewLine();
        }


        protected void WriteAccessibility(Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    Write("private ");
                    break;
                case Accessibility.Protected:
                    Write("protected ");
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                    Write("public ");
                    break;
                case Accessibility.Internal:
                    Write("public ");
                    break;
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
                                    EmitTree(value[0], cancellationToken);
                                }
                            }
                            else
                            {
                                for (var j = 0; j < value.Length; j++)
                                {
                                    if (j > 0) WriteComma();
                                    EmitTree(value[j], cancellationToken);
                                }
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

        protected void WriteParameterDeclarations(ImmutableArray<IParameterSymbol> methodParameters, CancellationToken cancellationToken)
        {
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var param = methodParameters[i];
                WriteSpace();
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
            }
        }

        protected AutoCastMode WriteConstant(IFieldSymbol constField)
        {
            switch (constField.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    Write((bool)constField.ConstantValue ? "true" : "false");
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    Write((int)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Decimal:
                    Write((decimal)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Single:
                    Write((float)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Double:
                    Write((double)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_String:
                    Write("\"" + constField.ConstantValue + "\"");
                    return AutoCastMode.SkipCast;
                default:
                    if (constField.Type.TypeKind == TypeKind.Enum)
                    {
                        Write(constField.Name);
                        return AutoCastMode.SkipCast;
                    }

                    Log.Error("Unknown constant type: " + constField.Type);
                    throw new PhaseCompilerException("Unknown constant type: " + constField.Type);
            }
        }
    }

    public abstract class AbstractKotlinEmitterBlock<T> : AbstractKotlinEmitterBlock where T : SyntaxNode
    {
        public T Node { get; set; }

        protected AbstractKotlinEmitterBlock() : base(null)
        {
        }

        public virtual void Emit(KotlinEmitterContext context, T node, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext = context;
            Node = node;
            Emit(cancellationToken);
        }

        protected override void BeginEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.BeginEmit(cancellationToken);
            EmitterContext.BeginEmit(Node);
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            EmitterContext.EndEmit(Node);
        }
    }
}