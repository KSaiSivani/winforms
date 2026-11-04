// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Reflection;
using Moq;

namespace System.Windows.Forms.Design.Tests;

/// <summary>
///  Tests for the private <c>StringCollectionEditor.StringCollectionForm</c> type.
/// </summary>
/// <remarks>
///  <para>The form is a private nested class, so every test reaches in through
///  reflection. The shape of these tests mirrors the way
///  <c>DateTimeEditorTests</c> exercises the private <c>DateTimeUI</c> type:
///  helpers at the bottom of the file resolve the type and its members, and
///  each public surface of the form is exercised at least once.</para>
/// </remarks>
public class StringCollectionFormTests
{
    #region Type and Constructor Tests

    [Fact]
    public void StringCollectionForm_CanBeResolved()
    {
        Type formType = GetStringCollectionFormType();
        formType.Should().NotBeNull();
    }

    [Fact]
    public void StringCollectionForm_IsNestedPrivate()
    {
        Type formType = GetStringCollectionFormType();
        formType.IsNested.Should().BeTrue();
        formType.IsNestedPrivate.Should().BeTrue();
        formType.DeclaringType.Should().Be(typeof(StringCollectionEditor));
    }

    [Fact]
    public void StringCollectionForm_InheritsFromCollectionForm()
    {
        // CollectionForm is the protected abstract base from CollectionEditor.
        Type formType = GetStringCollectionFormType();
        formType.BaseType!.Name.Should().Be("CollectionForm");
    }

