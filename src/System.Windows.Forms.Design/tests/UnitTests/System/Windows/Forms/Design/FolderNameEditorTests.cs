// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.TestUtilities;
using Moq;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace System.Windows.Forms.Design.Tests;

public class FolderNameEditorTests
{
    [Fact]
    public void FolderNameEditor_Ctor_Default()
    {
        FolderNameEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void FolderNameEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        FolderNameEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.Modal, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void FolderNameEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        FolderNameEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void FolderNameEditor_InitializeDialog_Invoke_Nop()
    {
        SubFolderNameEditor editor = new();
        editor.InitializeDialog();
    }

    [WinFormsFact]
    public void FolderNameEditor_EditValue_Invoke_ReturnsValue()
    {
        using SubFolderNameEditor editor = new();
        using EditorDialogTestHost host = new();
        object value = "initial";
        ITypeDescriptorContext context = new Mock<ITypeDescriptorContext>(MockBehavior.Strict).Object;
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);

        object result = editor.EditValue(context, mockServiceProvider.Object, value);

        Assert.Same(value, result);
        Assert.Equal(1, editor.InitializeDialogCount);
        Assert.Equal(1, host.DialogCount);
    }

    [WinFormsFact]
    public void FolderNameEditor_EditValue_Invoke_ReturnsDirectoryPath()
    {
        using SubFolderNameEditor editor = new();
        object value = "initial";
        string path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        using EditorDialogTestHost host = new(
            accept: true,
            onDialogIdle: dialog => PInvokeCore.SendMessage(
                dialog, PInvoke.BFFM_SETSELECTIONW, (WPARAM)(BOOL)true, path));
        ITypeDescriptorContext context = new Mock<ITypeDescriptorContext>(MockBehavior.Strict).Object;
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);

        object result = editor.EditValue(context, mockServiceProvider.Object, value);

