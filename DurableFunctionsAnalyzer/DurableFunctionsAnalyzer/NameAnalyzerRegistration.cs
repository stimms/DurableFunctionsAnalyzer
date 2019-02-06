using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using DurableFunctionsAnalyzer.Extensions;
using DurableFunctionsAnalyzer.Analyzers;

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
                return ImmutableArray.Create(NameAnalyzer.CloseRule, NameAnalyzer.MissingRule, ArgumentAnalyzer.Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            var nameAnalyzer = new NameAnalyzer();
            var argumentAnalyzer = new ArgumentAnalyzer();
            var baseAnalyzer = new BaseFunctionAnalyzer();
            baseAnalyzer.RegisterAnalyzer(nameAnalyzer);
            baseAnalyzer.RegisterAnalyzer(argumentAnalyzer);
            context.RegisterCompilationStartAction(c =>
            {
                c.RegisterCompilationEndAction(baseAnalyzer.ReportProblems);
                c.RegisterSyntaxNodeAction(baseAnalyzer.FindActivityCalls, SyntaxKind.InvocationExpression);
                c.RegisterSyntaxNodeAction(baseAnalyzer.FindActivities, SyntaxKind.Attribute);
            });
        }

    }
}
