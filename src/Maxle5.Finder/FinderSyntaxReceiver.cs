using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Maxle5.Finder
{
    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    internal class FinderSyntaxReceiver : ISyntaxContextReceiver
    {
        /// <summary>
        /// List of "Finder methods" to generate
        /// </summary>
        public List<IMethodSymbol> FinderMethodsToGenerate { get; } = new List<IMethodSymbol>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax             // any method
                && methodDeclarationSyntax.AttributeLists.Count > 0                         // with atleast 1 attribute
                && methodDeclarationSyntax.Modifiers.Any(m => m.ValueText == "partial")     // that is partial
                && methodDeclarationSyntax.ParameterList.Parameters.Count == 1)             // with 1 parameter
            {
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax) as IMethodSymbol;

                // Get the symbol being declared by the field, and keep it if its annotated
                if (methodSymbol?.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == "Maxle5.Finder.FinderGeneratorAttribute") == true)
                {
                    FinderMethodsToGenerate.Add(methodSymbol);
                }
            }
        }
    }
}
