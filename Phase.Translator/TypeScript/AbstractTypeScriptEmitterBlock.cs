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
using Phase.Translator.TypeScript.Expressions;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript
{
    public abstract class AbstractTypeScriptEmitterBlock : AbstractEmitterBlock
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public TypeScriptEmitterContext EmitterContext { get; set; }

        protected override IWriter Writer => EmitterContext.Writer;
        public TypeScriptEmitter Emitter => EmitterContext.Emitter;


        public void PushWriter()
        {
            EmitterContext.PushWriter();
        }

        public string PopWriter()
        {
            return EmitterContext.PopWriter();
        }

        protected AbstractTypeScriptEmitterBlock(TypeScriptEmitterContext context)
        {
            EmitterContext = context;
        }

        protected override void BeginEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.BeginEmit(cancellationToken);
        }

        protected void WriteImport(ITypeSymbol type)
        {
            string prefix;
            if (type.ContainingAssembly.Equals(Emitter.GetPhaseType("System.String").ContainingAssembly) ||
                type.ContainingAssembly.Equals(Emitter.GetPhaseType("System.Action").ContainingAssembly))
            {
                prefix = Emitter.Compiler.Options.CoreImportAlias;
            }
            else if (type.DeclaringSyntaxReferences.IsEmpty || Emitter.IsExternal(type) ||
                     !type.ContainingAssembly.Equals(Emitter.Compiler.Translator.Compilation.Assembly))
            {
                prefix = Emitter.Compiler.Options.ExternImportAlias;
            }
            else
            {
                prefix = Emitter.Compiler.Options.AssemblyImportAlias;
            }

            prefix += "/";
            
            Write("import { ", Emitter.GetTypeName(type, true, true), " } from '", prefix,
                Emitter.GetFileName(type, false, '/'),
                "';");
            WriteNewLine();
        }

        private bool IsBuiltInType(ITypeSymbol type)
        {
            return type.DeclaringSyntaxReferences.Length == 0;
        }

        protected AbstractTypeScriptEmitterBlock EmitTree(SyntaxNode value,
            CancellationToken cancellationToken = default(CancellationToken))
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
                var isCodeComment = false;
                var codeCommentIndent = "";

                foreach (var t in trivia)
                {
                    var s = t.ToFullString();

                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        var lines = NewLine.Split(s.Trim(' ', '\t', '\r', '\n'));
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("// </code>"))
                            {
                                isCodeComment = false;
                            }
                            else if (isCodeComment && trimmed.StartsWith("// "))
                            {
                                Write(codeCommentIndent, trimmed.Substring(3));
                                WriteNewLine();
                            }
                            else if (isCodeComment && trimmed.StartsWith("//"))
                            {
                                Write(codeCommentIndent, trimmed.Substring(2));
                                WriteNewLine();
                            }
                            else if (trimmed.StartsWith("///"))
                            {
                                if (!documentationWritten)
                                {
                                    WriteDocumentation(node, documentation);
                                    documentationWritten = true;
                                }
                            }
                            else if (trimmed.StartsWith("// <code>"))
                            {
                                isCodeComment = true;
                                codeCommentIndent =
                                    trimmed.Substring(0, trimmed.IndexOf("//", StringComparison.Ordinal));
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
                    Write(((XComment) node).Value);
                    break;
                case XmlNodeType.Element:
                    var element = (XElement) node;

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

                            if (crefs.TryGetValue(exceptionType, out var exceptionTypeCref) &&
                                exceptionTypeCref != null)
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
                            Write(" {@link ");
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
                            Write(" {@link ");

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
                            Write(" {@link ");
                            Write(" {@link ");

                            var typeparamref = element.Attribute("name")?.Value;
                            Write(typeparamref);

                            Write("}");
                            break;
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
                    WriteDocLines(((XText) node).Value);
                    break;
            }
        }

        protected void ApplyExpressions(CodeTemplate template, IEnumerable<IParameterSymbol> parameters,
            Dictionary<string, IEnumerable<ExpressionSyntax>> methodInvocation, CancellationToken cancellationToken)
        {
            foreach (var param in parameters)
            {
                if (template.Variables.TryGetValue(param.Name, out var variable))
                {
                    var values = methodInvocation[param.Name].ToArray();
                    PushWriter();
                    if (param.IsParams)
                    {
                        if (values.Length == 1)
                        {
                            var singleParamType = Emitter.GetTypeInfo(values[0]);

                            if (singleParamType.ConvertedType.Equals(param.Type))
                            {
                                EmitTree(values[0], cancellationToken);
                            }
                            else
                            {
                                if (variable.Modifier != "raw")
                                {
                                    Write("[");
                                }

                                EmitTree(values[0], cancellationToken);
                                if (variable.Modifier != "raw")
                                {
                                    Write("]");
                                }
                            }
                        }
                        else
                        {
                            if (variable.Modifier != "raw")
                            {
                                Write("[");
                            }

                            for (int j = 0; j < values.Length; j++)
                            {
                                if (j > 0) WriteComma();
                                EmitTree(values[0], cancellationToken);
                            }

                            if (variable.Modifier != "raw")
                            {
                                Write("]");
                            }
                        }
                    }
                    else
                    {
                        if (variable.Modifier == "raw")
                        {
                            var constValue = Emitter.GetConstantValue(values[0], cancellationToken);
                            if (constValue.HasValue)
                            {
                                Write(constValue);
                            }
                            else
                            {
                                EmitTree(values[0], cancellationToken);
                            }
                        }
                        else
                        {
                            EmitTree(values[0], cancellationToken);
                        }
                    }

                    var paramOutput = PopWriter();
                    variable.RawValue = paramOutput;
                }
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
                    WriteType((ITypeSymbol) value);
                    break;
                case SymbolKind.Event:
                    Write(Emitter.GetEventName((IEventSymbol) value));
                    break;
                case SymbolKind.Field:
                    Write(Emitter.GetFieldName((IFieldSymbol) value));
                    break;
                case SymbolKind.Local:
                    Write(value.Name);
                    break;
                case SymbolKind.Method:
                    Write(EmitterContext.GetMethodName((IMethodSymbol) value));
                    break;
                case SymbolKind.Parameter:
                    Write(Emitter.GetNameFromAttribute(value) ?? value.Name);
                    break;
                case SymbolKind.Property:
                    Write(Emitter.GetPropertyName((IPropertySymbol) value));
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
            foreach (var attribute in Emitter.GetAttributes(node))
            {
                string meta;
                bool printArguments;
                if (attribute.AttributeClass.Equals(Emitter.GetPhaseType("Phase.Attributes.MetaAttribute")))
                {
                    meta = attribute.ConstructorArguments[0].Value.ToString();
                    printArguments = false;
                }
                else
                {
                    meta = Emitter.GetMeta(attribute.AttributeClass);
                    printArguments = true;
                }

                if (!string.IsNullOrEmpty(meta))
                {
                    Write(meta);
                    if (printArguments && !meta.Contains("(") && attribute.ConstructorArguments.Length > 0)
                    {
                        Write("(");

                        for (int i = 0; i < attribute.ConstructorArguments.Length; i++)
                        {
                            if (i > 0) WriteComma();

                            var argument = attribute.ConstructorArguments[i];

                            if (argument.Type.TypeKind == TypeKind.Array)
                            {
                                for (var j = 0; j < argument.Values.Length; j++)
                                {
                                    if (j > 0) WriteComma();
                                    WriteMetaArgument(argument.Values[j].Value, argument.Values[j].Type);
                                }
                            }
                            else
                            {
                                WriteMetaArgument(argument.Value, argument.Type);
                            }
                        }

                        Write(")");
                    }

                    WriteNewLine();
                }
            }
        }

        private void WriteMetaArgument(object argumentValue, ITypeSymbol argumentType)
        {
            switch (argumentType.SpecialType)
            {
                case SpecialType.System_Boolean:
                    Write((bool) argumentValue ? "true" : "false");
                    break;
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    Write((int) argumentValue);
                    break;
                case SpecialType.System_Decimal:
                    Write((decimal) argumentValue);
                    break;
                case SpecialType.System_Single:
                    Write((float) argumentValue);
                    break;
                case SpecialType.System_Double:
                    Write((double) argumentValue);
                    break;
                case SpecialType.System_String:
                    Write("\"" + argumentValue + "\"");
                    break;
                default:
                    throw new PhaseCompilerException(
                        "Only built-in types supported for meta constructor");
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

        protected void WriteDefaultInitializers(INamedTypeSymbol type, bool forStaticMembers,
            CancellationToken cancellationToken)
        {
            foreach (var members in type.GetMembers().Where(f => f.IsStatic == forStaticMembers))
            {
                bool hasInitializer = true;
                ITypeSymbol memberType = null;
                string memberName = string.Empty;
                switch (members.Kind)
                {
                    case SymbolKind.Field:
                        foreach (var fieldReference in members.DeclaringSyntaxReferences)
                        {
                            if (fieldReference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax declaration)
                            {
                                hasInitializer = declaration.Initializer != null;
                                memberType = ((IFieldSymbol) members).Type;
                                memberName = Emitter.GetFieldName((IFieldSymbol) members);
                                break;
                            }
                        }

                        break;
                    case SymbolKind.Property:
                        var prop = (IPropertySymbol) members;
                        if (!Emitter.NeedsDefaultInitializer(prop))
                        {
                            continue;
                        }

                        memberType = ((IPropertySymbol) members).Type;

                        if (!Emitter.IsAutoProperty(prop) && prop.SetMethod == null)
                        {
                            var backingField = prop.ContainingType
                                .GetMembers()
                                .OfType<IFieldSymbol>()
                                .FirstOrDefault(f => f.AssociatedSymbol == prop);
                            memberName = Emitter.GetFieldName(backingField);
                        }
                        else
                        {
                            memberName = Emitter.GetPropertyName(prop);
                        }

                        hasInitializer = false;
                        break;
                    default:
                        continue;
                }


                if (!hasInitializer)
                {
                    var defaultValue = Emitter.GetDefaultValue(memberType);
                    if (!members.IsStatic)
                    {
                        Write("this.");
                    }
                    else
                    {
                        WriteType(type);
                        Write(".");
                    }

                    Write(memberName);
                    Write(" = ");
                    Write(defaultValue);
                    WriteSemiColon(true);
                }
            }
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
                convertedType = convertedType.TypeKind == TypeKind.Array
                    ? ((IArrayTypeSymbol) convertedType).ElementType
                    : convertedType;
                type = type.TypeKind == TypeKind.Array
                    ? ((IArrayTypeSymbol) type).ElementType
                    : type;

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

                        if (Emitter.IsIConvertible(type) && Emitter.AreTypeMethodsRedirected(type, out var redirect))
                        {
                            if (redirect.StartsWith("ph."))
                            {
                                EmitterContext.NeedsPhaseImport = true;
                            }

                            Write(redirect);
                            WriteDot();
                            Write("to" + convertedType.Name);
                            WriteOpenParentheses();
                            Write(result);
                            WriteCloseParentheses();
                        }
                        else if (type.TypeKind == TypeKind.Enum)
                        {
                            WriteType(type);
                            WriteDot();
                            Write("to" + convertedType.Name);
                            WriteOpenParentheses();
                            Write(result);
                            WriteCloseParentheses();
                        }
                        else
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

                            if (Emitter.IsIConvertible(type))
                            {
                                WriteDot();
                                Write("to" + convertedType.Name);
                                WriteOpenParentheses();
                                WriteCloseParentheses();
                            }
                        }

                        return;
                }

                if (convertedType.Equals(Emitter.GetPhaseType("TypeScript.TypeScriptInt")))
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
                            Write("toTypeScriptInt()");
                            return;
                    }
                }

                if (convertedType.Equals(Emitter.GetPhaseType("TypeScript.TypeScriptFloat")))
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
                            Write("toTypeScriptFloat()");
                            return;
                    }
                }

                if (type.SpecialType == SpecialType.System_String &&
                    convertedType.Equals(Emitter.GetPhaseType("TypeScript.TypeScriptString")))
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
                    Write("toTypeScriptString()");
                    return;
                }

                if (convertedType.OriginalDefinition.SpecialType ==
                    SpecialType.System_Collections_Generic_IEnumerable_T)
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
                        Write("toEnumerable()");
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

        protected void WriteEventType(INamedTypeSymbol delegateType, bool includeGenerics = true)
        {
            var delegateMethod = delegateType.DelegateInvokeMethod;

            EmitterContext.NeedsPhaseImport = true;
            Write("ph.Event");
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

                if (includeGenerics)
                {
                    Write("<");
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
        }


        protected void WriteAccessibility(Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    Write("private ");
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Public:
                    Write("public ");
                    break;
                case Accessibility.Protected:
                    Write("protected ");
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
            WriteMethodInvocation(method, argumentList?.Arguments.Select(a => new ParameterInvocationInfo(a)),
                callerNode, cancellationToken);
        }

        protected void WriteMethodInvocation(IMethodSymbol method, IEnumerable<ParameterInvocationInfo> argumentList,
            SyntaxNode callerNode = null, CancellationToken cancellationToken = default(CancellationToken))
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
                                if (Emitter.AreTypesEqual(singleParamType.ConvertedType, param.Type))
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
                                if (EmitterContext.TryGetCallerMemberInfo(param, EmitterContext.CurrentMember,
                                    callerNode,
                                    out var callerValue))
                                {
                                    Write(callerValue);
                                }
                                else if (methodDeclaration != null)
                                {
                                    var parameterDeclaration =
                                        methodDeclaration.ParameterList.Parameters[i].Default.Value;
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

        protected static Dictionary<string, IEnumerable<ExpressionSyntax>> BuildMethodInvocation(IMethodSymbol method,
            IEnumerable<ParameterInvocationInfo> argumentList)
        {
            return BuildMethodInvocation(method.Parameters, argumentList);
        }

        protected static Dictionary<string, IEnumerable<ExpressionSyntax>> BuildMethodInvocation(
            ImmutableArray<IParameterSymbol> parameters,
            IEnumerable<ParameterInvocationInfo> argumentList)
        {
            var arguments = new Dictionary<string, IEnumerable<ExpressionSyntax>>();
            var varArgs = new List<ExpressionSyntax>();
            var varArgsName = string.Empty;
            // fill expected parameters
            foreach (var param in parameters)
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
                    arguments[argument.Name] = new[] {argument.Expression};
                }
                else if (isVarArgs)
                {
                    varArgs.Add(argument.Expression);
                }
                else if (parameterIndex < parameters.Length)
                {
                    var param = parameters[parameterIndex];
                    if (param.IsParams)
                    {
                        isVarArgs = true;
                        varArgsName = param.Name;
                        arguments[param.Name] = varArgs;
                        varArgs.Add(argument.Expression);
                    }
                    else
                    {
                        arguments[param.Name] = new[] {argument.Expression};
                        parameterIndex++;
                    }
                }
            }

            return arguments;
        }


        protected void WriteES5PropertyDeclarations(INamedTypeSymbol type)
        {
            if (Emitter.IsAbstract(type) || type.DeclaredAccessibility != Accessibility.Public) return;
            var prototype = Emitter.GetTypeName(type, true, true) + ".prototype";
            foreach (var property in type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsIndexer && !Emitter.IsAutoProperty(p)))
            {
                Write(
                    "untyped Object.defineProperty(", prototype,
                    ", \"", Emitter.GetPropertyName(property), "\", {"
                );
                if (property.GetMethod != null)
                {
                    Write("get: ", prototype, ".", EmitterContext.GetMethodName(property.GetMethod));
                }

                if (property.SetMethod != null)
                {
                    if (property.GetMethod != null)
                    {
                        Write(",");
                    }

                    Write("set: ", prototype, ".", EmitterContext.GetMethodName(property.SetMethod));
                }

                Write("});");
                WriteNewLine();
            }
        }

        protected void WriteParameterDeclarations(ImmutableArray<IParameterSymbol> methodParameters,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var param = methodParameters[i];

                Write(param.Name);
                WriteColon();
                if (param.RefKind != RefKind.None)
                {
                    throw new PhaseCompilerException("ref parameters are not supported");
                    Write("CsRef<");
                }

                Write(Emitter.GetTypeNameWithNullability(methodParameters[i].Type));
                EmitterContext.ImportType(methodParameters[i].Type);

                if (param.RefKind != RefKind.None)
                {
                    Write(">");
                }

                if (param.IsOptional && param.ContainingType.TypeKind != TypeKind.Interface)
                {
                    Write(" = ");

                    var parameterSyntax = (ParameterSyntax) methodParameters[i].DeclaringSyntaxReferences.First()
                        .GetSyntax(cancellationToken);
                    EmitTree(parameterSyntax.Default.Value, cancellationToken);
                }
            }
        }

        protected AutoCastMode WriteConstant(IFieldSymbol constField)
        {
            switch (constField.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    Write((bool) constField.ConstantValue ? "true" : "false");
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Char:
                    Write((int) (char) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_SByte:
                    Write((sbyte) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Byte:
                    Write((byte) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Int16:
                    Write((short) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_UInt16:
                    Write((ushort) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Int32:
                    Write((int) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_UInt32:
                    Write((uint) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Int64:
                    Write((long) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_UInt64:
                    Write((ulong) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Decimal:
                    Write((decimal) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Single:
                    Write((float) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Double:
                    Write((double) constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_String:
                    Write("\"" + constField.ConstantValue + "\"");
                    return AutoCastMode.SkipCast;
                default:
                    if (constField.Type.TypeKind == TypeKind.Enum)
                    {
                        Write(Emitter.GetTypeName(constField.ContainingType) + "." +
                              EmitterContext.GetSymbolName(constField));
                        return AutoCastMode.SkipCast;
                    }

                    Log.Error("Unknown constant type: " + constField.Type);
                    throw new PhaseCompilerException("Unknown constant type: " + constField.Type);
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

    public abstract class AbstractTypeScriptEmitterBlock<T> : AbstractTypeScriptEmitterBlock where T : SyntaxNode
    {
        public T Node { get; set; }

        protected AbstractTypeScriptEmitterBlock() : base(null)
        {
        }

        public virtual void Emit(TypeScriptEmitterContext context, T node,
            CancellationToken cancellationToken = default(CancellationToken))
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