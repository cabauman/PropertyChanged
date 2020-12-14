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

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    [Generator]
    internal sealed class WhenChangedGenerator : ISourceGenerator
    {
        private static readonly string SourceGeneratorAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        private static readonly DiagnosticDescriptor InvalidMemberExpressionError = new DiagnosticDescriptor(
            id: "RM001",
            title: "Invalid member expression",
            messageFormat: "The expression can only include property and field access",
            category: "CA1001",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

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

            var argDetailObjects = new List<ArgumentDetail>(); // consider renaming to expressionArguments
            var multiExpressionMethods = new HashSet<MethodDetail>(new MethodDetailArgumentOutputTypesComparer());
            foreach (var invocationExpression in syntaxReceiver.WhenChangedMethods)
            {
                var model = compilation.GetSemanticModel(invocationExpression.SyntaxTree);
                var symbol = model.GetSymbolInfo(invocationExpression).Symbol;

                if (symbol is IMethodSymbol methodSymbol && methodSymbol.ContainingAssembly.Name.Equals(SourceGeneratorAssemblyName))
                {
                    var arguments = invocationExpression.ArgumentList.Arguments;
                    var argDetailObjectsForMethod = new List<ArgumentDetail>(arguments.Count);

                    foreach (var argument in arguments)
                    {
                        if (argument.Expression is LambdaExpressionSyntax lambdaExpression)
                        {
                            // If it's the conversion function, ignore it.
                            if (model.GetTypeInfo(argument.Expression).ConvertedType.Name.Equals("Expression"))
                            {
                                var lambdaOutputType = model.GetTypeInfo(lambdaExpression.Body).Type;
                                argDetailObjectsForMethod.Add(new(lambdaExpression, methodSymbol.TypeArguments[0], lambdaOutputType));
                            }
                        }
                        else if (model.GetTypeInfo(argument.Expression).ConvertedType.Name.Equals("Expression"))
                        {
                            // The argument is evaluates to an expression but it's not inline (could be a variable, method invocation, etc).
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    descriptor: InvalidMemberExpressionError,
                                    location: invocationExpression.GetLocation()));
                            return;
                        }
                    }

                    argDetailObjects.AddRange(argDetailObjectsForMethod);

                    if (argDetailObjectsForMethod.Count > 1)
                    {
                        // var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
                        // System.Diagnostics.Debug.Assert(string.Equals(returnType?.Name, "IObservable"));
                        // var outputType = returnType.TypeArguments[0];
                        var outputType = methodSymbol.TypeArguments.Last();
                        multiExpressionMethods.Add(new(methodSymbol.TypeArguments[0], outputType, argDetailObjectsForMethod));
                    }
                }
            }

            foreach (var group in argDetailObjects.GroupBy(x => x.InputType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), multiExpressionMethods);
                if (classSource != null)
                {
                    context.AddSource($"{group.Key.Name}.WhenChanged.g.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private static string ProcessClass(ITypeSymbol classSymbol, List<ArgumentDetail> propertyExpressionDetailObjects, HashSet<MethodDetail> multiExpressionMethods)
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
                        .GroupBy(x => x.LambdaExpression.Body.ToString())
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

            if (propertyExpressionDetailObjects.Count == 1)
            {
                // Return the observable directly from the method. No dictionary needed.
                if (lambdaExpression.Body is not MemberAccessExpressionSyntax expressionChain)
                {
                    // TODO: Issue a diagnostic stating that only fields and properties are supported.
                    return;
                }

                source.Append(
                    WhenChangedClassBuilder.GetWhenChangedMethodForDirectReturn(
                        inputTypeSymbol.ToDisplayString(),
                        outputTypeSymbol.ToDisplayString(),
                        GetObservableChainCode(expressionChain)));

                return;
            }

            var mapName = $"{inputTypeSymbol.Name}To{outputTypeSymbol.Name}Map";

            var whenChangedMethodCode = WhenChangedClassBuilder.GetWhenChangedMethodForMap(
                inputTypeSymbol.ToDisplayString(),
                outputTypeSymbol.ToDisplayString(),
                mapName);

            var mapEntryBuilder = new StringBuilder();
            foreach (var argumentDetail in propertyExpressionDetailObjects)
            {
                var lambda = argumentDetail.LambdaExpression;
                if (lambda.Body is not MemberAccessExpressionSyntax expressionChain)
                {
                    // TODO: Issue a diagnostic stating that only fields and properties are supported.
                    return;
                }

                var mapKey = lambda.Body.ToString();

                var mapEntryCode = WhenChangedClassBuilder.GetMapEntry(mapKey, GetObservableChainCode(expressionChain));
                mapEntryBuilder.Append(mapEntryCode);
            }

            var mapCode = WhenChangedClassBuilder.GetMap(
                inputTypeSymbol.ToDisplayString(), // Name
                outputTypeSymbol.ToDisplayString(), // Name
                mapName,
                mapEntryBuilder.ToString());

            source.Append(mapCode).Append(whenChangedMethodCode);
        }

        private static string GetObservableChainCode(MemberAccessExpressionSyntax expressionChain)
        {
            var members = new List<MemberAccessExpressionSyntax>();
            while (expressionChain != null)
            {
                members.Add(expressionChain);
                expressionChain = expressionChain.Expression as MemberAccessExpressionSyntax;
            }

            members.Reverse();

            var observableChainBuilder = new StringBuilder();
            foreach (var memberAccessExpression in members)
            {
                var observableChainCode = WhenChangedClassBuilder.GetMapEntryChain(memberAccessExpression.Name.ToString());
                observableChainBuilder.Append(observableChainCode);
            }

            return observableChainBuilder.ToString();
        }

        private static void ProcessMultiExpressionMethods(StringBuilder source, HashSet<MethodDetail> multiExpressionMethods)
        {
            // foreach (var method in multiExpressionMethods.GroupBy(x => string.Join("-", x.Arguments.Select(x => x.InputType.ToDisplayString() + x.OutputType.ToDisplayString()))).Select(x => x.First()))
            foreach (var method in multiExpressionMethods)
            {
                string bodyCode = WhenChangedClassBuilder.GetMultiExpressionMethodBody(method.Arguments.Count);
                var tempReturnTypes = method.Arguments
                    .Select(x => x.OutputType.ToDisplayString())
                    .ToList();

                var parametersCode = WhenChangedClassBuilder.GetMultiExpressionMethodParameters(method.InputType.ToDisplayString(), method.OutputType.ToDisplayString(), tempReturnTypes, method.Arguments.Count);
                var methodCode = WhenChangedClassBuilder.GetMultiExpressionMethod(method.InputType.ToDisplayString(), method.OutputType.ToDisplayString(), parametersCode, bodyCode);
                source.Append(methodCode);
            }
        }
    }
}
