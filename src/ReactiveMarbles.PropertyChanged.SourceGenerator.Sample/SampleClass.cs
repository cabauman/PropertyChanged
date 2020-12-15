// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator.Sample
{
    /// <summary>
    /// Dummy.
    /// </summary>
    public class SampleClass : INotifyPropertyChanged
    {
        private SampleClass _myClass;
        private string _myString;

        internal SampleClass()
        {
            var myClass = new SampleClass();
            Expression<Func<SampleClass, string>> expression = x => x.MyClass.MyClass.MyClass.MyString;

            // var stream = NotifyPropertyChangedExtensions.WhenChanged(MyClass, x => x.MyClass.MyString);
            // this.WhenChanged(expression);
            NotifyPropertyChangedExtensions.WhenChanged(this, expression);
            this.WhenChanged(x => x.MyString, x => x.MyString, (a, b) => a + b);
        }

        /// <summary>
        /// Dummy.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a class.
        /// </summary>
        public SampleClass MyClass
        {
            get
            {
                return _myClass;
            }

            set
            {
                _myClass = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyClass)));
            }
        }

        /// <summary>
        /// Gets or sets a string.
        /// </summary>
        public string MyString
        {
            get
            {
                return _myString;
            }

            set
            {
                _myString = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyString)));
            }
        }
    }
}
