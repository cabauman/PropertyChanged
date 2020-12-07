// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var myClass = new SampleClass();

            // myClass.WhenChanged(x => x.MyString, x => x.MyClass, (a, b) => a).Where(x => x != null).Subscribe(Console.WriteLine);
            myClass.WhenChanged(x => x.MyString).Where(x => x != null).Subscribe(Console.WriteLine);

            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Take(5)
                .Subscribe(x => myClass.MyString = x.ToString());

            Console.ReadLine();
        }
    }
}
