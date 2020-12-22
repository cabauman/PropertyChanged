// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record MultiExpressionMethodDatum : MethodDatum, IEquatable<MultiExpressionMethodDatum>
    {
        public MultiExpressionMethodDatum(IEnumerable<string> typeNames)
        {
            var list = typeNames.ToArray();
            InputTypeName = list[0];
            OutputTypeName = list[list.Length - 1];
            TempReturnTypes = new List<string>(list.Length - 2);
            for (int i = 1; i < list.Length - 1; i++)
            {
                TempReturnTypes.Add(list[i]);
            }
        }

        public string InputTypeName { get; }

        public string OutputTypeName { get; }

        public List<string> TempReturnTypes { get; }

        public override string BuildSource(ISourceCreator sourceCreator)
        {
            return sourceCreator.Create(this);
        }

        public bool Equals(MultiExpressionMethodDatum other)
        {
            if (other is null)
            {
                return false;
            }

            var result =
                InputTypeName == other.InputTypeName &&
                OutputTypeName == other.OutputTypeName &&
                TempReturnTypes.Count == other.TempReturnTypes.Count;

            if (!result)
            {
                return false;
            }

            for (int i = 0; i < TempReturnTypes.Count; ++i)
            {
                result &= EqualityComparer<string>.Default.Equals(TempReturnTypes[i], other.TempReturnTypes[i]);
            }

            return result;
        }

        public override int GetHashCode()
        {
            int hashCode = 1230885993;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(InputTypeName);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(OutputTypeName);

            foreach (var typeName in TempReturnTypes)
            {
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(typeName);
            }

            return hashCode;
        }
    }
}
