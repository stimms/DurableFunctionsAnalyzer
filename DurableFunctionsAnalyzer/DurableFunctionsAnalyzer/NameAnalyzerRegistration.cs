using DurableFunctionsAnalyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace DurableFunctionsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NameAnalyzerRegistration : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DurableFunctionsAnalyzer";


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    NameAnalyzer.CloseRule,
                    NameAnalyzer.MissingRule,
                    ArgumentAnalyzer.Rule,
                    ReturnTypeAnalyzer.Rule,
                    OrchestrationTriggerAnnotationAnalyzer.Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            var nameAnalyzer = new NameAnalyzer();
            var argumentAnalyzer = new ArgumentAnalyzer();
            var baseAnalyzer = new BaseFunctionAnalyzer();
            var orchestrationTriggerAnalyzer = new OrchestrationTriggerAnnotationAnalyzer();
            baseAnalyzer.RegisterAnalyzer(nameAnalyzer);
            baseAnalyzer.RegisterAnalyzer(argumentAnalyzer);
            baseAnalyzer.RegisterAnalyzer(returnTypeAnalyzer);
            context.RegisterSyntaxNodeAction(orchestrationTriggerAnalyzer.FindOrchestrationTriggers, SyntaxKind.Attribute);
            context.RegisterCompilationStartAction(c =>
            {
                c.RegisterCompilationEndAction(baseAnalyzer.ReportProblems);
                c.RegisterSyntaxNodeAction(baseAnalyzer.FindActivityCalls, SyntaxKind.InvocationExpression);
                c.RegisterSyntaxNodeAction(baseAnalyzer.FindActivities, SyntaxKind.Attribute);
            });
        }

    }
}
