using DurableFunctionsAnalyzer.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DurableFunctionsAnalyzer.Analyzers
{
    class NameAnalyzer
    {
        List<string> _availableNames = new List<string>();
        List<(string name, SyntaxNode node)> _calledFunctions = new List<(string, SyntaxNode)>();
        private DiagnosticDescriptor rule;

        public NameAnalyzer(DiagnosticDescriptor rule)
        {
            this.rule = rule;
        }

        private string GetClosestString(string name, List<string> availableNames)
        {
            return availableNames.OrderBy(x => x.LevenshteinDistance(name)).First();
        }

        public void ReportProblems(CompilationAnalysisContext cac)
        {
            foreach (var node in _calledFunctions)
            {
                if (!_availableNames.Contains(node.name))
                {
                    cac.ReportDiagnostic(Diagnostic.Create(rule, node.node.GetLocation(), node.name, GetClosestString(node.name, _availableNames)));
                }
            }
        }
        public void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = context.Node as InvocationExpressionSyntax;
            if (invocationExpression != null)
            {
                var expression = invocationExpression.Expression as MemberAccessExpressionSyntax;
                if (expression != null)
                {
                    var name = expression.Name;
                    if (name.ToString().StartsWith("CallActivityAsync"))
                    {
                        var functionName = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
                        if (functionName != null && functionName.ToString().StartsWith("\""))
                            _calledFunctions.Add((functionName.ToString().Trim('"'), context.Node));
                    }
                }
            }
        }
        public void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
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
