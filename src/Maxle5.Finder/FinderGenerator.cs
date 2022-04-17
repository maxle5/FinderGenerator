using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Maxle5.Finder
{
    [Generator]
    public class FinderSourceGenerator : ISourceGenerator
    {
        private readonly Queue<char> _variableNames = new(new[]
        {
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','v','w','x','y','z'
        });

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            // If you want to debug the Source Generator, please uncomment the below code.
            // if (!Debugger.IsAttached)
            // {
            //     Debugger.Launch();
            // }
#endif            

            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource(
                "FinderGeneratorAttribute.g.cs",
                @"
using System;
using System.Collections.Generic;

namespace Maxle5.Finder
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class FinderGeneratorAttribute : Attribute
    {
        public FinderGeneratorAttribute()
        {
        }
    }
}"));

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new FinderSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (context.SyntaxContextReceiver is not FinderSyntaxReceiver receiver)
            {
                return;
            }

            foreach (var group in receiver.FinderMethodsToGenerate.GroupBy(m => m.ContainingType.Name))
            {
                var finderMethods = new StringBuilder();
                foreach (var finderMethodToGenerate in group)
                {
                    if (finderMethodToGenerate.Parameters.Length != 1)
                    {
                        continue; // doesn't have a single parameter (this is a requirement)
                    }

                    if (!TryGetGenericArgumentTypeFromIEnumerable(finderMethodToGenerate.ReturnType as INamedTypeSymbol, out var typeToFind))
                    {
                        continue; // doesn't return IEnumerable (this is a requirement)
                    }

                    var finderMethodWrapper = BuildMethodWrapper(finderMethodToGenerate, typeToFind);
                    var parameter = finderMethodToGenerate.Parameters[0];
                    var typeToLookThrough = parameter.Type as INamedTypeSymbol;
                    finderMethods.Append(GenerateFinderClass(finderMethodWrapper, parameter.Name, typeToLookThrough, typeToFind));
                    finderMethods.Append("\n\n\t");
                }

                var finderNamespace = receiver.FinderMethodsToGenerate.FirstOrDefault()?.ContainingNamespace.ToString();
                var finderClassWrapper = BuildClassWrapper(receiver.FinderMethodsToGenerate.FirstOrDefault());

                var sourceCode = $@"using System;
using System.Collections.Generic;

namespace {finderNamespace}
{{
    {finderClassWrapper}
    {{
        {finderMethods}
    }}
}}";

                context.AddSource($"{group.Key}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
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

            return $@"{methodSignature}
        {{
            var instances = new List<{typeToFind.ToDisplayString()}>();
            {sourceCodeBody}
            return instances;
        }}";
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
                        tempSourceCode.Insert(0, $"foreach(var {variableName} in {currentPath})\n{{\n");
                        tempSourceCode.Append("}\n");
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
                    sourceCode.Append("instances.Add(").Append(currentPath).AppendLine(");\n");
                }

                if (TypeHasPropertiesToSearch(currentType))
                {
                    foreach (var property in currentType.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (property.Type is INamedTypeSymbol propertyType && !property.Type.Equals(currentType, SymbolEqualityComparer.Default))
                        {
                            GenerateFinderMethod(
                                sourceCode,
                                string.Concat(currentPath, ".", property.Name),
                                propertyType,
                                typeToFind);
                        }
                    }
                }
            }
        }

        private static bool TypeHasPropertiesToSearch(INamedTypeSymbol t)
        {
            switch (t.Name)
            {
                case "String":
                case "char":
                case "byte":
                case "sbyte":
                case "ushort":
                case "short":
                case "uint":
                case "int":
                case "ulong":
                case "long":
                case "float":
                case "double":
                case "decimal":
                case "DateTime":
                    return false;
                default:
                    return true;
            }
        }

        private static string BuildClassWrapper(IMethodSymbol method)
        {
            var sb = new StringBuilder();

            var @class = method.ContainingType;

            var accessibility = @class.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                _ => string.Empty
            };

            sb.Append(accessibility);

            if (@class.IsStatic)
            {
                sb.Append(" static");
            }

            // TODO: figure out how to determine if this is a partial class
            sb.Append(" partial");

            if (@class.IsSealed)
            {
                sb.Append(" sealed");
            }

            if (@class.IsAbstract)
            {
                sb.Append(" abstract");
            }

            sb.Append(" class ");
            sb.Append(@class.Name);

            return sb.ToString();
        }

        private static string BuildMethodWrapper(IMethodSymbol method, INamedTypeSymbol typeToFind)
        {
            var sb = new StringBuilder();

            var accessibility = method.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Protected => "protected",
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedAndInternal => "internal protected",
                _ => string.Empty
            };

            sb.Append(accessibility);

            if (method.IsStatic)
            {
                sb.Append(" static");
            }

            if (method.IsPartialDefinition)
            {
                sb.Append(" partial");
            }

            sb.Append(" IEnumerable<");
            sb.Append(typeToFind.ToDisplayString());
            sb.Append("> ");
            sb.Append(method.Name);
            sb.Append("(");
            sb.Append(method.Parameters[0].Type.ToDisplayString());
            sb.Append(" ");
            sb.Append(method.Parameters[0].Name);
            sb.Append(")");

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
}
