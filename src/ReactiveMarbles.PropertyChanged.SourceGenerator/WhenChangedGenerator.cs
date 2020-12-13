// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ReactiveMarbles.PropertyChanged
{
    [Generator]
    internal class WhenChangedGenerator : ISourceGenerator
    {
        private record MethodDetail(ITypeSymbol InputType, ITypeSymbol OutputType, List<ArgumentDetail> Arguments);

        private record ArgumentDetail(LambdaExpressionSyntax LambdaExpression, ITypeSymbol InputType, ITypeSymbol OutputType);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
            {
                return;
            }

            var compilation = context.Compilation;

            var propertyExpressionDetailObjects = new List<ArgumentDetail>();
            var multiExpressionMethods = new List<MethodDetail>();
            foreach (var invocationExpression in syntaxReceiver.WhenChangedMethods)
            {
                var model = compilation.GetSemanticModel(invocationExpression.SyntaxTree);
                var symbol = model.GetSymbolInfo(invocationExpression).Symbol;

                if (symbol is IMethodSymbol methodSymbol && methodSymbol.ContainingAssembly.Name.Equals("ReactiveMarbles.PropertyChanged.SourceGenerator"))
                {
                    var arguments = invocationExpression.ArgumentList.Arguments;
                    var argDetailObjectsForMethod = new List<ArgumentDetail>(arguments.Count);

                    foreach (var argument in arguments)
                    {
                        if (argument.Expression is LambdaExpressionSyntax lambdaExpression)
                        {
                            var lambdaOutputType = model.GetTypeInfo(lambdaExpression.Body).Type;
                            argDetailObjectsForMethod.Add(new(lambdaExpression, methodSymbol.ReceiverType, lambdaOutputType));
                        }
                        else if (model.GetTypeInfo(argument.Expression).ConvertedType.Name.Equals("Expression"))
                        {
                            // The argument is evaluates to an expression but it's not inline (could be a variable, method invocation, etc).
                            // TODO: Issue a diagnostic.
                            return;
                        }
                    }

                    propertyExpressionDetailObjects.AddRange(argDetailObjectsForMethod);

                    if (arguments.Count > 1)
                    {
                        propertyExpressionDetailObjects.RemoveAt(propertyExpressionDetailObjects.Count - 1);
                        var outputType = model.GetTypeInfo(argDetailObjectsForMethod.Last().LambdaExpression.Body).Type;
                        multiExpressionMethods.Add(new(methodSymbol.ReceiverType, outputType, argDetailObjectsForMethod));
                    }
                }
            }

            foreach (var group in propertyExpressionDetailObjects.GroupBy(x => x.InputType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), multiExpressionMethods);
                if (classSource != null)
                {
                    context.AddSource($"{group.Key.Name}.WhenChanged.g.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private static string ProcessClass(ITypeSymbol classSymbol, List<ArgumentDetail> propertyExpressionDetailObjects, List<MethodDetail> multiExpressionMethods)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                // TODO: Issue a diagnostic stating that nested classes aren't currently supported.
                return null;
            }

            StringBuilder classBodyBuilder = new StringBuilder();

            foreach (var group in propertyExpressionDetailObjects.GroupBy(x => x.OutputType))
            {
                ProcessMethod(
                    classBodyBuilder,
                    group
                        .GroupBy(
                            x =>
                            {
                                var body = x.LambdaExpression.Body.ToString();
                                return body.Substring(body.IndexOf('.') + 1);
                            })
                        .Select(x => x.First())
                        .ToList());
            }

            ProcessMultiExpressionMethods(classBodyBuilder, multiExpressionMethods);
            var source = WhenChangedClassBuilder.GetClass(classSymbol.Name, classBodyBuilder.ToString());

            return source;
        }

        private static void ProcessMethod(StringBuilder source, List<ArgumentDetail> propertyExpressionDetailObjects)
        {
            var (lambdaExpression, inputTypeSymbol, outputTypeSymbol) = propertyExpressionDetailObjects.First();
            var mapName = $"{inputTypeSymbol.Name}To{outputTypeSymbol.Name}Map";

            var whenChangedMethodCode = WhenChangedClassBuilder.GetWhenChangedMethod(
                inputTypeSymbol.ToDisplayString(),
                outputTypeSymbol.ToDisplayString(),
                mapName);

            var mapEntryBuilder = new StringBuilder();
            foreach (var methodDetail in propertyExpressionDetailObjects)
            {
                var lambda = methodDetail.LambdaExpression;
                if (lambda.Body is not MemberAccessExpressionSyntax expressionChain)
                {
                    // TODO: Issue a diagnostic stating that only fields and properties are supported.
                    return;
                }

                var current = expressionChain;
                var members = new List<MemberAccessExpressionSyntax>();
                while (current != null)
                {
                    members.Add(current);
                    current = current.Expression as MemberAccessExpressionSyntax;
                }

                members.Reverse();

                var expressionChainString = expressionChain.ToString();
                var mapKey = expressionChainString.Substring(expressionChainString.IndexOf('.') + 1);

                var observableChainBuilder = new StringBuilder();
                foreach (var memberAccessExpression in members)
                {
                    var observableChainCode = WhenChangedClassBuilder.GetMapEntryChain(memberAccessExpression.Name.ToString());
                    observableChainBuilder.Append(observableChainCode);
                }

                var mapEntryCode = WhenChangedClassBuilder.GetMapEntry(mapKey, observableChainBuilder.ToString());
                mapEntryBuilder.Append(mapEntryCode);
            }

            var mapCode = WhenChangedClassBuilder.GetMap(
                inputTypeSymbol.ToDisplayString(), // Name
                outputTypeSymbol.ToDisplayString(), // Name
                mapName,
                mapEntryBuilder.ToString());

            source.Append(mapCode).Append(whenChangedMethodCode);
        }

        private static void ProcessMultiExpressionMethods(StringBuilder source, List<MethodDetail> multiExpressionMethods)
        {
            foreach (var method in multiExpressionMethods)
            {
                string bodyCode = WhenChangedClassBuilder.GetMultiExpressionMethodBody(method.Arguments.Count - 1);
                var tempReturnTypes = method.Arguments
                    .Take(method.Arguments.Count - 1)
                    .Select(x => x.OutputType.Name)
                    .ToList();

                var parametersCode = WhenChangedClassBuilder.GetMultiExpressionMethodParameters(method.InputType.Name, method.OutputType.Name, tempReturnTypes, method.Arguments.Count - 1);
                var methodCode = WhenChangedClassBuilder.GetMultiExpressionMethod(method.InputType.Name, method.OutputType.Name, parametersCode, bodyCode);
                source.Append(methodCode);
            }
        }
    }
}
