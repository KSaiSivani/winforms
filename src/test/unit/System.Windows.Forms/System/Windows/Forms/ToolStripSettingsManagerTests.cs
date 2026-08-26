// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Forms.Tests;

public class ToolStripSettingsManagerTests : IClassFixture<UserConfigDisposableFixture>
{
    [WinFormsFact]
    public void ToolStripSettingsManager_Save_Load_RoundTripExpected()
    {
        using Form mainForm = new();

        using ToolStrip toolStrip = new();
        toolStrip.Name = "Child";
        toolStrip.Size = new Drawing.Size(10, 10);
        toolStrip.Visible = false;
        mainForm.Controls.Add(toolStrip);

        ToolStripSettingsManager toolStripSettingsManager = new(mainForm, "MainForm");

        toolStripSettingsManager.Save();

        toolStrip.Size = new Drawing.Size(5, 5);
        toolStrip.Visible = true;

        toolStripSettingsManager.Load();

        Assert.Equal(new Drawing.Size(10, 10), toolStrip.Size);
        Assert.False(toolStrip.Visible);
    }

    [WinFormsFact]
    public void ToolStripSettingsManager_Save_Load_MultipleToolStripsInPaddedPanel_PreservesLocation()
    {
        // Regression test for https://github.com/dotnet/winforms/issues/4449:
        // when multiple ToolStrips are docked in a ToolStripPanel that has non-zero Padding,
        // their Location must round-trip through Save/Load without drifting by Padding.Left/Top.
        using Form mainForm = new();
        using ToolStripPanel panel = new()
        {
            Dock = DockStyle.Top,
            Name = "toolStripPanel",
            Padding = new Padding(4, 0, 4, 0)
        };
        mainForm.Controls.Add(panel);

        using ToolStrip toolStrip1 = new() { Name = "ToolStripMain", Size = new Drawing.Size(200, 25) };
        using ToolStrip toolStrip2 = new() { Name = "ToolStripFilters", Size = new Drawing.Size(200, 25) };
        mainForm.Controls.Add(toolStrip1);
        mainForm.Controls.Add(toolStrip2);

        Drawing.Point location1 = new(7, 0);
        Drawing.Point location2 = new(7, 25);

        panel.BeginInit();
        panel.Join(toolStrip1, location1);
        panel.Join(toolStrip2, location2);
        panel.EndInit();

        mainForm.PerformLayout();

        ToolStripSettingsManager toolStripSettingsManager = new(mainForm, "MainForm");
        toolStripSettingsManager.Save();

        // Simulate the next application start: reset locations, then reload the saved settings.
        toolStrip1.Location = Drawing.Point.Empty;
        toolStrip2.Location = Drawing.Point.Empty;

        toolStripSettingsManager.Load();
        mainForm.PerformLayout();

        Assert.Equal(location1, toolStrip1.Location);
        Assert.Equal(location2, toolStrip2.Location);
    }
}
