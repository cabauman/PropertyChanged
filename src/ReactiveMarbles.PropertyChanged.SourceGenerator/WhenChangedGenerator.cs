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
    public record InputGroup<T>(ITypeSymbol InputType, List<OutputGroup<T>> OutputTypeGroups);

    public record OutputGroup<T>(List<T> ArgumentData);

    public static class MyExtensions
    {
        public static IEnumerable<T> DistinctBy<T, T2>(this IEnumerable<T> source, Func<T, T2> func)
        {
            return source
                .GroupBy(x => func(x))
                .Select(x => x.First());
        }

        public static InputGroup<T> ToInputGroup<T>(this IEnumerable<OutputGroup<T>> source, ITypeSymbol inputType)
        {

        }

        public static OutputGroup<T> ToOuputGroup<T>(this IEnumerable<T> source)
        {

        }
    }

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

            var argDetailObjects = new List<ExpressionArgument>(); // consider renaming to expressionArguments
            var multiExpressionMethods = new HashSet<MethodDetail>(new MethodTypeArgumentsComparer());
            foreach (var invocationExpression in syntaxReceiver.WhenChangedMethods)
            {
                var model = compilation.GetSemanticModel(invocationExpression.SyntaxTree);
                var symbol = model.GetSymbolInfo(invocationExpression).Symbol;

                if (symbol is IMethodSymbol methodSymbol)
                {
                    var arguments = invocationExpression.ArgumentList.Arguments;
                    var argDetailObjectsForMethod = new List<ExpressionArgument>(arguments.Count);

                    var isValid = true;
                    foreach (var argument in arguments)
                    {
                        if (argument.Expression is LambdaExpressionSyntax lambdaExpression)
                        {
                            var results = lambdaExpression.Body.DescendantNodesAndSelf().ToLookup(y => y.IsKind(SyntaxKind.SimpleMemberAccessExpression));

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
                                    location: argument.GetLocation()));
                            isValid = false;
                            break;
                        }
                    }

                    if (!isValid)
                    {
                        continue;
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
                    context.AddSource($"WhenChanged.{group.Key.Name}.g.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private static void Execute2(GeneratorExecutionContext context)
        {
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var stubSource = WhenChangedClassBuilder.GetWhenChangedStubClass();
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(stubSource, Encoding.UTF8), options));
            context.AddSource($"WhenChanged.Stubs.g.cs", SourceText.From(stubSource, Encoding.UTF8));

            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
            {
                return;
            }

            Func<InvocationExpressionSyntax, IEnumerable<(LambdaExpressionSyntax lambda, ITypeSymbol inputType, ITypeSymbol outputType)>> modelFactory = x =>
            {
                var model = compilation.GetSemanticModel(x.SyntaxTree);
                var methodSymbol = model.GetSymbolInfo(x).Symbol as IMethodSymbol;
                var containingType = compilation.GetTypeByMetadataName("NotifyPropertyChangedExtensions");
                var nameOfFirstParamType = methodSymbol.Parameters.First().Type.Name;
                Debug.Assert(methodSymbol.Parameters.First().Type.Equals(methodSymbol.TypeArguments.First(), SymbolEqualityComparer.Default), "The first parameter is an unexpected type.");
                var isWhenChangedInvocation = methodSymbol.ContainingType.Equals(containingType, SymbolEqualityComparer.Default);

                var lambdaExpression = x.ArgumentList.Arguments.First().Expression as LambdaExpressionSyntax;
                var memberNames = GetChain(lambdaExpression);
                var mapKey = lambdaExpression.Body.ToString();
                var mapEnty = new MapEntryBlueprint(mapKey, memberNames);

                var mapName = string.Empty;

                var typeArgumentNames = methodSymbol.TypeArguments.Select(x => x.ToDisplayString());
                if (typeArgumentNames.Count() > 2)
                {
                    var multiExpressionMethod = new Method3Blueprint(typeArgumentNames);
                }
                else if (typeArgumentNames.Count() == 2)
                {
                    var expressionMethod = new MethodBlueprint(typeArgumentNames.First(), typeArgumentNames.Last(), mapName);
                }

                return x.ArgumentList.Arguments
                    .Where(
                        x =>
                            x.Expression is LambdaExpressionSyntax &&
                            model.GetTypeInfo(x.Expression).ConvertedType.Name.Equals("Expression"))
                    //.Select(x => x.Expression.DescendantNodesAndSelf().ToLookup(y => y.IsKind(SyntaxKind.SimpleMemberAccessExpression)))
                    .Select((x, i) => (x.Expression as LambdaExpressionSyntax, methodSymbol.TypeArguments[0], methodSymbol.TypeArguments[i + 1]));
            };

            var result = syntaxReceiver.WhenChangedMethods
                .SelectMany(
                    x =>
                    {
                        var model = compilation.GetSemanticModel(x.SyntaxTree);
                        var methodSymbol = model.GetSymbolInfo(x).Symbol as IMethodSymbol;
                        return x.ArgumentList.Arguments
                            .Select((x, i) => (lambda: x.Expression as LambdaExpressionSyntax, inputType: methodSymbol.TypeArguments[0], outputType: methodSymbol.TypeArguments[i + 1]));
                    })
                //.SelectMany(x => modelFactory(x))
                .GroupBy(x => x.inputType)
                .GroupJoin(new List<Method3Blueprint>(), x => x.Key.Name, x => x.InputType, (a, b) => new { A = a, B = b })
                .Select(
                    x =>
                        (multiExpressions: x.B,
                        singleExpressions: x.A
                            //.GroupBy(y => y.outputType)
                            //.Select(z => z
                            //    .GroupBy(w => w.lambda.Body.ToString())
                            //    .Select(v => v.First()))
                            //.ToLookup(x => x.Count() > 1)));
                            .GroupBy(
                                y =>
                                    y.outputType)
                            .Select(
                                z =>
                                    z
                                        .GroupBy(
                                            w =>
                                                w.lambda.Body.ToString())
                                        .Select(v => v.First()))
                            .ToLookup(x => x.Count() > 1)));

            var result2 = syntaxReceiver.WhenChangedMethods
                .SelectMany(
                    x =>
                    {
                        var model = compilation.GetSemanticModel(x.SyntaxTree);
                        var methodSymbol = model.GetSymbolInfo(x).Symbol as IMethodSymbol;
                        return x.ArgumentList.Arguments
                            .Select((x, i) => (lambda: x.Expression as LambdaExpressionSyntax, inputType: methodSymbol.TypeArguments[0], outputType: methodSymbol.TypeArguments[i + 1]));
                    })
                //.GroupByInputType()
                .GroupBy(x => x.inputType)
                .Select(x => x
                    .GroupBy(y => y.outputType)
                    .Select(y => y
                        .GroupBy(z => z.lambda.Body.ToString())
                        .Select(z => z.First())));

            var multiExpressionMethodData = syntaxReceiver.WhenChangedMethods
                .Select(x => compilation.GetSemanticModel(x.SyntaxTree).GetSymbolInfo(x).Symbol as IMethodSymbol)
                .Where(x => x.TypeArguments.Length > 2)
                .Select(x => new Method3Blueprint(x.TypeArguments.Select(y => y.ToDisplayString())));

            var inputTypeGroups = syntaxReceiver.WhenChangedMethods
                .SelectMany(
                    x =>
                    {
                        // typeArgumentNames: methodSymbol.TypeArguments.Select(x => x.ToDisplayString().ToString())
                        // .GroupJoin(new List<Method3Blueprint>(), x => x.Key.Name, x => x.InputType, (a, b) => new { A = a, B = b })
                        var model = compilation.GetSemanticModel(x.SyntaxTree);
                        var methodSymbol = model.GetSymbolInfo(x).Symbol as IMethodSymbol;
                        return x.ArgumentList.Arguments
                            .Select((x, i) => (lambda: x.Expression as LambdaExpressionSyntax, inputType: methodSymbol.TypeArguments[0], outputType: methodSymbol.TypeArguments[i + 1]);
                    })
                .GroupBy(x => x.inputType)
                .Select(x => x
                    .GroupBy(y => y.outputType)
                    .Select(y => y
                        .DistinctBy(z => z.lambda.Body.ToString())
                        .ToOuputGroup())
                    .ToInputGroup(x.Key))
                .GroupJoin(
                    multiExpressionMethodData,
                    x => x.InputType.Name,
                    x => x.InputType,
                    (a, b) => (SingleExpressionMethodData: a, MultiExpressionMethodData: b));

            var model = compilation.GetSemanticModel(x.SyntaxTree);
            var methodSymbol = model.GetSymbolInfo(x).Symbol as IMethodSymbol;

            var classes = new List<ClassBlueprint>();

            foreach (var inputTypeGroup in inputTypeGroups)
            {
                var dictionaryImplementationMethodData = new List<MethodBlueprint>();
                var optimizedImplementationMethodData = new List<Method2Blueprint>();
                foreach (var outputTypeGroup in inputTypeGroup.SingleExpressionMethodData.OutputTypeGroups)
                {
                    var (lambdaExpression, inputTypeSymbol, outputTypeSymbol) = outputTypeGroup.ArgumentData.First();
                    var (inputTypeName, outputTypeName) = (inputTypeSymbol.ToDisplayString(), outputTypeSymbol.ToDisplayString());

                    if (outputTypeGroup.ArgumentData.Count == 1)
                    {
                        var methodData = new Method2Blueprint(inputTypeName, outputTypeName, GetChain(lambdaExpression));
                        optimizedImplementationMethodData.Add(methodData);
                    }
                    else if (outputTypeGroup.ArgumentData.Count > 1)
                    {
                        var mapName = $"{inputTypeSymbol.Name}To{outputTypeSymbol.Name}Map";

                        var entries = new List<MapEntryBlueprint>(outputTypeGroup.ArgumentData.Count);
                        foreach (var argumentDatum in outputTypeGroup.ArgumentData)
                        {
                            var mapKey = lambdaExpression.Body.ToString();
                            var mapEntry = new MapEntryBlueprint(mapKey, GetChain(lambdaExpression));
                            entries.Add(mapEntry);
                        }

                        var map = new MapBlueprint(mapName, entries);
                        var methodData = new MethodBlueprint(inputTypeName, outputTypeName, map);
                        dictionaryImplementationMethodData.Add(methodData);
                    }
                }

                classes.Add(new(dictionaryImplementationMethodData, optimizedImplementationMethodData, inputTypeGroup.MultiExpressionMethodData));
            }

            var blah = result
                .Select(x => new Class2Blueprint(x.multiExpressions));

            foreach (var @class in result)
            {
                foreach (var method in @class.singleExpressions[false])
                {
                    foreach (var key in method)
                    {
                        Console.WriteLine(key);
                    }
                }

                foreach (var method in @class.multiExpressions)
                {
                    Console.WriteLine(method);
                }
            }
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

        public class ClassBlueprint
        {
            public ClassBlueprint(
                IEnumerable<MethodBlueprint> singleExpressionDictionaryImplMethodData,
                IEnumerable<Method2Blueprint> singleExpressionOptimizedImplMethodData,
                IEnumerable<Method3Blueprint> multiExpressionMethodData)
            {
            }

            public List<MethodBlueprint> SingleExpressionDictionaryImplMethodData { get; }

            public List<Method2Blueprint> SingleExpressionOptimizedImplMethodData { get; }

            public List<Method3Blueprint> MultiExpressionMethodData { get; }
        }

        public class Class2Blueprint
        {
            public Class2Blueprint(IEnumerable<Method3Blueprint> multiExpressions)
            {
            }

            public List<MethodBlueprint> SingleExpressionMethods { get; }

            public List<Method3Blueprint> MultiExpressionMethods { get; }
        }

        public class NewMethodBlueprint
        {
            public string GetSource(/*ISourceBuilder sourceBuilder*/)
            {
                // sourceBuilder.Build(inputType, outputType, lambdaExpression);
                return string.Empty;
            }
        }

        public class MethodBlueprint
        {
            public MethodBlueprint(string inputType, string outputType, MapBlueprint map)
            {
                InputType = inputType;
                OutputType = outputType;
                Map = map;
            }

            public string InputType { get; }

            public string OutputType { get; }

            public MapBlueprint Map { get; }
        }

        public class Method2Blueprint
        {
            public Method2Blueprint(string inputType, string outputType, List<string> memberNames)
            {
                InputType = inputType;
                OutputType = outputType;
                MemberNames = memberNames;
            }

            public string InputType { get; }

            public string OutputType { get; }

            public List<string> MemberNames { get; }
        }

        public class Method3Blueprint
        {
            public Method3Blueprint(IEnumerable<string> typeNames)
            {
                var list = typeNames.ToArray();
                InputType = list[0];
                OutputType = list[list.Length - 1];
                TempReturnTypes = new List<string>(list.Length - 2);
                for (int i = 1; i < list.Length - 1; i++)
                {
                    TempReturnTypes.Add(list[i]);
                }
            }

            public string InputType { get; }

            public string OutputType { get; }

            public List<string> TempReturnTypes { get; }
        }

        public class MapBlueprint
        {
            public MapEntryBlueprint(string mapName, List<MapEntryBlueprint> entries)
            {
                MapName = mapName;
                Entries = entries;
            }

            public string InputType { get; }

            public string OutputType { get; }

            public string MapName { get; }

            public List<MapEntryBlueprint> Entries { get; }
        }

        public class MapEntryBlueprint
        {
            public MapEntryBlueprint(string key, List<string> memberNames)
            {
                Key = key;
                MemberNames = memberNames;
            }

            public string Key { get; }

            public List<string> MemberNames { get; }
        }

        private static string ProcessClass(ITypeSymbol classSymbol, List<ExpressionArgument> propertyExpressionDetailObjects, HashSet<MethodDetail> multiExpressionMethods)
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

        private static void ProcessMethod(StringBuilder source, List<ExpressionArgument> propertyExpressionDetailObjects)
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
                inputTypeSymbol.ToDisplayString(),
                outputTypeSymbol.ToDisplayString(),
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
