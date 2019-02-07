using DurableFunctionsAnalyzer.Models;
using Microsoft.CodeAnalysis;
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
        List<FunctionDefinition> _availableFunctions = new List<FunctionDefinition>();
        List<FunctionCall> _calledFunctions = new List<FunctionCall>();
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
                    if (name.ToString().StartsWith("CallActivityAsync") || name.ToString().StartsWith("CallActivityWithRetryAsync"))
                    {
                        var functionName = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
                        var argumentType = invocationExpression.ArgumentList.Arguments.Last();
                        var returnType = invocationExpression.ChildNodes().Where(x=>x.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                            .FirstOrDefault()?
                            .ChildNodes()
                            .Where(x => x.IsKind(SyntaxKind.GenericName))
                            .FirstOrDefault()?
                            .ChildNodes()
                            .Where(x=>x.IsKind(SyntaxKind.TypeArgumentList))?
                            .FirstOrDefault();
                        var returnTypeName = "";
                        if(returnType != null)
                        {
                            returnTypeName = GetQualifiedTypeName(context.SemanticModel.GetTypeInfo(returnType.ChildNodes().FirstOrDefault()).Type);
                            returnTypeName = "System.Threading.Tasks.Task<" + returnTypeName + ">";
                            
                        }
                        var typeInfo = context.SemanticModel.GetTypeInfo(argumentType.ChildNodes().First());
                        var typeName = "";
                        if (typeInfo.Type.OriginalDefinition.ContainingNamespace.ToString() != "<global namespace>")
                            typeName = typeInfo.Type.OriginalDefinition.ContainingNamespace + "." + typeInfo.Type?.OriginalDefinition?.Name;
                        else
                            typeName = "System." + typeInfo.Type?.OriginalDefinition?.Name;
                        if (functionName != null && functionName.ToString().StartsWith("\""))
                            _calledFunctions.Add(new FunctionCall
                            {
                                Name = functionName.ToString().Trim('"'),
                                NameNode = functionName,
                                ParameterNode = argumentType,
                                ParameterType = typeName,
                                ExpectedReturnType = returnTypeName,
                                ExpectedReturnTypeNode = invocationExpression
                            });
                    }
                }
            }
        }

        private string GetQualifiedTypeName(ITypeSymbol typeInfo)
        {
            var b = typeInfo as INamedTypeSymbol;
            var genericType = "";
            if (b.TypeArguments.Any())
            {
                 genericType = "<" +  GetQualifiedTypeName(b.TypeArguments.First()) + ">";
            }
            var typeName = "";
            if (typeInfo.OriginalDefinition.ContainingNamespace.ToString() != "<global namespace>")
                typeName = typeInfo.OriginalDefinition.ContainingNamespace + "." + typeInfo.OriginalDefinition?.Name;
            else
                typeName = "System." + typeInfo.OriginalDefinition?.Name;
            return typeName + genericType;
        }

        public void FindActivities(SyntaxNodeAnalysisContext context)
        {
            var attributeExpression = context.Node as AttributeSyntax;
            if (attributeExpression != null && attributeExpression.ChildNodes().First().ToString() == "FunctionName")
            {
                var didAdd = false;
                var functionName = attributeExpression.ArgumentList.Arguments.First().ToString().Trim('"');
                var parameterList = attributeExpression.Parent.Parent.ChildNodes().Where(x => x.IsKind(SyntaxKind.ParameterList)).SingleOrDefault();
                var methodDeclaration = attributeExpression.Parent.Parent;
                while (!methodDeclaration.IsKind(SyntaxKind.MethodDeclaration))
                {
                    methodDeclaration = methodDeclaration.Parent;
                }
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
                                            _availableFunctions.Add(new FunctionDefinition
                                            {
                                                Name = functionName,
                                                ActivityTriggerType = typeInfo.Type.OriginalDefinition.ContainingNamespace + "." + typeInfo.Type.OriginalDefinition.Name,
                                                ReturnType =  GetQualifiedTypeName(context.SemanticModel.GetTypeInfo((methodDeclaration as MethodDeclarationSyntax).ReturnType).Type)
                                            });
                                        else
                                            _availableFunctions.Add(new FunctionDefinition
                                            {
                                                Name = functionName,
                                                ActivityTriggerType = "System." + typeInfo.Type.OriginalDefinition.Name,
                                                ReturnType = GetQualifiedTypeName(context.SemanticModel.GetTypeInfo((methodDeclaration as MethodDeclarationSyntax).ReturnType).Type)
                                            });
                                        didAdd = true;
                                    }
                                }
                            }
                        }
                    }
                }
                if (!didAdd)
                {
                    _availableFunctions.Add(new FunctionDefinition
                    {
                        Name = functionName
                    });
                }
            }
        }
    }
}
