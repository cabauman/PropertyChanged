// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record StringBuilderSourceCreator : ISourceCreator
    {
        public string Create(ClassDatum @class)
        {
            var sb = new StringBuilder();
            foreach (var methodDatum in @class.MethodData)
            {
                sb.AppendLine(methodDatum.BuildSource(this));
            }

            return WhenChangedClassBuilder.GetClass(sb.ToString());
        }

        public string Create(SingleExpressionDictionaryImplMethodDatum methodDatum)
        {
            var mapEntrySb = new StringBuilder();
            foreach (var entry in methodDatum.Map.Entries)
            {
                var valueChainSb = new StringBuilder();
                foreach (var memberName in entry.MemberNames)
                {
                    valueChainSb.Append(WhenChangedClassBuilder.GetMapEntryChain(memberName));
                }

                mapEntrySb.Append(WhenChangedClassBuilder.GetMapEntry(entry.Key, valueChainSb.ToString()));
            }

            var map = WhenChangedClassBuilder.GetMap(methodDatum.InputType, methodDatum.OutputType, methodDatum.Map.MapName, mapEntrySb.ToString());
            var method = WhenChangedClassBuilder.GetWhenChangedMethodForMap(methodDatum.InputType, methodDatum.OutputType, methodDatum.Map.MapName);

            return map + "\n" + method;
        }

        public string Create(SingleExpressionOptimizedImplMethodDatum methodDatum)
        {
            var sb = new StringBuilder();
            foreach (var memberName in methodDatum.MemberNames)
            {
                sb.Append(WhenChangedClassBuilder.GetMapEntryChain(memberName));
            }

            return WhenChangedClassBuilder.GetWhenChangedMethodForDirectReturn(methodDatum.InputType, methodDatum.OutputType, sb.ToString());
        }

        public string Create(MultiExpressionMethodDatum methodDatum)
        {
            var expressionParameters = WhenChangedClassBuilder.GetMultiExpressionMethodParameters(methodDatum.InputTypeName, methodDatum.OutputTypeName, methodDatum.TempReturnTypes);
            var body = WhenChangedClassBuilder.GetMultiExpressionMethodBody(methodDatum.TempReturnTypes.Count);

            return WhenChangedClassBuilder.GetMultiExpressionMethod(methodDatum.InputTypeName, methodDatum.OutputTypeName, expressionParameters, body);
        }
    }
}
