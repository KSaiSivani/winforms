// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Forms;

/// <summary>
///  Provides data for the <see cref="ListView.ColumnDropDownClicked"/> event.
/// </summary>
public class ColumnDropDownClickEventArgs : ColumnClickEventArgs
{
    /// <summary>
    ///  Initializes a new instance of the <see cref="ColumnDropDownClickEventArgs"/> class.
    /// </summary>
    /// <param name="column">The zero-based index of the column whose drop-down button was clicked.</param>
    /// <param name="screenLocation">The screen coordinates at which to display the drop-down.</param>
    public ColumnDropDownClickEventArgs(int column, Point screenLocation)
        : base(column)
    {
        ScreenLocation = screenLocation;
    }

    /// <summary>
    ///  Gets or sets the screen coordinates at which to display the drop-down.
    /// </summary>
    public Point ScreenLocation { get; set; }
}
