// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Windows.Forms.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace System.Windows.Forms.Analyzers.Tests;

/// <summary>
///  Verifies MessageBox.Show overload selection diagnostics in C# and Visual Basic.
/// </summary>
public class SpecifyMessageBoxOptionsAnalyzerTests
{
    private static readonly ReferenceAssemblies s_netReferences = new(
        "net9.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "9.0.0"),
        Path.Combine("ref", "net9.0"));
    private static readonly ReferenceAssemblies s_windowsReferences = s_netReferences.AddPackages(
        [new PackageIdentity("Microsoft.WindowsDesktop.App.Ref", "9.0.0")]);

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public async Task CSharp_OverloadWithoutOptions_ReportsDiagnostic(int argumentCount, bool hasOwner)
    {
        string[] arguments =
        [
            "\"text\"", "\"caption\"", "MessageBoxButtons.OK", "MessageBoxIcon.Information",
            "MessageBoxDefaultButton.Button1"
        ];
        string argumentList = string.Join(", ", arguments.Take(argumentCount));
        if (hasOwner)
        {
            argumentList = "(IWin32Window)null, " + argumentList;
        }

        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.cs",
                $$"""
                using System.Windows.Forms;
                class Example
                {
                    void Display()
                    {
                        {|WFO2002:MessageBox.Show({{argumentList}})|};
                    }
                }
                """));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public async Task VisualBasic_OverloadWithoutOptions_ReportsDiagnostic(int argumentCount, bool hasOwner)
    {
        string[] arguments =
        [
            "\"text\"", "\"caption\"", "MessageBoxButtons.OK", "MessageBoxIcon.Information",
            "MessageBoxDefaultButton.Button1"
        ];
        string argumentList = string.Join(", ", arguments.Take(argumentCount));
        if (hasOwner)
        {
            argumentList = "DirectCast(Nothing, IWin32Window), " + argumentList;
        }

        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.vb",
                $$"""
                Imports System.Windows.Forms
                Class Example
                    Sub Display()
                        {|WFO2002:MessageBox.Show({{argumentList}})|}
                    End Sub
                End Class
                """));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(", true")]
    [InlineData(", \"help.chm\"")]
    [InlineData(", \"help.chm\", \"keyword\"")]
    [InlineData(", \"help.chm\", HelpNavigator.Topic")]
    [InlineData(", \"help.chm\", HelpNavigator.Topic, \"topic\"")]
    public async Task CSharp_OverloadWithOptions_NoDiagnostic(string helpArguments)
    {
        string arguments = "\"text\", \"caption\", MessageBoxButtons.OK, MessageBoxIcon.Information, "
            + "MessageBoxDefaultButton.Button1, options" + helpArguments;
        string ownerCall = helpArguments == ", true"
            ? ""
            : $"MessageBox.Show((IWin32Window)null, {arguments});";
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.cs",
                $$"""
                using System.Windows.Forms;
                class Example
                {
                    void Display(MessageBoxOptions options)
                    {
                        MessageBox.Show({{arguments}});
                        {{ownerCall}}
                    }
                }
                """));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(", True")]
    [InlineData(", \"help.chm\"")]
    [InlineData(", \"help.chm\", \"keyword\"")]
    [InlineData(", \"help.chm\", HelpNavigator.Topic")]
    [InlineData(", \"help.chm\", HelpNavigator.Topic, \"topic\"")]
    public async Task VisualBasic_OverloadWithOptions_NoDiagnostic(string helpArguments)
    {
        string arguments = "\"text\", \"caption\", MessageBoxButtons.OK, MessageBoxIcon.Information, "
            + "MessageBoxDefaultButton.Button1, options" + helpArguments;
        string ownerCall = helpArguments == ", True"
            ? ""
            : $"MessageBox.Show(DirectCast(Nothing, IWin32Window), {arguments})";
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.vb",
                $$"""
                Imports System.Windows.Forms
                Class Example
                    Sub Display(options As MessageBoxOptions)
                        MessageBox.Show({{arguments}})
                        {{ownerCall}}
                    End Sub
                End Class
                """));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading")]
    [InlineData("MessageBoxOptions.ServiceNotification")]
    public async Task CSharp_ExplicitOptionsWithNamedArguments_NoDiagnostic(string options)
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.cs",
                $$"""
                using System.Windows.Forms;
                class Example
                {
                    void Display()
                    {
                        MessageBox.Show(options: {{options}}, text: "text", caption: "caption",
                            buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information,
                            defaultButton: MessageBoxDefaultButton.Button1);
                    }
                }
                """));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("CType(0, MessageBoxOptions)")]
    [InlineData("MessageBoxOptions.RightAlign Or MessageBoxOptions.RtlReading")]
    [InlineData("MessageBoxOptions.ServiceNotification")]
    public async Task VisualBasic_ExplicitOptionsWithNamedArguments_NoDiagnostic(string options)
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.vb",
                $$"""
                Imports System.Windows.Forms
                Class Example
                    Sub Display()
                        MessageBox.Show(options:={{options}}, text:="text", caption:="caption",
                            buttons:=MessageBoxButtons.OK, icon:=MessageBoxIcon.Information,
                            defaultButton:=MessageBoxDefaultButton.Button1)
                    End Sub
                End Class
                """));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CSharp_AliasStaticImportAndNamedArguments_ReportsDiagnostic()
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.cs",
                """
                using Box = System.Windows.Forms.MessageBox;
                using static System.Windows.Forms.MessageBox;
                class Example
                {
                    void Display()
                    {
                        {|WFO2002:Box.Show("text")|};
                        {|WFO2002:Show("text")|};
                        {|WFO2002:global::System.Windows.Forms.MessageBox.Show(caption: "caption", text: "text")|};
                    }
                }
                """));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task VisualBasic_AliasImportAndNamedArguments_ReportsDiagnostic()
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.vb",
                """
                Imports Box = System.Windows.Forms.MessageBox
                Imports System.Windows.Forms.MessageBox
                Class Example
                    Sub Display()
                        {|WFO2002:Box.show("text")|}
                        {|WFO2002:Show("text")|}
                        {|WFO2002:Global.System.Windows.Forms.MessageBox.Show(caption:="caption", text:="text")|}
                    End Sub
                End Class
                """));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CSharp_UnrelatedType_NoDiagnostic(bool hasWinFormsReference)
    {
        AnalyzerTestCase testCase = new(
            hasWinFormsReference ? s_windowsReferences : s_netReferences,
            new AnalyzerTestSource("Example.cs",
                """
                class MessageBox
                {
                    public static void Show(string text) { }
                    void Display() { MessageBox.Show("text"); }
                }
                """));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VisualBasic_UnrelatedType_NoDiagnostic(bool hasWinFormsReference)
    {
        AnalyzerTestCase testCase = new(
            hasWinFormsReference ? s_windowsReferences : s_netReferences,
            new AnalyzerTestSource("Example.vb",
                """
                Class MessageBox
                    Public Shared Sub Show(text As String)
                    End Sub
                    Sub Display()
                        MessageBox.Show("text")
                    End Sub
                End Class
                """));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Example.Designer.cs", "")]
    [InlineData("Example.cs", "// <auto-generated/>\r\n")]
    public async Task CSharp_GeneratedCode_NoDiagnostic(string path, string header)
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource(path,
                header + "class Example { void Display() { System.Windows.Forms.MessageBox.Show(\"text\"); } }"));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Example.Designer.vb", "")]
    [InlineData("Example.vb", "' <auto-generated/>\r\n")]
    public async Task VisualBasic_GeneratedCode_NoDiagnostic(string path, string header)
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource(path, header +
                """
                Class Example
                    Sub Display()
                        System.Windows.Forms.MessageBox.Show("text")
                    End Sub
                End Class
                """));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CSharp_EditorConfigSuppression_NoDiagnostic()
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.cs",
                """class Example { void Display() { System.Windows.Forms.MessageBox.Show("text"); } }"""));
        testCase.AnalyzerConfigFiles.Add(("/.editorconfig",
            "root = true\r\n[*.cs]\r\ndotnet_diagnostic.WFO2002.severity = none"));

        await AnalyzerTestFactory.CreateCSharpAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task VisualBasic_EditorConfigSuppression_NoDiagnostic()
    {
        AnalyzerTestCase testCase = new(
            s_windowsReferences,
            new AnalyzerTestSource("Example.vb",
                """
                Class Example
                    Sub Display()
                        System.Windows.Forms.MessageBox.Show("text")
                    End Sub
                End Class
                """));
        testCase.AnalyzerConfigFiles.Add(("/.editorconfig",
            "root = true\r\n[*.vb]\r\ndotnet_diagnostic.WFO2002.severity = none"));

        await AnalyzerTestFactory.CreateVisualBasicAnalyzerTest<SpecifyMessageBoxOptionsAnalyzer>(testCase)
            .RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Diagnostic_ContainsLocalizedGuidance()
    {
        DiagnosticDescriptor descriptor = SharedDiagnosticDescriptors.s_specifyMessageBoxOptions;

        Assert.Equal("WFO2002", descriptor.Id);
        Assert.True(descriptor.IsEnabledByDefault);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.Equal("https://aka.ms/winforms-warnings/wfo2002", descriptor.HelpLinkUri);
        Assert.Equal(
            "Use a MessageBox.Show overload that takes MessageBoxOptions; specify RightAlign and RtlReading when a right-to-left reading order is required",
            Diagnostic.Create(descriptor, Location.None).GetMessage(CultureInfo.InvariantCulture));
    }
}
