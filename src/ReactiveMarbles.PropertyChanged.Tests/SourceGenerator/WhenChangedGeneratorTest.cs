// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ReactiveMarbles.PropertyChanged.SourceGenerator.Tests
{
    public class WhenChangedGeneratorTest
    {
        [Fact]
        public void SimpleGeneratorTest()
        {
            string userSource = @"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Sample
{
    public class Program : INotifyPropertyChanged
    {
        private string _myString;
        private Program _child;

        public static void Main(string[] args)
        {
        }

        public Program()
        {
            Expression<Func<Program, string>> myExpression = x => x.MyString;

            // NotifyPropertyChangedExtensions.WhenChanged(this, GetExpression());
            // NotifyPropertyChangedExtensions.WhenChanged(this, myExpression);

            // NotifyPropertyChangedExtensions.WhenChanged(this, x => x.MyString);
            // this.WhenChanged(x => x.Child, x => x.MyString, (a, b) => a.ToString() + b);
            this.WhenChanged(x => x.Child.Child.MyString);
        }

        public Expression<Func<Program, string>> GetExpression() => x => x.MyString;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MyString
        {
            get => _myString;
            set => RaiseAndSetIfChanged(ref _myString, value);
        }

        public Program Child
        {
            get => _child;
            set => RaiseAndSetIfChanged(ref _child, value);
        }

        protected void RaiseAndSetIfChanged<T>(ref T fieldValue, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(fieldValue, value))
            {
                return;
            }

            fieldValue = value;
            OnPropertyChanged(propertyName);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
";
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());

            Assert.Empty(generatorDiagnostics);
            Assert.Empty(newCompilation.GetDiagnostics());

            var generatedSource = newCompilation.SyntaxTrees.First().ToString();
        }

        private static Compilation CreateCompilation(string source, string assemblyPath) =>
            CSharpCompilation.Create(
                assemblyName: "compilation",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(Observable).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(WhenChangedGenerator).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.Expressions.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.ObjectModel.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
                },
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        private static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators) =>
            CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create(generators),
                additionalTexts: ImmutableArray<AdditionalText>.Empty,
                parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
                optionsProvider: null);

        private static Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators).RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out diagnostics);
            return outputCompilation;
        }
    }
}