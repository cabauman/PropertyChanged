// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal static class Extensions
    {
        public static InputTypeGroup<T> ToInputTypeGroup<T>(this IEnumerable<OutputTypeGroup<T>> source, ITypeSymbol inputType)
        {
            return new InputTypeGroup<T>(inputType.ToDisplayString(), source);
        }

        public static OutputTypeGroup<T> ToOuputTypeGroup<T>(this IEnumerable<T> source)
        {
            return new OutputTypeGroup<T>(source.ToList());
        }
    }
}
