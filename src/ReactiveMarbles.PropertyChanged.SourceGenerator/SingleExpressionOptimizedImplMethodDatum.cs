// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record SingleExpressionOptimizedImplMethodDatum
    {
        public SingleExpressionOptimizedImplMethodDatum(string inputType, string outputType, List<string> memberNames)
        {
            InputType = inputType;
            OutputType = outputType;
            MemberNames = memberNames;
        }

        public string InputType { get; }

        public string OutputType { get; }

        public List<string> MemberNames { get; }
    }
}
