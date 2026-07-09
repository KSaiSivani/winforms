// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using System.Drawing;

namespace System.Windows.Forms;

/// <summary>
/// Provides data for the <see cref="ListView.ColumnDropDownClicked"/> event.
/// </summary>
public class ColumnDropDownClickEventArgs : EventArgs
{
    public ColumnDropDownClickEventArgs(int column, Point screenLocation)
    {
        Column = column;
        ScreenLocation = screenLocation;
    }

    public int Column { get; }

    public Point ScreenLocation { get; }
}
