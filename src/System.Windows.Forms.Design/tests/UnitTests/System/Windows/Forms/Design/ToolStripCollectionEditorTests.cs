// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design;
using Moq;

namespace System.Windows.Forms.Design.Tests;

public class ToolStripCollectionEditorTests
{
    [WinFormsFact]
    public void ToolStripCollectionEditor_CreateCollectionForm_ReturnsToolStripItemEditorForm()
    {
        ToolStripCollectionEditor editor = new();
        using Form form = editor.TestAccessor.Dynamic.CreateCollectionForm();

        Assert.Equal("ToolStripItemEditorForm", form.GetType().Name);
    }

    [Fact]
    public void ToolStripCollectionEditor_HelpTopic_ReturnsExpectedValue()
    {
        ToolStripCollectionEditor editor = new();
        Assert.Equal("net.ComponentModel.ToolStripCollectionEditor", (string)editor.TestAccessor.Dynamic.HelpTopic);
    }

    [Fact]
    public void ToolStripCollectionEditor_EditValue_NullProvider_ReturnsNull()
    {
        Assert.Null(new ToolStripCollectionEditor().EditValue(null, null!, new object()));
    }

    [Fact]
    public void ToolStripCollectionEditor_EditValue_WithoutEditorService_ReturnsOriginal()
    {
        object value = new();
        Assert.Same(value, new ToolStripCollectionEditor().EditValue(null, new Mock<IServiceProvider>().Object, value));
    }

    [WinFormsTheory]
    [BoolData]
    public void ToolStripCollectionEditor_EditValue_SetsAndResetsEditingCollection(bool selectDropDownItem)
    {
        using DesignSurface surface = new(typeof(Form));
        var designHost = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var toolStrip = (ToolStrip)designHost.CreateComponent(typeof(ToolStrip));
        using EditorFixture fixture = new(toolStrip);
        using ToolStripMenuItem menuItem = new();
        fixture.ToolStrip.Items.Add(menuItem);
        var designer = (ToolStripDesigner)designHost.GetDesigner(toolStrip)!;
        Mock<IDesignerHost> host = new();
        host.Setup(h => h.GetDesigner(fixture.ToolStrip)).Returns(designer);
        Mock<ISelectionService> selection = new();
        selection.SetupGet(s => s.PrimarySelection)
            .Returns(selectDropDownItem ? menuItem : fixture.ToolStrip);
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        service.Setup(s => s.ShowDialog(It.IsAny<Form>()))
            .Callback<Form>(_ => Assert.True(designer.EditingCollection))
            .Returns(DialogResult.Cancel);
        Mock<IServiceProvider> provider = new();
        provider.Setup(p => p.GetService(typeof(ISelectionService))).Returns(selection.Object);
        provider.Setup(p => p.GetService(typeof(IDesignerHost))).Returns(host.Object);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);

        object? result = fixture.Editor.EditValue(fixture.Context.Object, provider.Object, fixture.ToolStrip.Items);

