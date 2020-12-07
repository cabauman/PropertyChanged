// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace ReactiveMarbles.PropertyChanged
{
    internal static class WhenChangedClassBuilder
    {
        public static string GetMultiExpressionMethodParameters(string inputType, string outputType, List<string> tempReturnTypes, int counter)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < counter; i++)
            {
                sb.AppendLine($"            Expression<Func<{inputType}, {tempReturnTypes[i]}>> propertyExpression{i + 1},");
            }

            sb.Append("            Func<");
            for (int i = 0; i < counter; i++)
            {
                sb.Append($"{tempReturnTypes[i]}, ");
            }

            sb.Append($"{outputType}> conversionFunc)");

            return sb.ToString();
        }

        public static string GetMultiExpressionMethodBody(int counter)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < counter; i++)
            {
                sb.AppendLine($"            var obs{i + 1} = objectToMonitor.WhenChanged(propertyExpression{i + 1});");
            }

            sb.Append("            return obs1.CombineLatest(");
            for (int i = 1; i < counter; i++)
            {
                sb.Append($"obs{i + 1}, ");
            }

            sb.Append("conversionFunc);");

            return sb.ToString();
        }

        public static string GetMultiExpressionMethod(string inputType, string outputType, string expressionParameters, string body)
        {
            return $@"
        public static IObservable<{outputType}> WhenChanged(
            this {inputType} objectToMonitor,
{expressionParameters}
        {{
{body}
        }}
";
        }

        public static string GetWhenChangedMethod(string inputType, string outputType, string mapName)
        {
            return $@"
        public static IObservable<{outputType}> WhenChanged(this {inputType} source, Expression<Func<{inputType}, {outputType}>> propertyExpression)
        {{
            var body = propertyExpression.Body.ToString();
            var key = body.Substring(body.IndexOf('.') + 1);
            return {mapName}[key].Invoke(source);
        }}
";
        }

        public static string GetMapEntryChain(string memberName)
        {
            return $@"
                    .Where(x => x != null)
                    .Select(x => GenerateObservable(x, ""{memberName}"", y => y.{memberName}))
                    .Switch()";
        }

        public static string GetMapEntry(string key, string valueChain)
        {
            return $@"
            {{
                ""{key}"",
                source => Observable.Return(source){valueChain}
            }},";
        }

        public static string GetMap(string inputType, string outputType, string mapName, string entries)
        {
            return $@"
        private static readonly Dictionary<string, Func<{inputType}, IObservable<{outputType}>>> {mapName} = new Dictionary<string, Func<{inputType}, IObservable<{outputType}>>>()
        {{
{entries}
        }};
";
        }

        public static string GetClass(string namespaceName, string className, string body)
        {
            return $@"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;

namespace {namespaceName}
{{
    public static class {className}WhenChanged
    {{
        private static IObservable<T> GenerateObservable<TObj, T>(
            TObj parent,
            string memberName,
            Func<TObj, T> getter)
            where TObj : INotifyPropertyChanged
        {{
            return Observable.FromEvent<PropertyChangedEventHandler, (object Sender, PropertyChangedEventArgs Args)>(
                    handler =>
                    {{
                        void Handler(object sender, PropertyChangedEventArgs e) => handler((sender, e));
                        return Handler;
                    }},
                    x => parent.PropertyChanged += x,
                    x => parent.PropertyChanged -= x)
                .Where(x => x.Args.PropertyName == memberName)
                .Select(x => getter(parent))
                .StartWith(getter(parent));
        }}

        {body}
    }}
}}
";
        }
    }
}
