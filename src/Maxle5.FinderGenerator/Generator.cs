using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Maxle5.FinderGenerator
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        private readonly Queue<char> _variableNames = new(new[]
        {
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','v','w','x','y','z'
        });

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG
            // If you want to debug the Source Generator, please uncomment the below code.
            //if (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Diagnostics.Debugger.Launch();
            //}
#endif

            // Register the marker attribute source
            context.RegisterPostInitializationOutput((i) => i.AddSource(
                "FinderGeneratorAttribute.g.cs",
                SourceText.From(Templates.FinderGeneratorAttribute, Encoding.UTF8)));

            // Do a simple filter for enums
            var methodDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),         // select methods with attributes, single params, and return IEnumerable<T>
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))  // select the method with the [FinderGenerator] attribute
                .Where(static m => m is not null);                                      // filter out attributed enums that we don't care about

            // Combine the selected methods with the `Compilation`
            IncrementalValueProvider<(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods)> compilationAndEnums
                = context.CompilationProvider.Combine(methodDeclarations.Collect());

            // Generate the source using the compilation and methods
            context.RegisterSourceOutput(compilationAndEnums,
                /*static*/ (spc, source) => Execute(source.compilation, source.methods, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax method                           // any method
                && method.AttributeLists.Count > 0                                  // with at least 1 attribute
                && method.ParameterList.Parameters.Count == 1;                       // with 1 parameter
        }

        private static MethodDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // we know the node is a MethodDeclarationSyntax thanks to IsSyntaxTargetForGeneration
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

            // loop through all the attributes on the method
            foreach (var attributeListSyntax in methodDeclarationSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        // weird, we couldn't get the symbol, ignore it
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // Is the attribute the [FinderGenerator] attribute?
                    if (fullName == Templates.FinderGeneratorAttributeFullName)
                    {
                        // return the enum
                        return methodDeclarationSyntax;
                    }
                }
            }

            // we didn't find the attribute we were looking for
            return null;
        }

        public void Execute(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context)
        {
            // nothing to do yet
            if (methods.IsDefaultOrEmpty)
            {
                return;
            }

            // Convert each MethodDeclarationSyntax to a MethodToGenerate
            var methodsToGenerate = GetMethodsToGenerate(compilation, methods, context.CancellationToken);

            // If there were errors in the MethodDeclarationSyntax, we won't create an
            // MethodToGenerate for it, so make sure we have something to generate
            if (methodsToGenerate.Count > 0)
            {
                foreach (var group in methodsToGenerate.GroupBy(m => m.Symbol.ContainingType.Name))
                {
                    var finderMethods = new StringBuilder();
                    foreach (var finderMethodToGenerate in group)
                    {
                        var finderMethodWrapper = finderMethodToGenerate.ToString();

                        finderMethods.Append(GenerateFinderClass(
                            finderMethodWrapper,
                            finderMethodToGenerate.ParameterName,
                            finderMethodToGenerate.TypeToLookThrough,
                            finderMethodToGenerate.TypeToFind));

                        finderMethods.AppendLine();
                    }

                    var finderNamespace = methodsToGenerate.FirstOrDefault()?.Symbol?.ContainingNamespace.ToString();
                    var finderClassWrapper = BuildClassWrapper(group.First()?.Symbol.ContainingType);

                    var sourceCode = new StringBuilder("using System;")
                        .AppendLine("using System.Collections;")
                        .AppendLine("using System.Collections.Generic;")
                        .AppendLine()
                        .Append("namespace ").AppendLine(finderNamespace)
                        .AppendLine("{")
                        .Append('\t').AppendLine(finderClassWrapper)
                        .Append('\t').AppendLine("{")
                        .AppendLine(finderMethods.ToString())
                        .Append('\t').AppendLine("}")
                        .AppendLine("}").ToString();

                    context.AddSource($"{group.Key}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
        }

        private static List<MethodToGenerate> GetMethodsToGenerate(
           Compilation compilation,
           IEnumerable<MethodDeclarationSyntax> methods,
           CancellationToken cancellationToken)
        {
            // Create a list to hold our output
            var enumsToGenerate = new List<MethodToGenerate>();

            // Get the semantic representation of our marker attribute 
            var enumAttribute = compilation.GetTypeByMetadataName(Templates.FinderGeneratorAttributeFullName);

            if (enumAttribute == null)
            {
                // If this is null, the compilation couldn't find the marker attribute type
                // which suggests there's something very wrong! Bail out..
                return enumsToGenerate;
            }

            foreach (var methodDeclarationSyntax in methods)
            {
                // stop if we're asked to
                cancellationToken.ThrowIfCancellationRequested();

                // Get the semantic representation of the enum syntax
                var semanticModel = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is not IMethodSymbol methodSymbol)
                {
                    // something went wrong, bail out
                    continue;
                }

                if (methodSymbol.Parameters.Length != 1)
                {
                    // something went wrong, bail out
                    continue;
                }

                if (methodSymbol.ReturnType is not INamedTypeSymbol returnTypeSymbol)
                {
                    // something went wrong, bail out
                    continue;
                }

                if (returnTypeSymbol.TypeArguments.Length != 1)
                {
                    // something went wrong, bail out
                    continue;
                }

                if (returnTypeSymbol.TypeArguments[0] is not INamedTypeSymbol)
                {
                    // something went wrong, bail out
                    continue;
                }

                // Create a MethodToGenerate for use in the generation phase
                enumsToGenerate.Add(new MethodToGenerate(methodSymbol));
            }

            return enumsToGenerate;
        }

        private string GenerateFinderClass(
            string methodSignature,
            string parameterName,
            INamedTypeSymbol typeToLookThrough,
            INamedTypeSymbol typeToFind)
        {
            var sourceCodeBody = new StringBuilder();

            GenerateFinderMethod(
                sourceCodeBody,
                parameterName,
                typeToLookThrough,
                typeToFind);

            return new StringBuilder(methodSignature)
                .AppendLine()
                .Append('\t').Append('\t').AppendLine("{")
                .Append('\t').Append('\t').Append('\t').Append("var instances = new List<").Append(typeToFind.ToDisplayString()).AppendLine(">();")
                .AppendLine()
                .AppendLine(sourceCodeBody.ToString())
                .Append('\t').Append('\t').Append('\t').AppendLine("return instances;")
                .Append('\t').Append('\t').AppendLine("}")
                .ToString();

            //return $@"{methodSignature}
            //{{
            //    var instances = new List<{typeToFind.ToDisplayString()}>();
            //    {sourceCodeBody}
            //    return instances;
            //}}";
        }

        private void GenerateFinderMethod(
            StringBuilder sourceCode,
            string currentPath,
            INamedTypeSymbol currentType,
            INamedTypeSymbol typeToFind)
        {
            // Check if the current type is an IEnumerable
            if (TryGetGenericArgumentTypeFromIEnumerable(currentType, out var genericType))
            {
                // Continue traversing properties of T
                var tempSourceCode = new StringBuilder();
                var variableName = _variableNames.Dequeue();

                if (genericType != null)
                {
                    GenerateFinderMethod(
                        tempSourceCode,
                        variableName.ToString(),
                        genericType,
                        typeToFind);

                    // Check if there were any matches in type T
                    if (tempSourceCode.Length > 0)
                    {
                        var nullCheck = $"\n\t\t\tif({currentPath} != null) \n{{\n";
                        var foreachLoop = $"\t\t\tforeach(var {variableName} in {currentPath})\n\t\t\t{{\n";

                        tempSourceCode.Insert(0, nullCheck + foreachLoop);
                        tempSourceCode.Append('\t').Append('\t').Append('\t').AppendLine("}\n}");
                        sourceCode.Append(tempSourceCode.ToString());
                    }
                }

                // Variables are out of scope now, so we can add them to be used again
                _variableNames.Enqueue(variableName);
            }
            else
            {
                if (
                    currentType.Equals(typeToFind, SymbolEqualityComparer.Default) ||
                    (typeToFind.TypeKind == TypeKind.Interface && currentType.AllInterfaces.Any(i => i.Equals(typeToFind, SymbolEqualityComparer.Default))))
                {
                    sourceCode.Append('\t').Append('\t').Append('\t').Append("instances.Add(").Append(currentPath).AppendLine(");");
                }

                if (TypeHasPropertiesToSearch(currentType))
                {
                    var found = false;
                    var tempSourceCode = new StringBuilder();

                    foreach (var property in currentType.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (property.Type is INamedTypeSymbol propertyType && !property.Type.Equals(currentType, SymbolEqualityComparer.Default))
                        {
                            found = true;
                            GenerateFinderMethod(
                                tempSourceCode,
                                string.Concat(currentPath, ".", property.Name),
                                propertyType,
                                typeToFind);
                        }
                    }

                    if (found && tempSourceCode.Length > 0)
                    {
                        sourceCode.Append('\t').Append('\t').Append('\t').Append("if (").Append(currentPath).Append(" != null)\r\n\t{");
                        sourceCode.Append(tempSourceCode.ToString());
                        sourceCode.Append('\t').Append('\t').Append('\t').Append("}");
                    }
                }
            }
        }

        private static bool TypeHasPropertiesToSearch(INamedTypeSymbol t)
        {
            var valueType = t.BaseType?.SpecialType == SpecialType.System_ValueType;
            var excludedType = t.Name switch
            {
                "String" or "DateTime" => true,
                _ => false,
            };

            return !valueType && !excludedType;
        }

        private static string BuildClassWrapper(INamedTypeSymbol containingType)
        {
            var sb = new StringBuilder();

            var accessibility = containingType.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                _ => string.Empty
            };

            sb.Append(accessibility);

            if (containingType.IsStatic)
            {
                sb.Append(" static");
            }

            // TODO: figure out how to determine if this is a partial class
            sb.Append(" partial");

            if (containingType.IsSealed)
            {
                sb.Append(" sealed");
            }

            if (containingType.IsAbstract)
            {
                sb.Append(" abstract");
            }

            sb.Append(" class ");
            sb.Append(containingType.Name);

            return sb.ToString();
        }

        private static bool TryGetGenericArgumentTypeFromIEnumerable(
            INamedTypeSymbol type,
            out INamedTypeSymbol genericArgumentType)
        {
            genericArgumentType = null;
            if ((type.ConstructedFrom.Name == "IEnumerable" || type.AllInterfaces.Any(i => i.Name == "IEnumerable")) && type.TypeArguments.Length > 0)
            {
                genericArgumentType = type.TypeArguments[0] as INamedTypeSymbol;

                if (genericArgumentType != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class MethodToGenerate
    {
        public IMethodSymbol Symbol { get; }
        public string ParameterName { get; }
        public INamedTypeSymbol TypeToFind { get; }
        public INamedTypeSymbol TypeToLookThrough { get; }

        public MethodToGenerate(IMethodSymbol methodSymbol)
        {
            Symbol = methodSymbol;
            ParameterName = methodSymbol.Parameters[0].Name;
            TypeToFind = (methodSymbol.ReturnType as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol;
            TypeToLookThrough = methodSymbol.Parameters[0].Type as INamedTypeSymbol;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("\t\t");

            var accessibility = Symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Protected => "protected",
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedAndInternal => "internal protected",
                _ => string.Empty
            };

            sb.Append(accessibility);

            if (Symbol.IsStatic)
            {
                sb.Append(" static");
            }

            if (Symbol.IsPartialDefinition)
            {
                sb.Append(" partial");
            }

            sb.Append(" IEnumerable<");
            sb.Append(TypeToFind.ToDisplayString());
            sb.Append("> ");
            sb.Append(Symbol.Name);
            sb.Append("(");
            sb.Append(Symbol.Parameters[0].Type.ToDisplayString());
            sb.Append(" ");
            sb.Append(Symbol.Parameters[0].Name);
            sb.Append(")");

            return sb.ToString();
        }
    }
}