    [Fact]
    public void StringCollectionForm_Constructor_TakesCollectionEditor()
    {
        // The single public constructor takes a CollectionEditor.
        Type formType = GetStringCollectionFormType();
        var ctor = formType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(CollectionEditor)],
            null);

        ctor.Should().NotBeNull();
    }

    [Fact]
    public void StringCollectionForm_Constructor_InitializesControls()
    {
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;

        // The InitializeComponent path adds the layout panel to the form.
        realForm.Controls.Count.Should().BeGreaterThan(0);

        // The internal fields must be initialized (i.e. not null) after
        // construction. Use reflection because they are private.
        GetField(form, "_instruction").Should().NotBeNull();
        GetField(form, "_textEntry").Should().NotBeNull();
        GetField(form, "_okButton").Should().NotBeNull();
        GetField(form, "_cancelButton").Should().NotBeNull();
        GetField(form, "_overarchingLayoutPanel").Should().NotBeNull();
        GetField(form, "_editor").Should().NotBeNull();
    }

    [Fact]
    public void StringCollectionForm_FormHasHelpButton()
    {
        // InitializeComponent sets HelpButton = true.
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        realForm.HelpButton.Should().BeTrue();
    }

    [Fact]
    public void StringCollectionForm_FormHasNoMinMaxOrIconInTaskbar()
    {
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        realForm.MaximizeBox.Should().BeFalse();
        realForm.MinimizeBox.Should().BeFalse();
        realForm.ShowInTaskbar.Should().BeFalse();
        realForm.ShowIcon.Should().BeFalse();
    }

    [Fact]
    public void StringCollectionForm_FormIsAutoScaleFont()
    {
        // InitializeComponent sets AutoScaleMode = AutoScaleMode.Font.
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        realForm.AutoScaleMode.Should().Be(AutoScaleMode.Font);
    }

    [Fact]
    public void StringCollectionForm_FormName_IsStringCollectionEditor()
    {
        // InitializeComponent sets the form's Name to "StringCollectionEditor".
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        realForm.Name.Should().Be("StringCollectionEditor");
    }

    #endregion

    #region Edit1_keyDown Tests

    [Theory]
    [InlineData(Keys.A)]
    [InlineData(Keys.Space)]
    [InlineData(Keys.Left)]
    [InlineData(Keys.Right)]
    [InlineData(Keys.F2)]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Tab)]
    public void StringCollectionForm_Edit1_keyDown_NonEscape_DoesNotCancel(Keys key)
    {
        // Escape is the only key that triggers the cancel button. Other keys
        // must leave DialogResult and Handled unchanged.
        using IDisposable form = CreateForm();
        KeyEventArgs args = new(key);

        InvokeInstanceMethod(form, "Edit1_keyDown", [null, args]);

        // The handler returns early without setting e.Handled.
        args.Handled.Should().BeFalse();
    }

    [WinFormsFact]
    public void StringCollectionForm_TextEntry_Escape_CancelsAndHandlesKey()
    {
        using IDisposable form = CreateForm();
        TextBox textEntry = GetField<TextBox>(form, "_textEntry")!;
        Form realForm = (Form)form;
        realForm.Show();
        KeyEventArgs args = new(Keys.Escape);

        textEntry.TestAccessor.Dynamic.OnKeyDown(args);

        Assert.True(args.Handled);
        Assert.Equal(DialogResult.Cancel, realForm.DialogResult);
    }

    #endregion

    #region StringCollectionEditor_HelpButtonClicked Tests

    [Fact]
    public void StringCollectionForm_StringCollectionEditor_HelpButtonClicked_Cancels()
    {
        // The handler always cancels the form's default close and routes to ShowHelp.
        // ShowHelp consults the IHelpService, which is null in this test (no context),
        // so it returns without throwing. We assert Cancel is set and the call is safe.
        using IDisposable form = CreateForm();

        CancelEventArgs args = new();
        InvokeInstanceMethod(form, "StringCollectionEditor_HelpButtonClicked", [null, args]);

        args.Cancel.Should().BeTrue();
    }

    [Fact]
    public void StringCollectionForm_HelpButtonClicked_OnForm_FiresHandler()
    {
        // The handler is wired in HookEvents. Raising the form's HelpButtonClicked
        // event must invoke the handler that the form installed.
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;

        CancelEventArgs args = new();
        bool invoked = false;
        // Form.HelpButtonClicked is typed as CancelEventHandler (not the
        // generic EventHandler<CancelEventArgs>); the C# compiler does not
        // allow implicit conversion between two different delegate types
        // even when their signatures match, so we declare the probe as
        // CancelEventHandler directly.
        CancelEventHandler probe = (_, _) => invoked = true;
        realForm.HelpButtonClicked += probe;

        // Form.OnHelpButtonClicked(CancelEventArgs) raises the event.
        MethodInfo onHelpButtonClicked = typeof(Form)
            .GetMethod("OnHelpButtonClicked", BindingFlags.NonPublic | BindingFlags.Instance)!;
        onHelpButtonClicked.Invoke(realForm, [args]);

        invoked.Should().BeTrue();
        // The form's installed handler also sets Cancel.
        args.Cancel.Should().BeTrue();
    }

    #endregion

    #region Form_HelpRequested Tests

    [Fact]
    public void StringCollectionForm_Form_HelpRequested_DoesNotThrow()
    {
        // The handler is wired in InitializeComponent. F1 help requests are
        // delivered through the HelpRequested event. Without an IHelpService
        // available, ShowHelp is a no-op.
        using IDisposable form = CreateForm();
        HelpEventArgs args = new(new Point(0, 0));

        ((Action)(() => InvokeInstanceMethod(form, "Form_HelpRequested", [null, args])))
            .Should().NotThrow();
    }

    [Fact]
    public void StringCollectionForm_HelpRequested_FiresFormHandler()
    {
        // The Form_HelpRequested handler is wired in InitializeComponent.
        // Invoke the handler through reflection directly; the call must not throw.
        using IDisposable form = CreateForm();

        ((Action)(() => InvokeInstanceMethod(form, "Form_HelpRequested",
            [form, new HelpEventArgs(new Point(0, 0))])))
            .Should().NotThrow();
    }

    #endregion

    #region OnEditValueChanged Tests

    [Fact]
    public void StringCollectionForm_OnEditValueChanged_PopulatesTextEntryFromItems()
    {
        // OnEditValueChanged is invoked by the base CollectionForm.EditValue
        // setter. The override joins the Items array with Environment.NewLine
        // and assigns to _textEntry.Text. We drive the call through the base
        // setter via reflection.
        using IDisposable form = CreateFormWithItems(["one", "two", "three"]);

        TextBox textEntry = GetField<TextBox>(form, "_textEntry")!;
        textEntry.Text.Should().Be($"one{Environment.NewLine}two{Environment.NewLine}three");
    }

    [Fact]
    public void StringCollectionForm_OnEditValueChanged_EmptyItems_ClearsTextEntry()
    {
        using IDisposable form = CreateFormWithItems([]);

        TextBox textEntry = GetField<TextBox>(form, "_textEntry")!;
        textEntry.Text.Should().BeEmpty();
    }

    [Fact]
    public void StringCollectionForm_OnEditValueChanged_SingleItem_SetsTextEntry()
    {
        using IDisposable form = CreateFormWithItems(["only"]);

        TextBox textEntry = GetField<TextBox>(form, "_textEntry")!;
        textEntry.Text.Should().Be("only");
    }

    [Fact]
    public void StringCollectionForm_OnEditValueChanged_NullEditValue_ClearsTextEntry()
    {
        // The base Items getter handles a null EditValue by returning an empty array.
        using IDisposable form = CreateForm();

        // The base EditValue setter is public; setting to null drives OnEditValueChanged.
        PropertyInfo editValueProperty = GetStringCollectionFormType().BaseType!
            .GetProperty("EditValue", BindingFlags.Public | BindingFlags.Instance)!;
        editValueProperty.SetValue(form, null);

        TextBox textEntry = GetField<TextBox>(form, "_textEntry")!;
        textEntry.Text.Should().BeEmpty();
    }

    #endregion

    #region OKButton_click Tests

    [WinFormsTheory]
    [InlineData("x\ny\nz", new string[] { "x", "y", "z" })]
    [InlineData("a\nc", new string[] { "a", "c" })]
    [InlineData("a\r\nc\r\n", new string[] { "a", "c" })]
    [InlineData("", new string[] { })]
    [InlineData("\n", new string[] { "" })]
    public void StringCollectionForm_OKButtonClick_UpdatesCollection(string text, string[] expected)
    {
        StringCollectionEditor editor = CreateEditor();
        Mock<ITypeDescriptorContext> context = new();
        context.Setup(c => c.OnComponentChanging()).Returns(true);
        new TestAccessor<CollectionEditor>(editor).Dynamic.Context = context.Object;
        using Form form = (Form)Activator.CreateInstance(
            GetStringCollectionFormType(),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [editor],
            null)!;
        List<string> items = ["a", "b"];
        form.GetType().GetProperty("EditValue")!.SetValue(form, items);
        SetTextEntryText(form, text);

        GetField<Button>(form, "_okButton")!.TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        Assert.Equal(expected, items);
        context.Verify(c => c.OnComponentChanging(), Times.Once);
        context.Verify(c => c.OnComponentChanged(), Times.Once);
    }

    [WinFormsFact]
    public void StringCollectionForm_OKButtonClick_RejectedChange_PreservesCollection()
    {
        StringCollectionEditor editor = CreateEditor();
        Mock<ITypeDescriptorContext> context = new();
        context.Setup(c => c.OnComponentChanging()).Returns(false);
        new TestAccessor<CollectionEditor>(editor).Dynamic.Context = context.Object;
        using Form form = (Form)Activator.CreateInstance(
            GetStringCollectionFormType(),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [editor],
            null)!;
        List<string> items = ["original"];
        form.GetType().GetProperty("EditValue")!.SetValue(form, items);
        SetTextEntryText(form, "changed");

        InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty]);

        Assert.Equal(["original"], items);
        context.Verify(c => c.OnComponentChanged(), Times.Never);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_SameContent_SetsDialogResultToCancel()
    {
        // If the lines match Items exactly, OKButton_click sets DialogResult
        // to Cancel so the form is dismissed without committing changes.
        using IDisposable form = CreateFormWithItems(["a", "b"]);
        Form realForm = (Form)form;
        SetTextEntryText(form, "a\nb");
        realForm.DialogResult = DialogResult.None;

        InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty]);

        realForm.DialogResult.Should().Be(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_DifferentLineCount_HandlesGracefully()
    {
        // The length-changed branch goes to UpdateItems and does NOT set
        // DialogResult. The Items setter needs a context to commit; without
        // one, Items stays empty. We assert that the length-changed branch
        // is taken (i.e. the call is safe and DialogResult is not Cancel).
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        // Empty initial items, two lines of text -> different length.
        SetTextEntryText(form, "x\ny");
        realForm.DialogResult = DialogResult.None;

        ((Action)(() => InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty])))
            .Should().NotThrow();
        realForm.DialogResult.Should().NotBe(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_DifferentLineContent_HandlesGracefully()
    {
        // Same length, different content -> UpdateItems branch.
        using IDisposable form = CreateFormWithItems(["a", "b"]);
        Form realForm = (Form)form;
        SetTextEntryText(form, "a\nc");
        realForm.DialogResult = DialogResult.None;

        ((Action)(() => InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty])))
            .Should().NotThrow();
        realForm.DialogResult.Should().NotBe(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_ContentDiffersAtLastIndex_HandlesGracefully()
    {
        // Same length, differ at the last index -> UpdateItems branch.
        using IDisposable form = CreateFormWithItems(["a", "b"]);
        Form realForm = (Form)form;
        SetTextEntryText(form, "a\nc");
        realForm.DialogResult = DialogResult.None;

        ((Action)(() => InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty])))
            .Should().NotThrow();
        realForm.DialogResult.Should().NotBe(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_ContentMatches_SetsDialogResultToCancel()
    {
        // Same length, same content -> DialogResult.Cancel.
        using IDisposable form = CreateFormWithItems(["a", "b"]);
        Form realForm = (Form)form;
        SetTextEntryText(form, "a\nb");
        realForm.DialogResult = DialogResult.None;

        InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty]);

        realForm.DialogResult.Should().Be(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_TextWithTrailingNewline_HandlesGracefully()
    {
        // The text "a\nb\n" splits into ["a", "b", ""] which differs in length
        // from ["a", "b"] -> UpdateItems branch.
        using IDisposable form = CreateFormWithItems(["a", "b"]);
        Form realForm = (Form)form;
        SetTextEntryText(form, "a\nb\n");
        realForm.DialogResult = DialogResult.None;

        ((Action)(() => InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty])))
            .Should().NotThrow();
        realForm.DialogResult.Should().NotBe(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_EmptyText_HandlesGracefully()
    {
        // Empty text -> one empty line -> length differs from 0 -> UpdateItems
        // branch (which is safe with no context).
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        SetTextEntryText(form, "");
        realForm.DialogResult = DialogResult.None;

        ((Action)(() => InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty])))
            .Should().NotThrow();
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_SingleLineMatchingOneItem_Cancels()
    {
        using IDisposable form = CreateFormWithItems(["x"]);
        Form realForm = (Form)form;
        SetTextEntryText(form, "x");
        realForm.DialogResult = DialogResult.None;

        InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty]);

        realForm.DialogResult.Should().Be(DialogResult.Cancel);
    }

    [Fact]
    public void StringCollectionForm_OKButton_click_OnlyEmptyLines_HandlesGracefully()
    {
        // Text "\n" splits into ["", ""] which differs from Items=[] -> UpdateItems.
        // UpdateItems sees the last line is empty, trims it, and assigns [""].
        // With no context this is a no-op.
        using IDisposable form = CreateForm();
        Form realForm = (Form)form;
        SetTextEntryText(form, "\n");
        realForm.DialogResult = DialogResult.None;

        ((Action)(() => InvokeInstanceMethod(form, "OKButton_click", [null, EventArgs.Empty])))
            .Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private static StringCollectionEditor CreateEditor()
        => new(typeof(List<string>));

    private static IDisposable CreateForm()
    {
        StringCollectionEditor editor = CreateEditor();
        object form = Activator.CreateInstance(
            GetStringCollectionFormType(),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [editor],
            null)!;
        return (IDisposable)form;
    }

    private static IDisposable CreateFormWithItems(string[] items)
    {
        IDisposable form = CreateForm();
        // Drive the OnEditValueChanged path that the form would normally take
        // when the editor assigns a new EditValue. The base CollectionForm.EditValue
        // setter calls OnEditValueChanged, which the StringCollectionForm overrides
        // to populate _textEntry.Text. SetItems on a string[] returns the same
        // array, so passing a string[] through gives the base Items getter
        // something to enumerate.
        PropertyInfo editValueProperty = GetStringCollectionFormType().BaseType!
            .GetProperty("EditValue", BindingFlags.Public | BindingFlags.Instance)!;
        editValueProperty.SetValue(form, items);

        return form;
    }

    private static void SetTextEntryText(IDisposable form, string text)
    {
        GetField<TextBox>(form, "_textEntry")!.Text = text;
    }

    private static T? GetField<T>(object instance, string name) where T : class
        => (T?)GetField(instance, name);

    private static object? GetField(object instance, string name)
    {
        FieldInfo? field = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull();
        return field!.GetValue(instance);
    }

    private static void InvokeInstanceMethod(object instance, string name, object?[] args)
    {
        MethodInfo? method = instance.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(instance, args);
    }

    private static Type GetStringCollectionFormType()
    {
        Type? formType = typeof(StringCollectionEditor).GetNestedType("StringCollectionForm", BindingFlags.NonPublic);
        formType.Should().NotBeNull();
        return formType!;
    }

    #endregion
}
