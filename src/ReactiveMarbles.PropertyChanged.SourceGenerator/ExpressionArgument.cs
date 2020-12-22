﻿// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record ExpressionArgument(LambdaExpressionSyntax LambdaExpression, List<string> ExpressionChain, ITypeSymbol InputType, ITypeSymbol OutputType);
}
