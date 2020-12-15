// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed class MethodTypeArgumentsComparer : IEqualityComparer<MethodDetail>
    {
        public bool Equals(MethodDetail x, MethodDetail y)
        {
            if (!SymbolEqualityComparer.Default.Equals(x?.InputType, y?.InputType) ||
                !SymbolEqualityComparer.Default.Equals(x?.OutputType, y?.OutputType))
            {
                return false;
            }

            if (x?.Arguments == null && y?.Arguments == null)
            {
                return true;
            }
            else if (x?.Arguments == null || y?.Arguments == null)
            {
                return false;
            }
            else if (x.Arguments.Count != y.Arguments.Count)
            {
                return false;
            }

            for (int i = 0; i < x.Arguments.Count; i++)
            {
                var outputTypeX = x.Arguments[i]?.OutputType;
                var outputTypeY = y.Arguments[i]?.OutputType;

                if (!SymbolEqualityComparer.Default.Equals(outputTypeX, outputTypeY))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(MethodDetail obj)
        {
            int hashCode = -2037187358;
            hashCode = (hashCode * -1521134295) + SymbolEqualityComparer.Default.GetHashCode(obj.InputType);
            hashCode = (hashCode * -1521134295) + SymbolEqualityComparer.Default.GetHashCode(obj.OutputType);
            foreach (var argument in obj.Arguments)
            {
                hashCode = (hashCode * -1521134295) + SymbolEqualityComparer.Default.GetHashCode(argument.OutputType);
            }

            return hashCode;
        }
    }
}
