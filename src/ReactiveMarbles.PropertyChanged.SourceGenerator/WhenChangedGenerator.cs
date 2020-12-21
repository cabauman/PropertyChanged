// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var stubSource = WhenChangedClassBuilder.GetWhenChangedStubClass();
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(stubSource, Encoding.UTF8), options));
            context.AddSource($"WhenChanged.Stubs.g.cs", SourceText.From(stubSource, Encoding.UTF8));

            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
            {
                return;
            }

            RequiredData requiredData = ExtractRequiredData(context, compilation, syntaxReceiver);

            if (!requiredData.AllExpressionArgumentsAreValid)
            {
                return;
            }

            var inputTypeGroups = requiredData.ExpressionArguments
                .GroupBy(x => x.InputType)
                .Select(x => x
                    .GroupBy(y => y.OutputType)
                    .Select(y => y
                        .DistinctBy(z => z.LambdaExpression.Body.ToString())
                        .ToOuputGroup())
                    .ToInputGroup(x.Key))
                .GroupJoin(
                    requiredData.MultiExpressionMethodData,
                    x => x.InputTypeName,
                    x => x.InputType,
                    (a, b) => (SingleExpressionMethodData: a, MultiExpressionMethodData: b));

            List<ClassBlueprint> classes = ConvertToClassBlueprints(inputTypeGroups);

            var sourceBuilder = new SourceBuilder();
            foreach (var @class in classes)
            {
                var source = sourceBuilder.Build(@class);
            }
        }

        private static RequiredData ExtractRequiredData(GeneratorExecutionContext context, Compilation compilation, SyntaxReceiver syntaxReceiver)
        {
            var allExpressionArgumentsAreValid = true;
            var expressionArguments = new List<ExpressionArgument>();
            var multiExpressionMethodData = new HashSet<MultiExpressionMethodDatum>();

            foreach (var invocationExpression in syntaxReceiver.WhenChangedMethods)
            {
                var model = compilation.GetSemanticModel(invocationExpression.SyntaxTree);
                var symbol = model.GetSymbolInfo(invocationExpression).Symbol;

                if (symbol is IMethodSymbol methodSymbol)
                {
                    var arguments = invocationExpression.ArgumentList.Arguments;
                    var expressionArgumentsForMethod = new List<ExpressionArgument>(arguments.Count);

                    foreach (var argument in arguments)
                    {
                        if (model.GetTypeInfo(argument.Expression).ConvertedType.Name.Equals("Expression"))
                        {
                            if (argument.Expression is LambdaExpressionSyntax lambdaExpression)
                            {
                                var lambdaInputType = methodSymbol.TypeArguments[0];
                                var lambdaOutputType = model.GetTypeInfo(lambdaExpression.Body).Type;
                                expressionArguments.Add(new(lambdaExpression, lambdaInputType, lambdaOutputType));

                                // TODO: Consider using GetChain and saving it to avoid recalculating it again later.
                                allExpressionArgumentsAreValid &= ValidateExpressionChain(lambdaExpression);
                            }
                            else
                            {
                                // The argument is evaluates to an expression but it's not inline (could be a variable, method invocation, etc).
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        descriptor: InvalidMemberExpressionError,
                                        location: argument.GetLocation()));

                                allExpressionArgumentsAreValid = false;
                            }
                        }
                    }

                    if (methodSymbol.TypeArguments.Length > 2)
                    {
                        multiExpressionMethodData.Add(new(methodSymbol.TypeArguments.Select(x => x.ToDisplayString())));
                    }
                }
            }

            return new RequiredData(allExpressionArgumentsAreValid, expressionArguments, multiExpressionMethodData);
        }

        private static List<ClassBlueprint> ConvertToClassBlueprints(IEnumerable<(InputGroup<ExpressionArgument> SingleExpressionMethodData, IEnumerable<MultiExpressionMethodDatum> MultiExpressionMethodData)> inputTypeGroups)
        {
            var classes = new List<ClassBlueprint>();

            foreach (var inputTypeGroup in inputTypeGroups)
            {
                var dictionaryImplementationMethodData = new List<SingleExpressionDictionaryImplMethodDatum>();
                var optimizedImplementationMethodData = new List<SingleExpressionOptimizedImplMethodDatum>();
                foreach (var outputTypeGroup in inputTypeGroup.SingleExpressionMethodData.OutputTypeGroups)
                {
                    var (lambdaExpression, inputTypeSymbol, outputTypeSymbol) = outputTypeGroup.ArgumentData.First();
                    var (inputTypeName, outputTypeName) = (inputTypeSymbol.ToDisplayString(), outputTypeSymbol.ToDisplayString());

                    if (outputTypeGroup.ArgumentData.Count == 1)
                    {
                        var methodData = new SingleExpressionOptimizedImplMethodDatum(inputTypeName, outputTypeName, GetChain(lambdaExpression));
                        optimizedImplementationMethodData.Add(methodData);
                    }
                    else if (outputTypeGroup.ArgumentData.Count > 1)
                    {
                        // TODO: Consider including namespace to prevent potential name conflicts.
                        var mapName = $"{inputTypeSymbol.Name}To{outputTypeSymbol.Name}Map";

                        var entries = new List<MapEntryBlueprint>(outputTypeGroup.ArgumentData.Count);
                        foreach (var argumentDatum in outputTypeGroup.ArgumentData)
                        {
                            var mapKey = lambdaExpression.Body.ToString();
                            var mapEntry = new MapEntryBlueprint(mapKey, GetChain(lambdaExpression));
                            entries.Add(mapEntry);
                        }

                        var map = new MapBlueprint(mapName, entries);
                        var methodData = new SingleExpressionDictionaryImplMethodDatum(inputTypeName, outputTypeName, map);
                        dictionaryImplementationMethodData.Add(methodData);
                    }
                }

                classes.Add(new(dictionaryImplementationMethodData, optimizedImplementationMethodData, inputTypeGroup.MultiExpressionMethodData));
            }

            return classes;
        }

        private static bool ValidateExpressionChain(LambdaExpressionSyntax lambdaExpression)
        {
            var expression = lambdaExpression.ExpressionBody;
            var expressionChain = expression as MemberAccessExpressionSyntax;

            while (expressionChain != null)
            {
                expression = expressionChain.Expression;
                expressionChain = expression as MemberAccessExpressionSyntax;
            }

            if (expression is not IdentifierNameSyntax firstLinkInChain)
            {
                // It stopped before reaching the lambda parameter, so the expression is invalid.
                return false;
            }

            var lambdaParameterName = (lambdaExpression as SimpleLambdaExpressionSyntax)?.Parameter.Identifier.ToString() ??
                (lambdaExpression as ParenthesizedLambdaExpressionSyntax)?.ParameterList.Parameters[0].Identifier.ToString();

            return string.Equals(lambdaParameterName, firstLinkInChain.Identifier.ToString(), StringComparison.InvariantCulture);
        }

        private static List<string> GetChain(LambdaExpressionSyntax lambdaExpression)
        {
            var members = new List<string>();
            var expression = lambdaExpression.ExpressionBody;
            var expressionChain = expression as MemberAccessExpressionSyntax;
            while (expressionChain != null)
            {
                members.Add(expressionChain.Name.ToString());
                expression = expressionChain.Expression;
                expressionChain = expression as MemberAccessExpressionSyntax;
            }

            if (expression is not IdentifierNameSyntax)
            {
                // TODO: It stopped before reaching the lambda parameter, so the expression is invalid.
            }

            members.Reverse();

            return members;
        }
    }
}
