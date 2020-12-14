// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record MethodDetail(ITypeSymbol InputType, ITypeSymbol OutputType, List<ArgumentDetail> Arguments);

    internal sealed record ArgumentDetail(LambdaExpressionSyntax LambdaExpression, ITypeSymbol InputType, ITypeSymbol OutputType);
}
