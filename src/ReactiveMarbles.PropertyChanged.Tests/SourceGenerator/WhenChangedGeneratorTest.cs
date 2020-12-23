// Copyright (c) 2019-2020 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            // Expression<Func<Program, string>> myExpression = x => x.MyString;
            // NotifyPropertyChangedExtensions.WhenChanged(this, GetExpression());
            // NotifyPropertyChangedExtensions.WhenChanged(this, myExpression);
            // NotifyPropertyChangedExtensions.WhenChanged(this, x => x.MyString);
            // this.WhenChanged(x => x.Child, x => x.MyString, (a, b) => a.ToString() + b);
            // this.WhenChanged(x => x.Child, x => x.MyString, (a, b) => a.ToString() + b);
            // this.WhenChanged(x => x.Child.MyString);
            this.WhenChanged(x => x.Child.MyString);
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

            Assert.Empty(generatorDiagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));
            Assert.Empty(newCompilation.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning));

            var generatedSource = newCompilation.SyntaxTrees.Last().ToString();
            var nodes = newCompilation.SyntaxTrees
                .Last()
                .GetRoot()
                .DescendantNodes();
            var methodDeclaration = nodes
                .OfType<MethodDeclarationSyntax>()
                .Single();
            var fieldDeclarations = nodes
                .OfType<FieldDeclarationSyntax>();

            Assert.Equal("WhenChanged", methodDeclaration.Identifier.Text);
            Assert.Single(fieldDeclarations);
        }

        [Fact]
        public void SimpleGeneratorTest0()
        {
            string constructorBody = @"
            this.WhenChanged(x => x.MyString);
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());

            Assert.Empty(generatorDiagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));
            Assert.Empty(newCompilation.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning));
        }

        [Fact]
        public void SimpleGeneratorTest00()
        {
            string constructorBody = @"
            var program = new Program();
            program.WhenChanged(x => x.MyString);
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());

            Assert.Empty(generatorDiagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));
            Assert.Empty(newCompilation.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning));
        }

        [Fact]
        public void SimpleGeneratorTest1()
        {
            string constructorBody = @"
            NotifyPropertyChangedExtensions.WhenChanged(this, x => x.MyString);
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());

            Assert.Empty(generatorDiagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));
            Assert.Empty(newCompilation.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning));
        }

        [Fact]
        public void SimpleGeneratorTest2()
        {
            string constructorBody = @"
            Expression<Func<Program, string>> myExpression = x => x.MyString;
            this.WhenChanged(myExpression);
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());
            var diagnostics = newCompilation.GetDiagnostics();

            Assert.Empty(diagnostics);
            Assert.Single(generatorDiagnostics);
            Assert.Equal(WhenChangedGenerator.InvalidMemberExpressionError, generatorDiagnostics[0].Descriptor);
        }

        [Fact]
        public void SimpleGeneratorTest3()
        {
            string constructorBody = @"
            this.WhenChanged(GetExpression());
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());
            var diagnostics = newCompilation.GetDiagnostics();

            Assert.Empty(diagnostics);
            Assert.Single(generatorDiagnostics);
            Assert.Equal(WhenChangedGenerator.InvalidMemberExpressionError, generatorDiagnostics[0].Descriptor);
        }

        [Fact]
        public void SimpleGeneratorTest21()
        {
            string constructorBody = @"
            Expression<Func<Program, string>> myExpression = x => x.MyString;
            var program = new Program();
            program.WhenChanged(myExpression);
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());
            var diagnostics = newCompilation.GetDiagnostics();

            Assert.Empty(diagnostics);
            Assert.Single(generatorDiagnostics);
            Assert.Equal(WhenChangedGenerator.InvalidMemberExpressionError, generatorDiagnostics[0].Descriptor);
        }

        [Fact]
        public void SimpleGeneratorTest31()
        {
            string constructorBody = @"
            var program = new Program();
            program.WhenChanged(GetExpression());
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());
            var diagnostics = newCompilation.GetDiagnostics();

            Assert.Empty(diagnostics);
            Assert.Single(generatorDiagnostics);
            Assert.Equal(WhenChangedGenerator.InvalidMemberExpressionError, generatorDiagnostics[0].Descriptor);
        }

        [Fact]
        public void SimpleGeneratorTest212()
        {
            string constructorBody = @"
            Expression<Func<Program, string>> myExpression = x => x.MyString;
            NotifyPropertyChangedExtensions.WhenChanged(this, myExpression);
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());
            var diagnostics = newCompilation.GetDiagnostics();

            Assert.Empty(diagnostics);
            Assert.Single(generatorDiagnostics);
            Assert.Equal(WhenChangedGenerator.InvalidMemberExpressionError, generatorDiagnostics[0].Descriptor);
        }

        [Fact]
        public void SimpleGeneratorTest312()
        {
            string constructorBody = @"
            NotifyPropertyChangedExtensions.WhenChanged(this, GetExpression());
";
            string userSource = GetSource(constructorBody);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Compilation compilation = CreateCompilation(userSource, assemblyPath);
            var newCompilation = RunGenerators(compilation, out var generatorDiagnostics, new WhenChangedGenerator());
            var diagnostics = newCompilation.GetDiagnostics();

            Assert.Empty(diagnostics);
            Assert.Single(generatorDiagnostics);
            Assert.Equal(WhenChangedGenerator.InvalidMemberExpressionError, generatorDiagnostics[0].Descriptor);
        }

        private static string GetSource(string constructorBody)
        {
            return $@"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Sample
{{
    public class Program : INotifyPropertyChanged
    {{
        private string _myString;
        private Program _child;

        public static void Main(string[] args)
        {{
        }}

        public Program()
        {{
{constructorBody}
        }}

        public Expression<Func<Program, string>> GetExpression() => x => x.MyString;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MyString
        {{
            get => _myString;
            set => RaiseAndSetIfChanged(ref _myString, value);
        }}

        public Program Child
        {{
            get => _child;
            set => RaiseAndSetIfChanged(ref _child, value);
        }}

        protected void RaiseAndSetIfChanged<T>(ref T fieldValue, T value, [CallerMemberName] string propertyName = null)
        {{
            if (EqualityComparer<T>.Default.Equals(fieldValue, value))
            {{
                return;
            }}

            fieldValue = value;
            OnPropertyChanged(propertyName);
        }}

        protected virtual void OnPropertyChanged(string propertyName)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
    }}
}}
";
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