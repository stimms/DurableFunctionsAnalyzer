using DurableFunctionsAnalyzer.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DurableFunctionsAnalyzer.Analyzers
{
    class NameAnalyzer : IFunctionAnalyzer
    {
        public const string DiagnosticId = "DurableFunctionsNameAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NameAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString CloseMessageFormat = new LocalizableResourceString(nameof(Resources.NameAnalyzerCloseMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MissingMessageFormat = new LocalizableResourceString(nameof(Resources.NameAnalyzerMissingMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NameAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        public static DiagnosticDescriptor CloseRule = new DiagnosticDescriptor(DiagnosticId, Title, CloseMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        public static DiagnosticDescriptor MissingRule = new DiagnosticDescriptor(DiagnosticId, Title, MissingMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);


        private string GetClosestString(string name, IEnumerable<string> availableNames)
        {
            return availableNames.OrderBy(x => x.LevenshteinDistance(name)).First();
        }

        public void ReportProblems(CompilationAnalysisContext cac, IEnumerable<(string name, string activityTriggerType)> availableFunctions, IEnumerable<(string name, SyntaxNode nameNode, SyntaxNode parameterNode, string parameterType)> calledFunctions)
        {
            foreach (var node in calledFunctions)
            {
                if (!availableFunctions.Any())
                {
                    cac.ReportDiagnostic(Diagnostic.Create(MissingRule, node.nameNode.GetLocation(), node.name));
                }
                else if (!availableFunctions.Select(x => x.name).Contains(node.name))
                {
                    cac.ReportDiagnostic(Diagnostic.Create(CloseRule, node.nameNode.GetLocation(), node.name, GetClosestString(node.name, availableFunctions.Select(x => x.name))));
                }
            }
        }

    }
}
