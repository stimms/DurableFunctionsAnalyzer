﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DurableFunctionsAnalyzer.Analyzers
{
    class BaseFunctionAnalyzer
    {
        List<(string name, string activityTriggerType)> _availableFunctions = new List<(string, string)>();
        List<(string name, SyntaxNode node, SyntaxNode parameterNode, String parameterType)> _calledFunctions = new List<(string, SyntaxNode, SyntaxNode, String)>();
        List<IFunctionAnalyzer> _analyzers = new List<IFunctionAnalyzer>();

        public void ReportProblems(CompilationAnalysisContext cac)
        {
            foreach (var analyzer in _analyzers)
                analyzer.ReportProblems(cac, _availableFunctions, _calledFunctions);
        }

        public void RegisterAnalyzer(IFunctionAnalyzer analyzer)
        {
            _analyzers.Add(analyzer);
        }
        public void FindActivityCalls(SyntaxNodeAnalysisContext context)
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
                        var argumentType = invocationExpression.ArgumentList.Arguments.Last();
                        var typeInfo = context.SemanticModel.GetTypeInfo(argumentType.ChildNodes().First());
                        var typeName = "";
                        if (typeInfo.Type.OriginalDefinition.ContainingNamespace.ToString() != "<global namespace>")
                            typeName = typeInfo.Type.OriginalDefinition.ContainingNamespace + "." + typeInfo.Type?.OriginalDefinition?.Name;
                        else
                            typeName = "System." + typeInfo.Type?.OriginalDefinition?.Name;
                        if (functionName != null && functionName.ToString().StartsWith("\""))
                            _calledFunctions.Add((functionName.ToString().Trim('"'), functionName, argumentType, typeName));
                    }
                }
            }
        }

        public void FindActivities(SyntaxNodeAnalysisContext context)
        {
            var attributeExpression = context.Node as AttributeSyntax;
            if (attributeExpression != null && attributeExpression.ChildNodes().First().ToString() == "FunctionName")
            {
                var didAdd = false;
                var functionName = attributeExpression.ArgumentList.Arguments.First().ToString().Trim('"');
                var parameterList = attributeExpression.Parent.Parent.ChildNodes().Where(x => x.IsKind(SyntaxKind.ParameterList)).SingleOrDefault();
                if (parameterList != null)
                {
                    foreach (var parameter in parameterList.ChildNodes().Where(x => x.IsKind(SyntaxKind.Parameter)))
                    {
                        foreach (var attributeList in parameter.ChildNodes().Where(x => x.IsKind(SyntaxKind.AttributeList)))
                        {
                            foreach (var attribute in attributeList.ChildNodes().Where(x => x.IsKind(SyntaxKind.Attribute)))
                            {
                                if ((attribute as AttributeSyntax).Name.ToString() == "ActivityTrigger")
                                {
                                    var kindName = parameter.ChildNodes().Where(x => x.IsKind(SyntaxKind.IdentifierName)).SingleOrDefault();
                                    if (kindName == null)
                                    {
                                        //predefined types
                                        kindName = parameter.ChildNodes().Where(x => x.IsKind(SyntaxKind.PredefinedType)).SingleOrDefault();
                                    }
                                    if (kindName != null)
                                    {
                                        var typeInfo = context.SemanticModel.GetTypeInfo(kindName);
                                        if (typeInfo.Type.OriginalDefinition.ContainingNamespace.ToString() != "<global namespace>")
                                            _availableFunctions.Add((functionName, typeInfo.Type.OriginalDefinition.ContainingNamespace + "." + typeInfo.Type.OriginalDefinition.Name));
                                        else
                                            _availableFunctions.Add((functionName, "System." + typeInfo.Type.OriginalDefinition.Name));
                                        didAdd = true;
                                    }
                                }
                            }
                        }
                    }
                }
                if (!didAdd)
                {
                    _availableFunctions.Add((functionName, null));
                }
            }
        }
    }
}
