using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DurableFunctionsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DurableFunctionsAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DurableFunctionsAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        static List<string> _availableNames = new List<string>();
        static List<(string name, SyntaxNode node)> _calledFunctions = new List<(string, SyntaxNode)>();
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(c =>
            {
                c.RegisterCompilationEndAction(cac =>
                {
                    foreach (var node in _calledFunctions)
                    {
                        if (!_availableNames.Contains(node.name))
                        {
                            cac.ReportDiagnostic(Diagnostic.Create(Rule, node.node.GetLocation(), node.name));
                        }
                    }
                });
            });
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            Console.Write("here");
            var invocationExpression = context.Node as InvocationExpressionSyntax;
            if (invocationExpression != null)
            {
                var expression = invocationExpression.Expression as MemberAccessExpressionSyntax;
                if (expression != null)
                {
                    var name = expression.Name;
                    if (name.ToString().StartsWith("CallActivityAsync"))
                    {
                        var memberInfo = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;
                        if (memberInfo != null)
                        {
                            var miName = memberInfo.ToString();
                        }
                        var functionName = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
                        if (functionName != null)
                            _calledFunctions.Add((functionName.ToString().Trim('"'), context.Node));
                    }
                }
            }
        }
        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
            var encountered = false;
            foreach (var node in root.DescendantTokens())
            {
                if (node.IsKind(SyntaxKind.IdentifierToken) && node.Text == "FunctionName")
                {
                    encountered = true;
                }
                if (node.IsKind(SyntaxKind.StringLiteralToken) && encountered)
                {
                    encountered = false;
                    _availableNames.Add(node.Text.Trim('"'));
                }
            }

        }
    }
}
