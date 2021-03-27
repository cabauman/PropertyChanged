﻿// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.PropertyChanged.SourceGenerator.Tests
{
    internal class EmptyClassBuilder : BaseUserSourceBuilder<EmptyClassBuilder>
    {
        protected override string CreateClass(string nestedClasses)
        {
            return $@"
    {ClassAccess.ToFriendlyString()} partial class {ClassName}
    {{
        {nestedClasses}
    }}
";
        }
    }
}
