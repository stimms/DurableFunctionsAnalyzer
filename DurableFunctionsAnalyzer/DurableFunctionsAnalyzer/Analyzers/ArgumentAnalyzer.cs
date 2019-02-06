﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DurableFunctionsAnalyzer.Analyzers
{
    class ArgumentAnalyzer : IFunctionAnalyzer
    {
        public const string DiagnosticId = "DurableFunctionsArgumentAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ArgumentAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ArgumentAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ArgumentAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Argument";

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);


        public void ReportProblems(CompilationAnalysisContext cac, IEnumerable<(string name, string activityTriggerType)> availableFunctions, IEnumerable<(string name, SyntaxNode nameNode, SyntaxNode parameterNode, string parameterType)> calledFunctions)
        {
            foreach (var node in calledFunctions)
            {
                if (availableFunctions.Where(x => x.name == node.name).Any())
                {
                    var functionDefinition = availableFunctions.Where(x => x.name == node.name).SingleOrDefault();
                    if (functionDefinition.activityTriggerType != node.parameterType)
                    {
                        cac.ReportDiagnostic(Diagnostic.Create(Rule, node.parameterNode.GetLocation(), node.name, functionDefinition.activityTriggerType, node.parameterType));
                    }
                }
              
            }
        }

    }
}
