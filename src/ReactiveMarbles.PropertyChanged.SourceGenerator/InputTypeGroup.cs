﻿// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal readonly struct InputTypeGroup<T>
    {
        public InputTypeGroup(string inputTypeName, IEnumerable<OutputTypeGroup<T>> outputTypeGroups)
        {
            InputTypeName = inputTypeName;
            OutputTypeGroups = outputTypeGroups;
        }

        public string InputTypeName { get; }

        public IEnumerable<OutputTypeGroup<T>> OutputTypeGroups { get; }
    }
}