        Assert.Same(fixture.ToolStrip.Items, result);
        Assert.False(designer.EditingCollection);
        service.VerifyAll();
        host.Verify(h => h.GetDesigner(fixture.ToolStrip), Times.Once);
    }

    [WinFormsFact]
    public void ToolStripCollectionEditor_EditValue_DialogThrows_ResetsEditingCollection()
    {
        using DesignSurface surface = new(typeof(Form));
        var designHost = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var toolStrip = (ToolStrip)designHost.CreateComponent(typeof(ToolStrip));
        using EditorFixture fixture = new(toolStrip);
        var designer = (ToolStripDesigner)designHost.GetDesigner(toolStrip)!;
        Mock<IDesignerHost> host = new();
        host.Setup(h => h.GetDesigner(fixture.ToolStrip)).Returns(designer);
        Mock<ISelectionService> selection = new();
        selection.SetupGet(s => s.PrimarySelection).Returns(fixture.ToolStrip);
        InvalidOperationException expected = new("Dialog failed.");
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        service.Setup(s => s.ShowDialog(It.IsAny<Form>()))
            .Callback<Form>(_ => Assert.True(designer.EditingCollection))
            .Throws(expected);
        Mock<IServiceProvider> provider = new();
        provider.Setup(p => p.GetService(typeof(ISelectionService))).Returns(selection.Object);
        provider.Setup(p => p.GetService(typeof(IDesignerHost))).Returns(host.Object);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(
            () => fixture.Editor.EditValue(fixture.Context.Object, provider.Object, fixture.ToolStrip.Items)));
        Assert.False(designer.EditingCollection);
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_Collection_PopulatesHostAndItems()
    {
        using ToolStripButton first = new();
        using ToolStripButton second = new();
        using EditorFixture fixture = new(first, second);

        Assert.Equal([fixture.ToolStrip, first, second], fixture.ListBox.Items.Cast<object>());
        Assert.Same(fixture.ToolStrip.Items, (object)fixture.Form.TestAccessor.Dynamic._targetToolStripCollection);
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_AddItem_SynchronizesCollectionsAndNotifies()
    {
        using EditorFixture fixture = new();
        using ToolStripButton item = new();

        fixture.Form.TestAccessor.Dynamic.AddItem(item, -1);

        Assert.Equal([item], fixture.ToolStrip.Items.Cast<ToolStripItem>());
        Assert.Equal([fixture.ToolStrip, item], fixture.ListBox.Items.Cast<object>());
        fixture.Context.Verify(c => c.OnComponentChanging(), Times.Once);
        fixture.Context.Verify(c => c.OnComponentChanged(), Times.Once);
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_InsertItem_SynchronizesCollections()
    {
        using ToolStripButton first = new();
        using ToolStripButton second = new();
        using EditorFixture fixture = new(first);

        fixture.Form.TestAccessor.Dynamic.AddItem(second, 1);

        Assert.Equal([second, first], fixture.ToolStrip.Items.Cast<ToolStripItem>());
        Assert.Equal([fixture.ToolStrip, second, first], fixture.ListBox.Items.Cast<object>());
    }

    [WinFormsTheory]
    [InlineData(-2)]
    [InlineData(2)]
    public void ToolStripItemEditorForm_AddItem_InvalidIndex_DoesNotChangeCollections(int index)
    {
        using ToolStripButton existing = new();
        using ToolStripButton item = new();
        using EditorFixture fixture = new(existing);

        fixture.Form.TestAccessor.Dynamic.AddItem(item, index);

        Assert.Equal([existing], fixture.ToolStrip.Items.Cast<ToolStripItem>());
        Assert.Equal([fixture.ToolStrip, existing], fixture.ListBox.Items.Cast<object>());
        fixture.Context.Verify(c => c.OnComponentChanging(), Times.Never);
    }

    [WinFormsTheory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void ToolStripItemEditorForm_MoveItem_SynchronizesCollectionsAndNotifies(int fromIndex, int toIndex)
    {
        using ToolStripButton first = new();
        using ToolStripButton second = new();
        using EditorFixture fixture = new(first, second);

        fixture.Form.TestAccessor.Dynamic.MoveItem(fromIndex, toIndex);

        Assert.Equal([second, first], fixture.ToolStrip.Items.Cast<ToolStripItem>());
        Assert.Equal([fixture.ToolStrip, second, first], fixture.ListBox.Items.Cast<object>());
        fixture.Context.Verify(c => c.OnComponentChanging(), Times.Once);
        fixture.Context.Verify(c => c.OnComponentChanged(), Times.Once);
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_RemoveItem_SynchronizesCollectionsAndNotifies()
    {
        using ToolStripButton first = new();
        using ToolStripButton second = new();
        using EditorFixture fixture = new(first, second);
        object collection = fixture.Form.TestAccessor.Dynamic._itemList;

        collection.GetType().GetMethod("Remove")!.Invoke(collection, [first]);

        Assert.Equal([second], fixture.ToolStrip.Items.Cast<ToolStripItem>());
        Assert.Equal([fixture.ToolStrip, second], fixture.ListBox.Items.Cast<object>());
        fixture.Context.Verify(c => c.OnComponentChanging(), Times.Once);
        fixture.Context.Verify(c => c.OnComponentChanged(), Times.Once);
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_Collection_ReplacingAndClearing_UnsubscribesChangeService()
    {
        using EditorFixture fixture = new();
        using ToolStrip replacement = new();

        fixture.Form.TestAccessor.Dynamic.Collection = replacement.Items;
        fixture.Form.TestAccessor.Dynamic.Collection = null;

        fixture.Changes.VerifyAdd(s => s.ComponentChanged += It.IsAny<ComponentChangedEventHandler>(), Times.Exactly(2));
        fixture.Changes.VerifyRemove(s => s.ComponentChanged -= It.IsAny<ComponentChangedEventHandler>(), Times.Exactly(2));
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_OnComponentChanged_NameProperty_InvalidatesLabel()
    {
        using ToolStripButton item = new();
        using EditorFixture fixture = new(item);
        Label label = fixture.Form.TestAccessor.Dynamic._lblItems;
        _ = label.Handle;
        int invalidations = 0;
        label.Invalidated += (_, _) => invalidations++;
        PropertyDescriptor name = TypeDescriptor.GetProperties(item)[nameof(item.Name)]!;

        fixture.Changes.Raise(s => s.ComponentChanged += null,
            new ComponentChangedEventArgs(item, name, "old", "new"));

        Assert.Equal(1, invalidations);
    }

    [WinFormsTheory]
    [InlineData(0, false, false, false)]
    [InlineData(1, false, true, true)]
    [InlineData(2, true, false, true)]
    public void ToolStripItemEditorForm_Selection_UpdatesCommandsAndPropertyGrid(
        int index, bool moveUp, bool moveDown, bool remove)
    {
        using ToolStripButton first = new();
        using ToolStripButton second = new();
        using EditorFixture fixture = new(first, second);

        fixture.ListBox.SelectedIndex = index;

        Assert.Equal(moveUp, ((Button)fixture.Form.TestAccessor.Dynamic._btnMoveUp).Enabled);
        Assert.Equal(moveDown, ((Button)fixture.Form.TestAccessor.Dynamic._btnMoveDown).Enabled);
        Assert.Equal(remove, ((Button)fixture.Form.TestAccessor.Dynamic._btnRemove).Enabled);
        PropertyGrid grid = fixture.Form.TestAccessor.Dynamic._selectedItemProps;
        if (index == 0)
        {
            Assert.Empty(grid.SelectedObjects);
        }
        else
        {
            Assert.Same(fixture.ListBox.SelectedItem, Assert.Single(grid.SelectedObjects));
        }
    }

    [WinFormsFact]
    public void ToolStripItemEditorForm_OKButtonClick_SetsDialogResult()
    {
        using EditorFixture fixture = new();
        Button button = fixture.Form.TestAccessor.Dynamic._btnOK;

        button.TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        Assert.Equal(DialogResult.OK, fixture.Form.DialogResult);
    }

    [WinFormsTheory]
    [BoolData]
    public void ToolStripItemEditorForm_MeasureItem_SetsExactHeight(bool fromComboBox)
    {
        using EditorFixture fixture = new();
        Control control = fromComboBox
            ? (ComboBox)fixture.Form.TestAccessor.Dynamic._newItemTypes
            : fixture.ListBox;
        MeasureItemEventArgs args = new(null!, 0);
        int iconHeight = fixture.Form.TestAccessor.Dynamic.s_iconHeight;
        int padding = fixture.Form.TestAccessor.Dynamic.s_imagePaddingY;

        fixture.Form.TestAccessor.Dynamic.OnListBoxItems_MeasureItem(control, args);

        Assert.Equal(Math.Max(iconHeight, fixture.Form.Font.Height) + 2 * padding, args.ItemHeight);
    }

    /// <summary>
    ///  Hosts the editor with a real collection and observable designer notifications.
    /// </summary>
    private sealed class EditorFixture : IDisposable
    {
        public ToolStrip ToolStrip { get; }
        public ToolStripCollectionEditor Editor { get; } = new();
        public Mock<ITypeDescriptorContext> Context { get; } = new();
        public Mock<IComponentChangeService> Changes { get; } = new();
        public Form Form { get; }
        public ListBox ListBox { get; }

        public EditorFixture(params ToolStripItem[] items)
            : this(new ToolStrip(), items)
        {
        }

        public EditorFixture(ToolStrip toolStrip, params ToolStripItem[] items)
        {
            ToolStrip = toolStrip;
            ToolStrip.Items.AddRange(items);
            Context.SetupGet(c => c.Instance).Returns(ToolStrip);
            Context.SetupGet(c => c.PropertyDescriptor).Returns(TypeDescriptor.GetProperties(ToolStrip)["Items"]);
            Context.Setup(c => c.GetService(typeof(IComponentChangeService))).Returns(Changes.Object);
            Context.Setup(c => c.OnComponentChanging()).Returns(true);
            new TestAccessor<CollectionEditor>(Editor).Dynamic.Context = Context.Object;
            Form = Editor.TestAccessor.Dynamic.CreateCollectionForm();
            Form.GetType().GetProperty("EditValue")!.SetValue(Form, ToolStrip.Items);
            ListBox = Form.TestAccessor.Dynamic._listBoxItems;
        }

        public void Dispose()
        {
            Form.GetType().GetProperty("EditValue")!.SetValue(Form, null);
            Form.Dispose();
            ToolStrip.Dispose();
        }
    }
}
