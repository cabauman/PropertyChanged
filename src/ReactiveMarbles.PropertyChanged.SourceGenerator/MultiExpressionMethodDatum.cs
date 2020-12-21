// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed class MultiExpressionMethodDatum : IEquatable<MultiExpressionMethodDatum>
    {
        public MultiExpressionMethodDatum(IEnumerable<string> typeNames)
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

        public static bool operator ==(MultiExpressionMethodDatum left, MultiExpressionMethodDatum right)
        {
            return EqualityComparer<MultiExpressionMethodDatum>.Default.Equals(left, right);
        }

        public static bool operator !=(MultiExpressionMethodDatum left, MultiExpressionMethodDatum right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MultiExpressionMethodDatum);
        }

        public bool Equals(MultiExpressionMethodDatum other)
        {
            return other != null &&
                   InputType == other.InputType &&
                   OutputType == other.OutputType &&
                   EqualityComparer<List<string>>.Default.Equals(TempReturnTypes, other.TempReturnTypes);
        }

        public override int GetHashCode()
        {
            int hashCode = 1230885993;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(InputType);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(OutputType);
            hashCode = (hashCode * -1521134295) + EqualityComparer<List<string>>.Default.GetHashCode(TempReturnTypes);
            return hashCode;
        }
    }
}
