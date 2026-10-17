using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisableGetComponentAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(DisableGetComponentAnalyzerRule.Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction((analysisContext =>
            {
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                {
                    analysisContext.RegisterSemanticModelAction((this.AnalyzeSemanticModel));
                }
            }));
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext analysisContext)
        {
            foreach (var memberAccessExpressionSyntax in analysisContext.SemanticModel.SyntaxTree.GetRoot()
                         .DescendantNodes<MemberAccessExpressionSyntax>())
            {
                AnalyzeMemberAccessExpression(analysisContext, memberAccessExpressionSyntax);
            }
        }

        private void AnalyzeMemberAccessExpression(SemanticModelAnalysisContext context,
        MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            string methodName = memberAccessExpressionSyntax.Name.Identifier.Text;
            if (methodName != Definition.ComponentMethod[1])
            {
                return;
            }

            if (memberAccessExpressionSyntax.Parent is not InvocationExpressionSyntax invocationExpressionSyntax ||
                context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (methodSymbol.Name != Definition.ComponentMethod[1])
            {
                return;
            }

            ITypeSymbol? componentTypeSymbol = GetComponentTypeSymbol(context.SemanticModel, invocationExpressionSyntax, methodSymbol);
            if (componentTypeSymbol == null)
            {
                return;
            }

            if (componentTypeSymbol is ITypeParameterSymbol)
            {
                return;
            }

            if (!componentTypeSymbol.HasAttributeInTypeAndBaseTyes(Definition.DisableGetComponentAttribute))
            {
                return;
            }

            if (IsGetComponentEnabled(context.SemanticModel, memberAccessExpressionSyntax, componentTypeSymbol))
            {
                return;
            }

            Diagnostic diagnostic = Diagnostic.Create(DisableGetComponentAnalyzerRule.Rule,
                memberAccessExpressionSyntax.Name.Identifier.GetLocation(), componentTypeSymbol.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        private static ITypeSymbol? GetComponentTypeSymbol(SemanticModel semanticModel,
        InvocationExpressionSyntax invocationExpressionSyntax, IMethodSymbol methodSymbol)
        {
            if (methodSymbol.IsGenericMethod)
            {
                return methodSymbol.TypeArguments.FirstOrDefault();
            }

            var firstArgumentSyntax = invocationExpressionSyntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
            if (firstArgumentSyntax == null)
            {
                return null;
            }

            if (firstArgumentSyntax is TypeOfExpressionSyntax typeOfExpressionSyntax)
            {
                firstArgumentSyntax = typeOfExpressionSyntax.Type;
            }

            ISymbol? firstArgumentSymbol = semanticModel.GetSymbolInfo(firstArgumentSyntax).Symbol;

            return firstArgumentSymbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                IMethodSymbol method => method.ReturnType,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                INamedTypeSymbol namedTypeSymbol => namedTypeSymbol,
                ITypeParameterSymbol typeParameterSymbol => typeParameterSymbol,
                _ => null
            };
        }

        private static bool IsGetComponentEnabled(SemanticModel semanticModel,
        MemberAccessExpressionSyntax memberAccessExpressionSyntax, ITypeSymbol componentTypeSymbol)
        {
            IMethodSymbol? containingMethodSymbol = null;
            var methodDeclarationSyntax = memberAccessExpressionSyntax.GetNeareastAncestor<MethodDeclarationSyntax>();
            if (methodDeclarationSyntax != null)
            {
                containingMethodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax) as IMethodSymbol;
            }

            if (containingMethodSymbol == null)
            {
                var localFunctionSyntax = memberAccessExpressionSyntax.GetNeareastAncestor<LocalFunctionStatementSyntax>();
                if (localFunctionSyntax != null)
                {
                    containingMethodSymbol = semanticModel.GetDeclaredSymbol(localFunctionSyntax) as IMethodSymbol;
                }
            }

            if (containingMethodSymbol == null)
            {
                return false;
            }

            foreach (AttributeData? attributeData in containingMethodSymbol.GetAttributes())
            {
                if (attributeData?.AttributeClass?.ToString() != Definition.EnableGetComponentAttribute)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments.Length == 0)
                {
                    return true;
                }

                foreach (TypedConstant argument in attributeData.ConstructorArguments)
                {
                    if (argument.Kind == TypedConstantKind.Array)
                    {
                        foreach (var value in argument.Values)
                        {
                            if (value.Value is INamedTypeSymbol namedTypeSymbol &&
                                SymbolEqualityComparer.Default.Equals(namedTypeSymbol, componentTypeSymbol))
                            {
                                return true;
                            }
                        }
                        continue;
                    }

                    if (argument.Value is INamedTypeSymbol singleTypeSymbol &&
                        SymbolEqualityComparer.Default.Equals(singleTypeSymbol, componentTypeSymbol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
