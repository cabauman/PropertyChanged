// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record SingleExpressionDictionaryImplMethodDatum
    {
        public SingleExpressionDictionaryImplMethodDatum(string inputType, string outputType, MapBlueprint map)
        {
            InputType = inputType;
            OutputType = outputType;
            Map = map;
        }

        public string InputType { get; }

        public string OutputType { get; }

        public MapBlueprint Map { get; }
    }
}
