// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms.Tests;

public class ColumnDropDownClickEventArgsTests
{
    [WinFormsFact]
    public void ColumnDropDownClickEventArgs_Ctor_SetsProperties()
    {
        Point location = new(10, 20);
        ColumnDropDownClickEventArgs e = new(1, location);

        Assert.IsAssignableFrom<ColumnClickEventArgs>(e);
        Assert.Equal(1, e.Column);
        Assert.Equal(location, e.ScreenLocation);
    }

    [WinFormsFact]
    public void ColumnDropDownClickEventArgs_ScreenLocation_Set_GetReturnsExpected()
    {
        ColumnDropDownClickEventArgs e = new(1, Point.Empty)
        {
            ScreenLocation = new Point(30, 40)
        };

        Assert.Equal(new Point(30, 40), e.ScreenLocation);
    }
}
