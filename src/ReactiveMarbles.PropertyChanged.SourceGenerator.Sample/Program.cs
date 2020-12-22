// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var myClass = new SampleClass();
            Expression<Func<SampleClass, string>> expression = GetExpression(); // x => x.MyString;

            // myClass.WhenChanged(x => x.MyString, x => x.MyClass, (a, b) => a).Where(x => x != null).Subscribe(Console.WriteLine);
            // myClass.WhenChanged(x => x.MyString);
            myClass.WhenChanged(x => string.Empty).Subscribe(Console.WriteLine);

            // myClass.WhenChanged(x => x.MyClass.MyString, x => x.MyString, (a, b) => a + b);
            // myClass.WhenChanged(x => x.MyString, x => x.MyString, (a, b) => a + b);
            // myClass.WhenChanged(x => x.MyClass, x => x.MyString, (a, b) => a);
            // myClass.WhenChanged(x => x.MyString);
            // NotifyPropertyChangedExtensions.WhenChanged(myClass, x => x.MyString);
            // NotifyPropertyChangedExtensions.WhenChanged(myClass, x => x.MyClass, x => x.MyClass.MyClass, x => x.MyString, (a, b, c) => a);
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Take(5)
                .Subscribe(x => myClass.MyString = x.ToString());

            Console.ReadLine();
        }

        private static Expression<Func<SampleClass, string>> GetExpression()
        {
            return x => x.MyString;
        }
    }
}
