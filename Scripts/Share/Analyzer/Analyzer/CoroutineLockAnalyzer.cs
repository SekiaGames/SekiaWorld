#nullable disable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CoroutineLockAnalyzer: DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(
                    CoroutineLockUsingAnalyzerRule.Rule,
                    CoroutineLockDisposeAnalyzerRule.Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(analysisContext =>
            {
                if (!AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                {
                    return;
                }

                analysisContext.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
                analysisContext.RegisterSyntaxNodeAction(AnalyzeLocalDeclarationStatement, SyntaxKind.LocalDeclarationStatement);
                analysisContext.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            UsingStatementSyntax usingStatement = (UsingStatementSyntax)context.Node;

            if (usingStatement.Declaration != null)
            {
                TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(usingStatement.Declaration.Type);
                if (IsCoroutineLock(typeInfo.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        CoroutineLockUsingAnalyzerRule.Rule,
                        usingStatement.UsingKeyword.GetLocation()));
                }
            }

            if (usingStatement.Expression != null)
            {
                TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(usingStatement.Expression);
                if (IsCoroutineLock(typeInfo.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        CoroutineLockUsingAnalyzerRule.Rule,
                        usingStatement.UsingKeyword.GetLocation()));
                }
            }
        }

        private static void AnalyzeLocalDeclarationStatement(SyntaxNodeAnalysisContext context)
        {
            LocalDeclarationStatementSyntax localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

            if (!localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
            {
                return;
            }

            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(localDeclaration.Declaration.Type);
            if (IsCoroutineLock(typeInfo.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CoroutineLockUsingAnalyzerRule.Rule,
                    localDeclaration.UsingKeyword.GetLocation()));
            }
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess) ||
                memberAccess.Name.Identifier.ValueText != "Dispose")
            {
                return;
            }

            TypeInfo memberType = context.SemanticModel.GetTypeInfo(memberAccess.Expression);

            if (IsCoroutineLock(memberType.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CoroutineLockDisposeAnalyzerRule.Rule,
                    invocation.GetLocation()));
            }
        }

        private static bool IsCoroutineLock(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            return typeSymbol.Name == "CoroutineLock" &&
                   typeSymbol.ContainingNamespace?.ToDisplayString() == "ET";
        }
    }
}
