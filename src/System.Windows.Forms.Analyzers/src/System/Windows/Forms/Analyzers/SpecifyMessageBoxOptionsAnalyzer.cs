// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Windows.Forms.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace System.Windows.Forms.Analyzers;

/// <summary>
///  Reports MessageBox.Show calls that do not specify message box options.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class SpecifyMessageBoxOptionsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [SharedDiagnosticDescriptors.s_specifyMessageBoxOptions];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(startContext =>
        {
            INamedTypeSymbol? messageBoxType = startContext.Compilation.GetTypeByMetadataName("System.Windows.Forms.MessageBox");
            INamedTypeSymbol? optionsType = startContext.Compilation.GetTypeByMetadataName("System.Windows.Forms.MessageBoxOptions");
            if (messageBoxType is null || optionsType is null)
            {
                return;
            }

            startContext.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;
                IMethodSymbol method = invocation.TargetMethod;
                if (method.Name != "Show"
                    || !SymbolEqualityComparer.Default.Equals(method.ContainingType, messageBoxType)
                    || method.Parameters.Any(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, optionsType)))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    SharedDiagnosticDescriptors.s_specifyMessageBoxOptions,
                    invocation.Syntax.GetLocation()));
            }, OperationKind.Invocation);
        });
    }
}
