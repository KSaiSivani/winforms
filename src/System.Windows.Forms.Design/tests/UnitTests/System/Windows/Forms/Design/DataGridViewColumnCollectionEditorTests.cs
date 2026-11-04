// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Moq;

namespace System.Windows.Forms.Design.Tests;

public class DataGridViewColumnCollectionEditorTests
{
    [Fact]
    public void DataGridViewColumnCollectionEditor_GetEditStyle() =>
        new DataGridViewColumnCollectionEditor().GetEditStyle().Should().Be(UITypeEditorEditStyle.Modal);

    [Fact]
    public void DataGridViewColumnCollectionEditor_GetEditStyle_WithContext_ReturnsModal()
    {
        Mock<ITypeDescriptorContext> mockTypeDescriptorContext = new(MockBehavior.Loose);

        new DataGridViewColumnCollectionEditor().GetEditStyle(mockTypeDescriptorContext.Object).Should().Be(UITypeEditorEditStyle.Modal);
    }

    [Fact]
    public void DataGridViewColumnCollectionEditor_IsDropDownResizable() =>
        new DataGridViewColumnCollectionEditor().IsDropDownResizable.Should().Be(false);

    [Fact]
    public void DataGridViewColumnCollectionEditor_EditValue()
    {
        DataGridViewColumnCollectionEditor dataGridViewColumnCollectionEditor = new();
        object value = "123";
        dataGridViewColumnCollectionEditor.EditValue(null, null!, value).Should().Be(value);

        Mock<ITypeDescriptorContext> mockTypeDescriptorContext = new(MockBehavior.Strict);
        dataGridViewColumnCollectionEditor.EditValue(mockTypeDescriptorContext.Object, null!, value).Should().Be(value);

        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider.Setup(x => x.GetService(typeof(IWindowsFormsEditorService))).Returns(null!);
        dataGridViewColumnCollectionEditor.EditValue(null, mockServiceProvider.Object, value).Should().Be(value);

        mockTypeDescriptorContext.Setup(x => x.Instance).Returns(null!);
        dataGridViewColumnCollectionEditor.EditValue(null, mockServiceProvider.Object, value).Should().Be(value);
    }

    [Fact]
    public void DataGridViewColumnCollectionEditor_EditValue_WhenDesignerHostServiceMissing_ReturnsValue()
    {
        // IWindowsFormsEditorService is found, context.Instance is a DataGridView, but IDesignerHost is missing.
        // The editor should bail out before opening the dialog and return the original value.
        DataGridViewColumnCollectionEditor editor = new();
        object value = "value-without-host";

        using DataGridView dataGridView = new();

        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Loose);
        mockEditorService
            .Setup(s => s.ShowDialog(It.IsAny<Form>()))
            .Returns(DialogResult.OK);

        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IDesignerHost)))
            .Returns(null!);

        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext.Setup(c => c.Instance).Returns(dataGridView);

        object? result = editor.EditValue(mockContext.Object, mockServiceProvider.Object, value);

        result.Should().Be(value);
        mockEditorService.Verify(
            s => s.ShowDialog(It.IsAny<Form>()),
            Times.Never);
    }

    [WinFormsFact]
    public void DataGridViewColumnCollectionEditor_EditValue_WhenUserCancelsDialog_CancelsTransaction()
    {
        // Full success path: services are present, dialog is shown, user clicks Cancel.
        // The editor should call transaction.Cancel() and return the original value.
        DataGridViewColumnCollectionEditor editor = new();
        object value = "value-cancelled";

        using DataGridView dataGridView = new();

        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(s => s.ShowDialog(It.IsAny<Form>()))
            .Returns(DialogResult.Cancel);

        Mock<DesignerTransaction> mockTransaction = new(MockBehavior.Loose);

        Mock<IDesignerHost> mockDesignerHost = new(MockBehavior.Strict);
        mockDesignerHost
            .Setup(h => h.CreateTransaction(It.IsAny<string>()))
            .Returns(mockTransaction.Object);

        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IDesignerHost)))
            .Returns(mockDesignerHost.Object);

        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext.Setup(c => c.Instance).Returns(dataGridView);

        object? result = editor.EditValue(mockContext.Object, mockServiceProvider.Object, value);
        using Form dialog = (Form)editor.TestAccessor.Dynamic._dataGridViewColumnCollectionDialog;

        result.Should().Be(value);
        mockTransaction.Object.Canceled.Should().BeTrue();
        mockTransaction.Object.Committed.Should().BeFalse();
        mockEditorService.Verify(
            s => s.ShowDialog(It.IsAny<Form>()),
            Times.Once);
    }

    [WinFormsFact]
    public void DataGridViewColumnCollectionEditor_EditValue_WhenUserAcceptsDialog_CommitsTransaction()
    {
        // Full success path: services are present, dialog is shown, user clicks OK.
        // The editor should call transaction.Commit() and return the original value.
        DataGridViewColumnCollectionEditor editor = new();
        object value = "value-committed";

        using DataGridView dataGridView = new();

        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(s => s.ShowDialog(It.IsAny<Form>()))
            .Returns(DialogResult.OK);

        Mock<DesignerTransaction> mockTransaction = new(MockBehavior.Loose);

        Mock<IDesignerHost> mockDesignerHost = new(MockBehavior.Strict);
        mockDesignerHost
            .Setup(h => h.CreateTransaction(It.IsAny<string>()))
            .Returns(mockTransaction.Object);

        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IDesignerHost)))
            .Returns(mockDesignerHost.Object);

        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext.Setup(c => c.Instance).Returns(dataGridView);

        object? result = editor.EditValue(mockContext.Object, mockServiceProvider.Object, value);
        using Form dialog = (Form)editor.TestAccessor.Dynamic._dataGridViewColumnCollectionDialog;

        result.Should().Be(value);
        mockTransaction.Object.Committed.Should().BeTrue();
        mockTransaction.Object.Canceled.Should().BeFalse();
        mockEditorService.Verify(
            s => s.ShowDialog(It.IsAny<Form>()),
            Times.Once);
    }

    [Fact]
    public void DataGridViewColumnCollectionEditor_EditValue_WithInstanceAndNoProvider_ReturnsValue()
    {
        // provider is null - the first guard short-circuits before looking at the context.
        DataGridViewColumnCollectionEditor editor = new();
        object value = "value-no-provider";

        using DataGridView dataGridView = new();

        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext.Setup(c => c.Instance).Returns(dataGridView);

        object? result = editor.EditValue(mockContext.Object, null!, value);

        result.Should().Be(value);
    }

    [Fact]
    public void DataGridViewColumnCollectionEditor_EditValue_WhenContextInstanceIsNull_ReturnsValue()
    {
        // IWindowsFormsEditorService is found, but context.Instance is null.
        // The editor should short-circuit and return the original value.
        DataGridViewColumnCollectionEditor editor = new();
        object value = "value-no-instance";

        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Loose);
        mockEditorService
            .Setup(s => s.ShowDialog(It.IsAny<Form>()))
            .Returns(DialogResult.OK);

        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);

        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext.Setup(c => c.Instance).Returns(null!);

        object? result = editor.EditValue(mockContext.Object, mockServiceProvider.Object, value);

        result.Should().Be(value);
        mockEditorService.Verify(
            s => s.ShowDialog(It.IsAny<Form>()),
            Times.Never);
    }
}
