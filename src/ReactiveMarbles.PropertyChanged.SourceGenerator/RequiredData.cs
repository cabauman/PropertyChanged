﻿// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator
{
    internal sealed record RequiredData(
        bool AllExpressionArgumentsAreValid,
        List<ExpressionArgument> ExpressionArguments,
        HashSet<MultiExpressionMethodDatum> MultiExpressionMethodData);
}