        Assert.Equal(path, result);
        Assert.Equal(1, host.DialogCount);
    }

    [WinFormsFact]
    public void FolderNameEditor_EditValue_CalledTwice_ReusesFolderBrowser()
    {
        using SubFolderNameEditor editor = new();
        using EditorDialogTestHost host = new();
        object value = "initial";
        ITypeDescriptorContext context = new Mock<ITypeDescriptorContext>(MockBehavior.Strict).Object;
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);

        object firstResult = editor.EditValue(context, mockServiceProvider.Object, value);
        object secondResult = editor.EditValue(context, mockServiceProvider.Object, value);

        Assert.Same(value, firstResult);
        Assert.Same(value, secondResult);
        Assert.Equal(1, editor.InitializeDialogCount);
        Assert.Equal(2, host.DialogCount);
    }

    public class FolderBrowserTests : FolderNameEditor
    {
        [Fact]
        public void InitialDirectoryEditor_Ctor_Default()
        {
            SubInitialDirectoryEditor editor = new();
            Assert.NotNull(editor);
        }

        [Fact]
        public void InitialDirectoryEditor_InitializeDialog_SetsDescription()
        {
            SubInitialDirectoryEditor editor = new();
            FolderBrowser browser = new();

            editor.InitializeDialog(browser);

            Assert.Equal(SR.InitialDirectoryEditorLabel, browser.Description);
        }

        [Fact]
        public void SelectedPathEditor_Ctor_Default()
        {
            SubSelectedPathEditor editor = new();
            Assert.NotNull(editor);
        }

        [Fact]
        public void SelectedPathEditor_InitializeDialog_SetsDescription()
        {
            SubSelectedPathEditor editor = new();
            FolderBrowser browser = new();

            editor.InitializeDialog(browser);

            Assert.Equal(SR.SelectedPathEditorLabel, browser.Description);
        }

        [Fact]
        public void FolderBrowser_Ctor_Default()
        {
            FolderBrowser browser = new();
            Assert.Empty(browser.DirectoryPath);
            Assert.Empty(browser.Description);
            Assert.Equal(FolderBrowserStyles.RestrictToFilesystem, browser.Style);
            Assert.Equal(FolderBrowserFolder.Desktop, browser.StartLocation);
        }

        [Theory]
        [NormalizedStringData]
        public void FolderBrowser_Description_Set_GetReturnsExpected(string value, string expected)
        {
            FolderBrowser browser = new()
            {
                Description = value
            };
            Assert.Equal(expected, browser.Description);

            // Set same.
            browser.Description = value;
            Assert.Equal(expected, browser.Description);
        }

        [Fact]
        public void FolderBrowser_Description_SetNull_ReturnsEmpty()
        {
            FolderBrowser browser = new()
            {
                Description = "not-empty"
            };

            browser.Description = null;

            Assert.Empty(browser.Description);
        }

        [Fact]
        public void FolderBrowser_Description_SetEmpty_RemainsEmpty()
        {
            FolderBrowser browser = new()
            {
                Description = string.Empty
            };

            Assert.Empty(browser.Description);
        }

        [Theory]
        [MemberData(nameof(StartLocationData))]
        public void FolderBrowser_StartLocation_Set_GetReturnsExpected(int value)
        {
            FolderBrowserFolder folderBrowserFolder = (FolderBrowserFolder)value;
            FolderBrowser browser = new()
            {
                StartLocation = folderBrowserFolder
            };
            Assert.Equal(folderBrowserFolder, browser.StartLocation);

            // Set same.
            browser.StartLocation = folderBrowserFolder;
            Assert.Equal(folderBrowserFolder, browser.StartLocation);
        }

        [Theory]
        [MemberData(nameof(StyleData))]
        public void FolderBrowser_Style_Set_GetReturnsExpected(int value)
        {
            FolderBrowserStyles folderBrowserStyles = (FolderBrowserStyles)value;
            FolderBrowser browser = new()
            {
                Style = folderBrowserStyles
            };
            Assert.Equal(folderBrowserStyles, browser.Style);

            // Set same.
            browser.Style = folderBrowserStyles;
            Assert.Equal(folderBrowserStyles, browser.Style);
        }

        public static IEnumerable<object[]> StartLocationData()
            => Enum.GetValues<FolderBrowserFolder>()
                .Select(value => new object[] { (int)value })
                .Concat([[-1], [int.MaxValue]]);

        public static IEnumerable<object[]> StyleData()
            => Enum.GetValues<FolderBrowserStyles>()
                .Select(value => new object[] { (int)value })
                .Concat([[0], [-1], [int.MaxValue]]);

        [WinFormsFact]
        public void FolderBrowser_ShowDialog_WithOwner_ReturnsCancel()
        {
            FolderBrowser browser = new();
            using DialogHostForm owner = new();

            // The DialogHostForm auto-closes the folder dialog as soon as it becomes idle.
            DialogResult result = browser.ShowDialog(owner);

            Assert.Equal(DialogResult.Cancel, result);
        }

        [WinFormsFact]
        public void FolderBrowser_ShowDialog_WithDescription_ReturnsCancel()
        {
            FolderBrowser browser = new()
            {
                Description = "Pick a folder",
                StartLocation = FolderBrowserFolder.Desktop,
                Style = FolderBrowserStyles.ShowTextBox
            };
            using DialogHostForm owner = new();

            DialogResult result = browser.ShowDialog(owner);

            Assert.Equal(DialogResult.Cancel, result);
        }

        [WinFormsTheory]
        [InlineData(FolderBrowserFolder.Desktop)]
        [InlineData(FolderBrowserFolder.Favorites)]
        [InlineData(FolderBrowserFolder.MyComputer)]
        [InlineData(FolderBrowserFolder.MyDocuments)]
        [InlineData(FolderBrowserFolder.MyPictures)]
        [InlineData(FolderBrowserFolder.NetAndDialUpConnections)]
        [InlineData(FolderBrowserFolder.NetworkNeighborhood)]
        [InlineData(FolderBrowserFolder.Printers)]
        [InlineData(FolderBrowserFolder.Recent)]
        [InlineData(FolderBrowserFolder.SendTo)]
        [InlineData(FolderBrowserFolder.StartMenu)]
        [InlineData(FolderBrowserFolder.Templates)]
        public void FolderBrowser_ShowDialog_StartLocation_ReturnsCancel(int value)
        {
            FolderBrowserFolder folderBrowserFolder = (FolderBrowserFolder)value;
            FolderBrowser browser = new()
            {
                StartLocation = folderBrowserFolder
            };
            using DialogHostForm owner = new();

            DialogResult result = browser.ShowDialog(owner);

            Assert.Equal(DialogResult.Cancel, result);
        }

        [WinFormsTheory]
        [InlineData(FolderBrowserStyles.BrowseForComputer)]
        [InlineData(FolderBrowserStyles.BrowseForEverything)]
        [InlineData(FolderBrowserStyles.BrowseForPrinter)]
        [InlineData(FolderBrowserStyles.RestrictToDomain)]
        [InlineData(FolderBrowserStyles.RestrictToFilesystem)]
        [InlineData(FolderBrowserStyles.RestrictToSubfolders)]
        [InlineData(FolderBrowserStyles.ShowTextBox)]
        public void FolderBrowser_ShowDialog_Style_ReturnsCancel(int value)
        {
            FolderBrowserStyles folderBrowserStyles = (FolderBrowserStyles)value;
            FolderBrowser browser = new()
            {
                Style = folderBrowserStyles
            };
            using DialogHostForm owner = new();

            DialogResult result = browser.ShowDialog(owner);

            Assert.Equal(DialogResult.Cancel, result);
        }

        // Derived wrappers expose the protected/internal InitializeDialog methods of the
        // internal editor types to the test code. They live inside FolderBrowserTests
        // (which derives from FolderNameEditor) so they have access to the protected
        // FolderBrowser type and can call the protected InitializeDialog method.
        private class SubInitialDirectoryEditor : InitialDirectoryEditor
        {
            public new void InitializeDialog(FolderBrowser folderBrowser) => base.InitializeDialog(folderBrowser);
        }

        private class SubSelectedPathEditor : SelectedPathEditor
        {
            public new void InitializeDialog(FolderBrowser folderBrowser) => base.InitializeDialog(folderBrowser);
        }
    }

    /// <summary>
    ///  Observes production browser initialization and owns the resulting component.
    /// </summary>
    private class SubFolderNameEditor : FolderNameEditor, IDisposable
    {
        private FolderBrowser _folderBrowser;
        public int InitializeDialogCount { get; private set; }

        public void InitializeDialog() => base.InitializeDialog(null);

        protected override void InitializeDialog(FolderBrowser folderBrowser)
        {
            InitializeDialogCount++;
            _folderBrowser = folderBrowser;
            base.InitializeDialog(folderBrowser);
        }

        public void Dispose() => _folderBrowser?.Dispose();
    }
}
