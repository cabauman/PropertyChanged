// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
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
            title: "Invalid member access expression",
            messageFormat: "The expression must be inline (e.g. not a variable or method invocation).",
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

            var classData = requiredData.ExpressionArguments
                .GroupBy(x => x.InputType)
                .Select(x => x
                    .GroupBy(y => y.OutputType)
                    .Select(y => y
                        .DistinctBy(z => z.LambdaExpression.Body.ToString())
                        .ToOuputTypeGroup())
                    .ToInputTypeGroup(x.Key))
                .GroupJoin(
                    requiredData.MultiExpressionMethodData,
                    x => x.InputTypeName,
                    x => x.InputTypeName,
                    (inputTypeGroup, multiExpressionMethodData) =>
                    {
                        var allMethodData = inputTypeGroup
                            .OutputTypeGroups
                            .Select(CreateSingleExpressionMethodDatum)
                            .Concat(multiExpressionMethodData);

                        return new ClassDatum(inputTypeGroup.InputTypeName, allMethodData);
                    });

            var sourceCreator = new StringBuilderSourceCreator();
            foreach (var @class in classData)
            {
                var source = sourceCreator.Create(@class);
                context.AddSource($"WhenChanged.{@class.InputTypeName}.g.cs", SourceText.From(source, Encoding.UTF8));
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

                    foreach (var argument in arguments)
                    {
                        if (model.GetTypeInfo(argument.Expression).ConvertedType.Name.Equals("Expression"))
                        {
                            if (argument.Expression is LambdaExpressionSyntax lambdaExpression)
                            {
                                var lambdaInputType = methodSymbol.TypeArguments[0];
                                var lambdaOutputType = model.GetTypeInfo(lambdaExpression.Body).Type;
                                var expressionChain = GetExpressionChain(lambdaExpression);
                                expressionArguments.Add(new(lambdaExpression, expressionChain, lambdaInputType, lambdaOutputType));
                                allExpressionArgumentsAreValid &= expressionChain != null;
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

        private static MethodDatum CreateSingleExpressionMethodDatum(OutputTypeGroup<ExpressionArgument> outputTypeGroup)
        {
            MethodDatum methodDatum = null;

            var (lambdaExpression, expressionChain, inputTypeSymbol, outputTypeSymbol) = outputTypeGroup.ArgumentData.First();
            var (inputTypeName, outputTypeName) = (inputTypeSymbol.ToDisplayString(), outputTypeSymbol.ToDisplayString());

            if (outputTypeGroup.ArgumentData.Count == 1)
            {
                methodDatum = new SingleExpressionOptimizedImplMethodDatum(inputTypeName, outputTypeName, expressionChain);
            }
            else if (outputTypeGroup.ArgumentData.Count > 1)
            {
                // TODO: Consider including namespace to prevent potential name conflicts.
                var mapName = $"{inputTypeSymbol.Name}To{outputTypeSymbol.Name}Map";

                var entries = new List<MapEntryDatum>(outputTypeGroup.ArgumentData.Count);
                foreach (var argumentDatum in outputTypeGroup.ArgumentData)
                {
                    var mapKey = argumentDatum.LambdaExpression.Body.ToString();
                    var mapEntry = new MapEntryDatum(mapKey, argumentDatum.ExpressionChain);
                    entries.Add(mapEntry);
                }

                var map = new MapDatum(mapName, entries);
                methodDatum = new SingleExpressionDictionaryImplMethodDatum(inputTypeName, outputTypeName, map);
            }

            return methodDatum;
        }

        private static List<string> GetExpressionChain(LambdaExpressionSyntax lambdaExpression)
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

            if (expression is not IdentifierNameSyntax firstLinkInChain)
            {
                // It stopped before reaching the lambda parameter, so the expression is invalid.
                return null;
            }

            var lambdaParameterName =
                (lambdaExpression as SimpleLambdaExpressionSyntax)?.Parameter.Identifier.ToString() ??
                (lambdaExpression as ParenthesizedLambdaExpressionSyntax)?.ParameterList.Parameters[0].Identifier.ToString();

            if (string.Equals(lambdaParameterName, firstLinkInChain.Identifier.ToString(), StringComparison.InvariantCulture))
            {
                members.Reverse();
                return members;
            }

            return null;
        }
    }
}
