using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DurableFunctionsAnalyzer.Analyzers
{
    interface IFunctionAnalyzer
    {
        void ReportProblems(CompilationAnalysisContext cac, IEnumerable<(string name, string activityTriggerType)> availableFunctions, IEnumerable<(string name, SyntaxNode nameNode, SyntaxNode parameterNode, String parameterType)> calledFunctions);
    }
}
