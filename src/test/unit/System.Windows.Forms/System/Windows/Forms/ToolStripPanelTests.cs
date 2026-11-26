// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms.TestUtilities;

namespace System.Windows.Forms.Tests;

public class ToolStripPanelTests
{
    public static IEnumerable<object[]> MinimizeRestore_TestData()
    {
        foreach (DockStyle dock in new[] { DockStyle.Top, DockStyle.Bottom, DockStyle.Left, DockStyle.Right })
        {
            foreach (bool mdiChild in new[] { false, true })
            {
                foreach (RightToLeft rightToLeft in new[] { RightToLeft.No, RightToLeft.Yes })
                {
                    foreach (bool stretch in new[] { false, true })
                    {
                        foreach (FormWindowState restoreState in new[] { FormWindowState.Normal, FormWindowState.Maximized })
                        {
                            yield return [dock, mdiChild, rightToLeft, stretch, restoreState];
                        }
                    }
                }
            }
        }
    }

    [WinFormsTheory]
    [MemberData(nameof(MinimizeRestore_TestData))]
    public void ToolStripPanel_MinimizeRestore_PreservesToolStripBounds(
        DockStyle dock,
        bool mdiChild,
        RightToLeft rightToLeft,
        bool stretch,
        FormWindowState restoreState)
    {
        using Form parent = new()
        {
            IsMdiContainer = mdiChild,
            ClientSize = new Size(1000, 800),
            ShowInTaskbar = false
        };
        using Form child = new()
        {
            ClientSize = new Size(700, 500),
            ShowInTaskbar = false
        };
        Form form = mdiChild ? child : parent;
        if (mdiChild)
        {
            child.MdiParent = parent;
        }

        form.RightToLeft = rightToLeft;
        using ToolStripPanel panel = new() { Dock = dock };
        form.Controls.Add(panel);
        bool horizontal = dock is DockStyle.Top or DockStyle.Bottom;
        using ToolStrip first = new() { Stretch = stretch };
        using ToolStrip second = new() { Stretch = stretch };
        first.Items.Add("First");
        second.Items.Add("Second");
        parent.Show();
        if (mdiChild)
        {
            child.Show();
            Assert.Same(parent, panel.TopLevelControl);
            Assert.Same(child, panel.FindForm());
        }

        form.WindowState = restoreState;
        panel.Join(first, stretch ? Point.Empty : horizontal ? new Point(80, 0) : new Point(0, 80));
        if (!stretch)
        {
            panel.Join(second, horizontal ? new Point(260, 0) : new Point(0, 260));
        }

        panel.PerformLayout();
        Rectangle firstBounds = first.Bounds;
        Rectangle secondBounds = second.Bounds;
        Assert.True(first.Visible);
        Assert.Equal(horizontal ? Orientation.Horizontal : Orientation.Vertical, panel.Orientation);
        if (!stretch)
        {
            Assert.True(second.Visible);
            Assert.True(horizontal ? first.Left > 0 : first.Top > 0);
            Assert.True(horizontal ? second.Left > first.Right : second.Top > first.Bottom);
        }

        for (int cycle = 0; cycle < 3; cycle++)
        {
            form.WindowState = FormWindowState.Minimized;
            Assert.Equal(FormWindowState.Minimized, form.WindowState);
            panel.PerformLayout();
            if (mdiChild)
            {
                Assert.Equal(FormWindowState.Normal, parent.WindowState);
            }

            form.WindowState = restoreState;
            panel.PerformLayout();
            Assert.Equal(restoreState, form.WindowState);
            Assert.Equal(firstBounds, first.Bounds);
            if (!stretch)
            {
                Assert.Equal(secondBounds, second.Bounds);
            }
        }
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top, false, RightToLeft.No)]
    [InlineData(DockStyle.Bottom, false, RightToLeft.No)]
    [InlineData(DockStyle.Left, false, RightToLeft.No)]
    [InlineData(DockStyle.Right, false, RightToLeft.No)]
    [InlineData(DockStyle.Top, true, RightToLeft.No)]
    [InlineData(DockStyle.Bottom, true, RightToLeft.No)]
    [InlineData(DockStyle.Left, true, RightToLeft.No)]
    [InlineData(DockStyle.Right, true, RightToLeft.No)]
    [InlineData(DockStyle.Top, false, RightToLeft.Yes)]
    [InlineData(DockStyle.Bottom, false, RightToLeft.Yes)]
    [InlineData(DockStyle.Left, false, RightToLeft.Yes)]
    [InlineData(DockStyle.Right, false, RightToLeft.Yes)]
    [InlineData(DockStyle.Top, true, RightToLeft.Yes)]
    [InlineData(DockStyle.Bottom, true, RightToLeft.Yes)]
    [InlineData(DockStyle.Left, true, RightToLeft.Yes)]
    [InlineData(DockStyle.Right, true, RightToLeft.Yes)]
    public void ToolStripPanel_Resize_UpdatesToolStripBounds(DockStyle dock, bool stretch, RightToLeft rightToLeft)
    {
        using Form form = new()
        {
            ClientSize = new Size(700, 700),
            ShowInTaskbar = false,
            RightToLeft = rightToLeft
        };
        using ToolStripPanel panel = new() { Dock = dock };
        form.Controls.Add(panel);
        bool horizontal = dock is DockStyle.Top or DockStyle.Bottom;
        using ToolStrip toolStrip = new()
        {
            AutoSize = false,
            Size = new Size(100, 100),
            Stretch = stretch
        };
        panel.Join(toolStrip, horizontal ? new Point(260, 0) : new Point(0, 260));
        form.Show();
        Rectangle originalBounds = toolStrip.Bounds;

        form.ClientSize = new Size(200, 200);
        panel.PerformLayout();
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.NotEqual(originalBounds, toolStrip.Bounds);
        Assert.True(panel.ClientRectangle.Contains(toolStrip.Bounds));
        if (stretch)
        {
            Assert.Equal(
                horizontal ? panel.DisplayRectangle.Width : panel.DisplayRectangle.Height,
                horizontal ? toolStrip.Width : toolStrip.Height);
        }

        form.ClientSize = new Size(700, 700);
        panel.PerformLayout();
        Assert.True(panel.ClientRectangle.Contains(toolStrip.Bounds));
        if (stretch)
        {
            Assert.Equal(originalBounds.Size, toolStrip.Size);
        }
        else
        {
            Assert.Equal(originalBounds, toolStrip.Bounds);
        }
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top, false)]
    [InlineData(DockStyle.Left, false)]
    [InlineData(DockStyle.Top, true)]
    [InlineData(DockStyle.Left, true)]
    public void ToolStripPanel_Layout_RestoresFixedSizeToolStripBounds(DockStyle dock, bool minimize)
    {
        Size originalClientSize = new(700, 700);
        using Form form = new() { ClientSize = originalClientSize, ShowInTaskbar = false };
        using ToolStripPanel panel = new() { Dock = dock };
        bool horizontal = dock == DockStyle.Top;
        Size stripSize = horizontal ? new Size(200, 30) : new Size(30, 200);
        using ToolStrip first = new() { AutoSize = false, Size = stripSize };
        using ToolStrip second = new() { AutoSize = false, Size = stripSize };
        form.Controls.Add(panel);
        form.Show();
        panel.Join(first, horizontal ? new Point(20, 0) : new Point(0, 20));
        panel.Join(second, horizontal ? new Point(350, 0) : new Point(0, 350));
        Rectangle firstBounds = first.Bounds;
        Rectangle secondBounds = second.Bounds;
        Assert.Single(panel.Rows);

        if (minimize)
        {
            form.WindowState = FormWindowState.Minimized;
        }
        else
        {
            form.ClientSize = new Size(200, 200);
        }

        Assert.True(horizontal ? second.Width < secondBounds.Width : second.Height < secondBounds.Height);

        if (minimize)
        {
            form.WindowState = FormWindowState.Normal;
        }
        else
        {
            form.ClientSize = originalClientSize;
        }

        Assert.Equal(firstBounds, first.Bounds);
        Assert.Equal(secondBounds, second.Bounds);
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top)]
    [InlineData(DockStyle.Left)]
    public void ToolStripPanel_MoveRejectedInConstrainedRow_PreservesPositions(DockStyle dock)
    {
        Size originalClientSize = new(600, 600);
        using Form form = new() { ClientSize = originalClientSize, ShowInTaskbar = false };
        using ToolStripPanel panel = new() { Dock = dock };
        Size stripSize = new(50, 50);
        using ToolStrip first = new() { AutoSize = false, Size = stripSize, MinimumSize = stripSize };
        using ToolStrip second = new() { AutoSize = false, Size = stripSize, MinimumSize = stripSize };
        using ToolStrip third = new() { AutoSize = false, Size = stripSize, MinimumSize = stripSize };
        bool horizontal = dock == DockStyle.Top;
        form.Controls.Add(panel);
        form.Show();
        panel.Join(first, Point.Empty);
        panel.Join(second, horizontal ? new Point(300, 0) : new Point(0, 300));
        panel.Join(third, horizontal ? new Point(500, 0) : new Point(0, 500));
        Rectangle firstBounds = first.Bounds;
        Rectangle secondBounds = second.Bounds;
        Rectangle thirdBounds = third.Bounds;

        form.ClientSize = new Size(155, 155);
        Assert.Single(panel.Rows);
        Assert.NotEqual(secondBounds.Location, second.Location);
        Point currentLocation = first.Location;
        Point requestedLocation = horizontal
            ? new Point(currentLocation.X + 10, currentLocation.Y)
            : new Point(currentLocation.X, currentLocation.Y + 10);
        first.Location = requestedLocation;
        Assert.Equal(currentLocation, first.Location);

        form.ClientSize = originalClientSize;
        Assert.Equal(firstBounds, first.Bounds);
        Assert.Equal(secondBounds, second.Bounds);
        Assert.Equal(thirdBounds, third.Bounds);
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top)]
    [InlineData(DockStyle.Left)]
    public void ToolStripPanel_MoveAcceptedInConstrainedRow_PreservesNewPosition(DockStyle dock)
    {
        Size originalClientSize = new(600, 600);
        using Form form = new() { ClientSize = originalClientSize, ShowInTaskbar = false };
        using ToolStripPanel panel = new() { Dock = dock };
        Size stripSize = new(50, 50);
        using ToolStrip first = new() { AutoSize = false, Size = stripSize, MinimumSize = stripSize };
        using ToolStrip second = new() { AutoSize = false, Size = stripSize, MinimumSize = stripSize };
        bool horizontal = dock == DockStyle.Top;
        form.Controls.Add(panel);
        form.Show();
        panel.Join(first, Point.Empty);
        panel.Join(second, horizontal ? new Point(300, 0) : new Point(0, 300));

        form.ClientSize = new Size(200, 200);
        Point currentLocation = second.Location;
        Point requestedLocation = horizontal
            ? new Point(currentLocation.X - 30, currentLocation.Y)
            : new Point(currentLocation.X, currentLocation.Y - 30);
        second.Location = requestedLocation;
        Assert.Equal(requestedLocation, second.Location);

        form.ClientSize = originalClientSize;
        Assert.Equal(requestedLocation, second.Location);
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top)]
    [InlineData(DockStyle.Bottom)]
    [InlineData(DockStyle.Left)]
    [InlineData(DockStyle.Right)]
    public void ToolStripPanel_MdiParentMinimizeRestore_PreservesChildToolStripBounds(DockStyle dock)
    {
        using Form parent = new()
        {
            IsMdiContainer = true,
            ClientSize = new Size(800, 600),
            ShowInTaskbar = false
        };
        using Form child = new()
        {
            MdiParent = parent,
            ClientSize = new Size(600, 400)
        };
        using ToolStripPanel panel = new() { Dock = dock };
        child.Controls.Add(panel);
        using ToolStrip toolStrip = new();
        toolStrip.Items.Add("Child");
        parent.Show();
        child.Show();
        panel.Join(toolStrip, dock is DockStyle.Top or DockStyle.Bottom ? new Point(80, 0) : new Point(0, 80));
        Rectangle bounds = toolStrip.Bounds;

        parent.WindowState = FormWindowState.Minimized;
        panel.PerformLayout();
        Assert.Equal(FormWindowState.Minimized, parent.WindowState);
        Assert.Equal(FormWindowState.Normal, child.WindowState);

        parent.WindowState = FormWindowState.Normal;
        panel.PerformLayout();
        Assert.Equal(bounds, toolStrip.Bounds);
    }

    [WinFormsFact]
    public void ToolStripPanel_Ctor_Default()
    {
        using SubToolStripPanel control = new();
        Assert.Null(control.AccessibleDefaultActionDescription);
        Assert.Null(control.AccessibleDescription);
        Assert.Null(control.AccessibleName);
        Assert.Equal(AccessibleRole.Default, control.AccessibleRole);
        Assert.Null(control.ActiveControl);
        Assert.False(control.AllowDrop);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left, control.Anchor);
        Assert.False(control.AutoScroll);
        Assert.Equal(SizeF.Empty, control.AutoScaleDimensions);
        Assert.Equal(new SizeF(1, 1), control.AutoScaleFactor);
        Assert.Equal(Size.Empty, control.AutoScrollMargin);
        Assert.Equal(AutoScaleMode.None, control.AutoScaleMode);
        Assert.Equal(Size.Empty, control.AutoScrollMinSize);
        Assert.Equal(Point.Empty, control.AutoScrollPosition);
        Assert.True(control.AutoSize);
        Assert.Equal(Control.DefaultBackColor, control.BackColor);
        Assert.Null(control.BackgroundImage);
        Assert.Equal(ImageLayout.Tile, control.BackgroundImageLayout);
        Assert.NotNull(control.BindingContext);
        Assert.Same(control.BindingContext, control.BindingContext);
        Assert.Equal(0, control.Bottom);
        Assert.Equal(Rectangle.Empty, control.Bounds);
        Assert.False(control.CanEnableIme);
        Assert.False(control.CanFocus);
        Assert.True(control.CanRaiseEvents);
        Assert.False(control.CanSelect);
        Assert.False(control.Capture);
        Assert.True(control.CausesValidation);
        Assert.Equal(Rectangle.Empty, control.ClientRectangle);
        Assert.Equal(Size.Empty, control.ClientSize);
        Assert.Null(control.Container);
        Assert.False(control.ContainsFocus);
        Assert.Null(control.ContextMenuStrip);
        Assert.Empty(control.Controls);
        Assert.Same(control.Controls, control.Controls);
        Assert.False(control.Created);
        Assert.Equal(SizeF.Empty, control.CurrentAutoScaleDimensions);
        Assert.Equal(Cursors.Default, control.Cursor);
        Assert.Equal(Cursors.Default, control.DefaultCursor);
        Assert.Equal(ImeMode.Inherit, control.DefaultImeMode);
        Assert.Equal(Padding.Empty, control.DefaultMargin);
        Assert.Equal(Size.Empty, control.DefaultMaximumSize);
        Assert.Equal(Size.Empty, control.DefaultMinimumSize);
        Assert.Equal(Padding.Empty, control.DefaultPadding);
        Assert.Equal(Size.Empty, control.DefaultSize);
        Assert.False(control.DesignMode);
        Assert.Equal(Rectangle.Empty, control.DisplayRectangle);
        Assert.Equal(DockStyle.None, control.Dock);
        Assert.NotNull(control.DockPadding);
        Assert.Same(control.DockPadding, control.DockPadding);
        Assert.Equal(0, control.DockPadding.Top);
        Assert.Equal(0, control.DockPadding.Bottom);
        Assert.Equal(0, control.DockPadding.Left);
        Assert.Equal(0, control.DockPadding.Right);
        Assert.True(control.DoubleBuffered);
        Assert.True(control.Enabled);
        Assert.NotNull(control.Events);
        Assert.Same(control.Events, control.Events);
        Assert.False(control.Focused);
        Assert.Equal(Control.DefaultFont, control.Font);
        Assert.Equal(control.Font.Height, control.FontHeight);
        Assert.Equal(Control.DefaultForeColor, control.ForeColor);
        Assert.False(control.HasChildren);
        Assert.Equal(0, control.Height);
        Assert.NotNull(control.HorizontalScroll);
        Assert.Same(control.HorizontalScroll, control.HorizontalScroll);
        Assert.False(control.HScroll);
        Assert.Equal(ImeMode.NoControl, control.ImeMode);
        Assert.Equal(ImeMode.NoControl, control.ImeModeBase);
        Assert.False(control.IsAccessible);
        Assert.False(control.IsMirrored);
        Assert.NotNull(control.LayoutEngine);
        Assert.Same(control.LayoutEngine, control.LayoutEngine);
        Assert.Equal(0, control.Left);
        Assert.Equal(Point.Empty, control.Location);
        Assert.Equal(Padding.Empty, control.Margin);
        Assert.Equal(Size.Empty, control.MaximumSize);
        Assert.Equal(Size.Empty, control.MinimumSize);
        Assert.Equal(Padding.Empty, control.Padding);
        Assert.Null(control.Parent);
        Assert.Equal(Size.Empty, control.PreferredSize);
        Assert.Equal("Microsoft\u00AE .NET", control.ProductName);
        Assert.False(control.RecreatingHandle);
        Assert.Null(control.Region);
        Assert.True(control.ResizeRedraw);
        Assert.Equal(0, control.Right);
        Assert.Equal(RightToLeft.No, control.RightToLeft);
        Assert.Equal(new Padding(3, 0, 0, 0), control.RowMargin);
        Assert.True(control.ShowFocusCues);
        Assert.True(control.ShowKeyboardCues);
        Assert.Null(control.Site);
        Assert.Equal(Size.Empty, control.Size);
        Assert.Equal(0, control.TabIndex);
        Assert.False(control.TabStop);
        Assert.Empty(control.Text);
        Assert.Equal(0, control.Top);
        Assert.Null(control.TopLevelControl);
        Assert.False(control.UseWaitCursor);
        Assert.True(control.Visible);
        Assert.NotNull(control.VerticalScroll);
        Assert.Same(control.VerticalScroll, control.VerticalScroll);
        Assert.False(control.VScroll);
        Assert.Equal(0, control.Width);

        Assert.False(control.IsHandleCreated);
    }

    [WinFormsFact]
    public void ToolStripPanel_CreateParams_GetDefault_ReturnsExpected()
    {
        using SubToolStripPanel control = new();
        CreateParams createParams = control.CreateParams;
        Assert.Null(createParams.Caption);
        Assert.Null(createParams.ClassName);
        Assert.Equal(0x8, createParams.ClassStyle);
        Assert.Equal(0x10000, createParams.ExStyle);
        Assert.Equal(0, createParams.Height);
        Assert.Equal(IntPtr.Zero, createParams.Parent);
        Assert.Null(createParams.Param);
        Assert.Equal(0x56000000, createParams.Style);
        Assert.Equal(0, createParams.Width);
        Assert.Equal(0, createParams.X);
        Assert.Equal(0, createParams.Y);
        Assert.Same(createParams, control.CreateParams);
    }

    [WinFormsTheory]
    [BoolData]
    public void ToolStripPanel_AllowDrop_Set_GetReturnsExpected(bool value)
    {
        using ToolStripPanel panel = new()
        {
            AllowDrop = value
        };
        Assert.Equal(value, panel.AllowDrop);
        Assert.False(panel.IsHandleCreated);

        // Set same.
        panel.AllowDrop = value;
        Assert.Equal(value, panel.AllowDrop);
        Assert.False(panel.IsHandleCreated);

        // Set different.
        panel.AllowDrop = !value;
        Assert.Equal(!value, panel.AllowDrop);
        Assert.False(panel.IsHandleCreated);
    }

    [WinFormsTheory]
    [BoolData]
    public void AutoScroll_Set_GetReturnsExpected(bool value)
    {
        using ToolStripPanel panel = new()
        {
            AutoScroll = value
        };
        Assert.Equal(value, panel.AutoScroll);

        // Set same.
        panel.AutoScroll = value;
        Assert.Equal(value, panel.AutoScroll);
    }

    [WinFormsTheory]
    [CommonMemberData(typeof(CommonTestHelper), nameof(CommonTestHelper.GetSizeTheoryData), TestIncludeType.NoNegatives)]
    public void AutoScrollMargin_Set_GetReturnsExpected(Size value)
    {
        using ToolStripPanel panel = new()
        {
            AutoScrollMargin = value
        };
        Assert.Equal(value, panel.AutoScrollMargin);

        // Set same.
        panel.AutoScrollMargin = value;
        Assert.Equal(value, panel.AutoScrollMargin);
    }

    [WinFormsTheory]
    [CommonMemberData(typeof(CommonTestHelper), nameof(CommonTestHelper.GetSizeTheoryData), TestIncludeType.NoPositives)]
    public void AutoScrollMargin_SetInvalid_ThrowsArgumentOutOfRangeException(Size value)
    {
        using ToolStripPanel panel = new();
        Assert.Throws<ArgumentOutOfRangeException>("value", () => panel.AutoScrollMargin = value);
    }

    [WinFormsTheory]
    [CommonMemberData(typeof(CommonTestHelper), nameof(CommonTestHelper.GetSizeTheoryData))]
    public void AutoScrollMinSize_Set_GetReturnsExpected(Size value)
    {
        using ToolStripPanel panel = new()
        {
            AutoScrollMinSize = value
        };
        Assert.Equal(value, panel.AutoScrollMinSize);
        Assert.Equal(value != Size.Empty, panel.AutoScroll);

        // Set same.
        panel.AutoScrollMinSize = value;
        Assert.Equal(value, panel.AutoScrollMinSize);
        Assert.Equal(value != Size.Empty, panel.AutoScroll);
    }

    [WinFormsTheory]
    [BoolData]
    public void ToolStripPanel_AutoSize_Set_GetReturnsExpected(bool value)
    {
        using ToolStripPanel control = new();
        int layoutCallCount = 0;
        control.Layout += (sender, e) => layoutCallCount++;

        control.AutoSize = value;
        Assert.Equal(value, control.AutoSize);
        Assert.Equal(0, layoutCallCount);
        Assert.False(control.IsHandleCreated);

        // Set same.
        control.AutoSize = value;
        Assert.Equal(value, control.AutoSize);
        Assert.Equal(0, layoutCallCount);
        Assert.False(control.IsHandleCreated);

        // Set different.
        control.AutoSize = !value;
        Assert.Equal(!value, control.AutoSize);
        Assert.Equal(0, layoutCallCount);
        Assert.False(control.IsHandleCreated);
    }

    [WinFormsFact]
    public void ToolStripPanel_AutoSize_SetWithHandler_CallsAutoSizeChanged()
    {
        using ToolStripPanel control = new()
        {
            AutoSize = true
        };
        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(control, sender);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        };
        control.AutoSizeChanged += handler;

        // Set different.
        control.AutoSize = false;
        Assert.False(control.AutoSize);
        Assert.Equal(1, callCount);

        // Set same.
        control.AutoSize = false;
        Assert.False(control.AutoSize);
        Assert.Equal(1, callCount);

        // Set different.
        control.AutoSize = true;
        Assert.True(control.AutoSize);
        Assert.Equal(2, callCount);

        // Remove handler.
        control.AutoSizeChanged -= handler;
        control.AutoSize = false;
        Assert.False(control.AutoSize);
        Assert.Equal(2, callCount);
    }

    [WinFormsTheory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetPaddingTheoryData))]
    public void RowMargin_Set_GetReturnsExpected(Padding value)
    {
        using ToolStripPanel panel = new()
        {
            RowMargin = value
        };
        Assert.Equal(value, panel.RowMargin);

        // Set same.
        panel.RowMargin = value;
        Assert.Equal(value, panel.RowMargin);
    }

    [WinFormsFact]
    public void ToolStripPanel_GetAutoSizeMode_Invoke_ReturnsExpected()
    {
        using SubToolStripPanel control = new();
        Assert.Equal(AutoSizeMode.GrowOnly, control.GetAutoSizeMode());
    }

    [WinFormsTheory]
    [InlineData(ControlStyles.ContainerControl, true)]
    [InlineData(ControlStyles.UserPaint, true)]
    [InlineData(ControlStyles.Opaque, false)]
    [InlineData(ControlStyles.ResizeRedraw, true)]
    [InlineData(ControlStyles.FixedWidth, false)]
    [InlineData(ControlStyles.FixedHeight, false)]
    [InlineData(ControlStyles.StandardClick, true)]
    [InlineData(ControlStyles.Selectable, false)]
    [InlineData(ControlStyles.UserMouse, false)]
    [InlineData(ControlStyles.SupportsTransparentBackColor, true)]
    [InlineData(ControlStyles.StandardDoubleClick, true)]
    [InlineData(ControlStyles.AllPaintingInWmPaint, false)]
    [InlineData(ControlStyles.CacheText, false)]
    [InlineData(ControlStyles.EnableNotifyMessage, false)]
    [InlineData(ControlStyles.DoubleBuffer, false)]
    [InlineData(ControlStyles.OptimizedDoubleBuffer, true)]
    [InlineData(ControlStyles.UseTextForAccessibility, true)]
    [InlineData((ControlStyles)0, true)]
    [InlineData((ControlStyles)int.MaxValue, false)]
    [InlineData((ControlStyles)(-1), false)]
    public void ToolStripPanel_GetStyle_Invoke_ReturnsExpected(ControlStyles flag, bool expected)
    {
        using SubToolStripPanel control = new();
        Assert.Equal(expected, control.GetStyle(flag));

        // Call again to test caching.
        Assert.Equal(expected, control.GetStyle(flag));
    }

    [WinFormsFact]
    public void ToolStripPanel_GetTopLevel_Invoke_ReturnsExpected()
    {
        using SubToolStripPanel control = new();
        Assert.False(control.GetTopLevel());
    }

    [WinFormsFact]
    public void ToolStripPanel_AddTwoToolStrips_DoesNotThrowInvalidCastException()
    {
        using ToolStripContainer toolStripContainer = new();
        using ToolStrip toolStrip1 = new();
        using ToolStrip toolStrip2 = new();

        toolStripContainer.TopToolStripPanel.Controls.Add(toolStrip1);
        var exception = Record.Exception(() => toolStripContainer.TopToolStripPanel.Controls.Add(toolStrip2));

        Assert.Null(exception);
    }

    private class SubToolStripPanel : ToolStripPanel
    {
        public new SizeF AutoScaleFactor => base.AutoScaleFactor;

        public new bool CanEnableIme => base.CanEnableIme;

        public new bool CanRaiseEvents => base.CanRaiseEvents;

        public new CreateParams CreateParams => base.CreateParams;

        public new Cursor DefaultCursor => base.DefaultCursor;

        public new ImeMode DefaultImeMode => base.DefaultImeMode;

        public new Padding DefaultMargin => base.DefaultMargin;

        public new Size DefaultMaximumSize => base.DefaultMaximumSize;

        public new Size DefaultMinimumSize => base.DefaultMinimumSize;

        public new Padding DefaultPadding => base.DefaultPadding;

        public new Size DefaultSize => base.DefaultSize;

        public new bool DesignMode => base.DesignMode;

        public new bool DoubleBuffered
        {
            get => base.DoubleBuffered;
            set => base.DoubleBuffered = value;
        }

        public new EventHandlerList Events => base.Events;

        public new int FontHeight
        {
            get => base.FontHeight;
            set => base.FontHeight = value;
        }

        public new ImeMode ImeModeBase
        {
            get => base.ImeModeBase;
            set => base.ImeModeBase = value;
        }

        public new bool ResizeRedraw
        {
            get => base.ResizeRedraw;
            set => base.ResizeRedraw = value;
        }

        public new bool ShowFocusCues => base.ShowFocusCues;

        public new bool ShowKeyboardCues => base.ShowKeyboardCues;

        public new bool HScroll
        {
            get => base.HScroll;
            set => base.HScroll = value;
        }

        public new bool VScroll
        {
            get => base.VScroll;
            set => base.VScroll = value;
        }

        public new AutoSizeMode GetAutoSizeMode() => base.GetAutoSizeMode();

        public new bool GetStyle(ControlStyles flag) => base.GetStyle(flag);

        public new bool GetTopLevel() => base.GetTopLevel();
    }
}
