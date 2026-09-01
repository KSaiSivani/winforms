// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Windows.Forms.Design;

public partial class FolderNameEditor
{
    protected sealed class FolderBrowser : Component
    {
        // Description text to show.
        private string _descriptionText = string.Empty;

        /// <summary>
        ///  The styles the folder browser will use when browsing
        ///  folders. This should be a combination of flags from
        ///  the FolderBrowserStyles enum.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   The dialog shown by <see cref="ShowDialog(IWin32Window?)"/> is delegated to the modern,
        ///   Vista-style <see cref="FolderBrowserDialog"/>, which always restricts browsing to the file system.
        ///   As a result, only <see cref="FolderBrowserStyles.RestrictToFilesystem"/> (the default) is honored;
        ///   the remaining flags (e.g. <see cref="FolderBrowserStyles.BrowseForComputer"/>,
        ///   <see cref="FolderBrowserStyles.BrowseForPrinter"/>, <see cref="FolderBrowserStyles.BrowseForEverything"/>,
        ///   <see cref="FolderBrowserStyles.RestrictToDomain"/>, <see cref="FolderBrowserStyles.RestrictToSubfolders"/>,
        ///   and <see cref="FolderBrowserStyles.ShowTextBox"/>) have no effect, as the modern folder picker has no
        ///   equivalent concepts.
        ///  </para>
        /// </remarks>
        public FolderBrowserStyles Style { get; set; } = FolderBrowserStyles.RestrictToFilesystem;

        /// <summary>
        ///  Gets the directory path of the folder the user picked.
        /// </summary>
        public string DirectoryPath { get; private set; } = string.Empty;

        /// <summary>
        ///  Gets/sets the start location of the root node.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   This value is passed through to <see cref="FolderBrowserDialog.RootFolder"/>, which is only consulted
        ///   by the legacy Shell folder browser dialog. The modern Vista-style picker used by
        ///   <see cref="ShowDialog(IWin32Window?)"/> does not currently read <see cref="FolderBrowserDialog.RootFolder"/>,
        ///   so setting this property has no observable effect unless the legacy dialog fallback is used
        ///   (for example, when <see cref="FolderBrowserDialog.AutoUpgradeEnabled"/> cannot be honored).
        ///  </para>
        /// </remarks>
        public FolderBrowserFolder StartLocation { get; set; } = FolderBrowserFolder.Desktop;

        /// <summary>
        ///  Gets or sets a description to show above the folders. Here you can provide instructions for
        ///  selecting a folder.
        /// </summary>
        [AllowNull]
        public string Description
        {
            get => _descriptionText;
            set => _descriptionText = value ?? string.Empty;
        }

        /// <summary>
        ///  Shows the folder browser dialog.
        /// </summary>
        public DialogResult ShowDialog() => ShowDialog(null);

        /// <summary>
        ///  Shows the folder browser dialog with the specified owner.
        /// </summary>
        public DialogResult ShowDialog(IWin32Window? owner)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = _descriptionText,
                SelectedPath = DirectoryPath,
                // Some FolderBrowserFolder values (NetAndDialUpConnections, NetworkNeighborhood, Printers) don't
                // correspond to a defined Environment.SpecialFolder member. That's fine: FolderBrowserDialog.RootFolder
                // intentionally accepts undefined values without validation for compatibility with this enum.
                RootFolder = (Environment.SpecialFolder)StartLocation,
                AutoUpgradeEnabled = true
            };

            DialogResult result = owner is not null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                DirectoryPath = dialog.SelectedPath ?? string.Empty;
            }

            return result;
        }
    }
}
