// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.Design;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Forms.Design.Tests.Mocks;
using Moq;

namespace System.Windows.Forms.Design.Tests;

[Collection("Sequential")]
public class ToolStripDesignerTests
{
    [WinFormsFact]
    public void ToolStripDesigner_InitialStateTest()
    {
        using ToolStripDesigner toolStripDesigner = new();
        using ToolStrip toolStrip = new();

        Mock<IComponentChangeService> mockIComponentChangeService = new(MockBehavior.Strict);
        Mock<IDesignerHost> mockDesignerHost = new(MockBehavior.Strict);
        mockDesignerHost
            .Setup(h => h.RootComponent)
            .Returns(toolStrip);
        mockDesignerHost
            .Setup(s => s.GetService(typeof(IComponentChangeService)))
            .Returns(mockIComponentChangeService.Object);
        mockDesignerHost.Setup(s => s.AddService(typeof(ToolStripKeyboardHandlingService), It.IsAny<object>()));
        mockDesignerHost.Setup(s => s.AddService(typeof(ISupportInSituService), It.IsAny<object>()));

        var mockSite = MockSite.CreateMockSiteWithDesignerHost(mockDesignerHost.Object);
        mockSite.Setup(s => s.GetService(typeof(BehaviorService))).Returns(null);
        mockSite.Setup(s => s.GetService(typeof(ToolStripAdornerWindowService))).Returns(null);

        Mock<IComponentChangeService> mockComponentChangeService = new(MockBehavior.Strict);
        mockSite.Setup(s => s.GetService(typeof(IComponentChangeService))).Returns(mockComponentChangeService.Object);

        toolStrip.Site = mockSite.Object;

        toolStripDesigner.Initialize(toolStrip);

        Assert.Empty(toolStripDesigner.AssociatedComponents);

        toolStrip.Items.Add("123");
        toolStrip.Items.Add("abc");

        Assert.Equal(
            toolStrip.Items.Cast<ToolStripItem>(),
            Assert.IsType<List<ToolStripItem>>(toolStripDesigner.AssociatedComponents));
        Assert.Equal(2, toolStripDesigner.ActionLists.Count);
        Assert.False(toolStripDesigner.CacheItems);
        Assert.False(toolStripDesigner.DontCloseOverflow);
        Assert.IsType<Rectangle>(toolStripDesigner.DragBoxFromMouseDown);
        Assert.Equal(Rectangle.Empty, toolStripDesigner.DragBoxFromMouseDown);
        Assert.False(toolStripDesigner.EditingCollection);
        Assert.NotNull(toolStripDesigner.EditManager);
        Assert.Null(toolStripDesigner.Editor);
        Assert.Null(toolStripDesigner.EditorNode);
        Assert.Null(toolStripDesigner.EditorToolStrip);
        Assert.False(toolStripDesigner.FireSyncSelection);
        Assert.IsType<int>(toolStripDesigner.IndexOfItemUnderMouseToDrag);
        Assert.Equal(-1, toolStripDesigner.IndexOfItemUnderMouseToDrag);
        Assert.Equal(-1, toolStripDesigner.IndexOfItemUnderMouseToDrag);
        Assert.Null(toolStripDesigner.InsertTransaction);
        Assert.Empty(toolStripDesigner.Items);
        List<ToolStripItem> cachedItems = Assert.IsType<List<ToolStripItem>>(toolStripDesigner.Items);
        cachedItems.Add(toolStrip.Items[0]);
        Assert.Same(cachedItems, toolStripDesigner.Items);
        Assert.Same(toolStrip.Items[0], Assert.Single(toolStripDesigner.Items));
        Assert.Null(toolStripDesigner.NewItemTransaction);
        Assert.NotNull(toolStripDesigner.SelectionService);
        Assert.True(toolStripDesigner.SerializePerformLayout);
        Assert.True(toolStripDesigner.SupportEditing);
        Assert.True(toolStripDesigner.Visible);
    }

    [WinFormsTheory]
    [InlineData(SystemColorMode.Classic, 255, 171)]
    [InlineData(SystemColorMode.Dark, 32, 240)]
    public void MiniToolStripRenderer_RenderLabelBackground_RendersExpectedColors(
        SystemColorMode colorMode,
        int expectedBackgroundComponent,
        int expectedForegroundComponent)
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        var applicationAccessor = typeof(Application).TestAccessor.Dynamic;
        SystemColorMode? originalColorMode = applicationAccessor.s_colorMode;

        try
        {
            applicationAccessor.s_colorMode = colorMode;

            var owner = (ToolStripTemplateNode)RuntimeHelpers.GetUninitializedObject(
                typeof(ToolStripTemplateNode));
            using ToolStripTemplateNode.TransparentToolStrip toolStrip = new(owner)
            {
                Size = new Size(100, 25)
            };

            owner.TestAccessor.Dynamic._miniToolStrip = toolStrip;

            using ToolStripLabel item = new("Type Here")
            {
                Name = ToolStripTemplateNode.CenterLabelName,
                Size = toolStrip.Size
            };
            toolStrip.Items.Add(item);

            ToolStripTemplateNode.MiniToolStripRenderer renderer = new(owner)
            {
                State = (int)TemplateNodeSelectionState.TemplateNodeSelected
            };

            using Bitmap bitmap = new(toolStrip.Width, toolStrip.Height);
            using Graphics graphics = Graphics.FromImage(bitmap);
            ToolStripItemRenderEventArgs eventArgs = new(graphics, item);

            renderer.TestAccessor.Dynamic.OnRenderLabelBackground(eventArgs);

            bitmap.GetPixel(5, 5).Should().Be(Color.FromArgb(
                expectedBackgroundComponent,
                expectedBackgroundComponent,
                expectedBackgroundComponent));
            item.ForeColor.Should().Be(Color.FromArgb(
                expectedForegroundComponent,
                expectedForegroundComponent,
                expectedForegroundComponent));
        }
        finally
        {
            applicationAccessor.s_colorMode = originalColorMode;
        }
    }

    [WinFormsTheory]
    [InlineData(false, (int)TemplateNodeSelectionState.None)]
    [InlineData(true, (int)TemplateNodeSelectionState.None)]
    [InlineData(false, (int)TemplateNodeSelectionState.TemplateNodeSelected)]
    [InlineData(true, (int)TemplateNodeSelectionState.TemplateNodeSelected)]
    [InlineData(false, (int)TemplateNodeSelectionState.MouseOverLabel)]
    [InlineData(true, (int)TemplateNodeSelectionState.MouseOverLabel)]
    [InlineData(false, (int)TemplateNodeSelectionState.MouseOverHotRegion)]
    [InlineData(true, (int)TemplateNodeSelectionState.MouseOverHotRegion)]
    [InlineData(false, (int)TemplateNodeSelectionState.HotRegionSelected)]
    [InlineData(true, (int)TemplateNodeSelectionState.HotRegionSelected)]
    public void MiniToolStripRenderer_RenderLabelBackground_ColorModeChanges_UpdatesColors(
        bool darkMode,
        int state)
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        var applicationAccessor = typeof(Application).TestAccessor.Dynamic;
        SystemColorMode? originalColorMode = applicationAccessor.s_colorMode;

        try
        {
            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Classic : SystemColorMode.Dark;

            var owner = (ToolStripTemplateNode)RuntimeHelpers.GetUninitializedObject(typeof(ToolStripTemplateNode));
            using ToolStripTemplateNode.TransparentToolStrip toolStrip = new(owner)
            {
                Size = new Size(100, 25)
            };
            owner.TestAccessor.Dynamic._miniToolStrip = toolStrip;

            using ToolStripLabel item = new("Type Here")
            {
                Name = ToolStripTemplateNode.CenterLabelName,
                AutoSize = false,
                Size = toolStrip.Size
            };
            toolStrip.Items.Add(item);

            ToolStripTemplateNode.MiniToolStripRenderer renderer = new(owner)
            {
                State = (int)TemplateNodeSelectionState.TemplateNodeSelected
            };
            using Bitmap bitmap = new(toolStrip.Width, toolStrip.Height);
            using Graphics graphics = Graphics.FromImage(bitmap);
            ToolStripItemRenderEventArgs eventArgs = new(graphics, item);
            renderer.DrawLabelBackground(eventArgs);

            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Dark : SystemColorMode.Classic;
            renderer.State = state;
            graphics.Clear(Color.Magenta);
            renderer.DrawLabelBackground(eventArgs);

            bitmap.GetPixel(5, 5).Should().Be(darkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(255, 255, 255));
            item.ForeColor.Should().Be(darkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(171, 171, 171));

            if (state is (int)TemplateNodeSelectionState.MouseOverHotRegion or (int)TemplateNodeSelectionState.HotRegionSelected)
            {
                Color expectedColor = (darkMode, state) switch
                {
                    (true, (int)TemplateNodeSelectionState.MouseOverHotRegion) => Color.FromArgb(70, 70, 70),
                    (true, _) => Color.FromArgb(90, 90, 90),
                    (false, (int)TemplateNodeSelectionState.MouseOverHotRegion) => Color.FromArgb(193, 210, 238),
                    (false, _) => Color.FromArgb(152, 181, 226)
                };
                Rectangle hotRegion = owner.HotRegion;
                bitmap.GetPixel(hotRegion.Left + 1, hotRegion.Top + 1).Should().Be(expectedColor);
            }
        }
        finally
        {
            applicationAccessor.s_colorMode = originalColorMode;
        }
    }

    [WinFormsTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void MiniToolStripRenderer_RenderToolStrip_ColorModeChanges_UpdatesColors(bool darkMode, bool renderBorder)
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        var applicationAccessor = typeof(Application).TestAccessor.Dynamic;
        SystemColorMode? originalColorMode = applicationAccessor.s_colorMode;

        try
        {
            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Classic : SystemColorMode.Dark;

            var owner = (ToolStripTemplateNode)RuntimeHelpers.GetUninitializedObject(typeof(ToolStripTemplateNode));
            using MenuStrip menuStrip = new();
            owner.TestAccessor.Dynamic._component = menuStrip;
            ToolStripTemplateNode.MiniToolStripRenderer renderer = new(owner);
            using Bitmap bitmap = new(menuStrip.Width, menuStrip.Height);
            using Graphics graphics = Graphics.FromImage(bitmap);
            ToolStripRenderEventArgs eventArgs = new(graphics, menuStrip);

            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Dark : SystemColorMode.Classic;
            graphics.Clear(Color.Magenta);

            if (renderBorder)
            {
                renderer.DrawToolStripBorder(eventArgs);
            }
            else
            {
                renderer.DrawToolStripBackground(eventArgs);
            }

            bitmap.GetPixel(0, 0).Should().Be(darkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(255, 255, 255));
        }
        finally
        {
            applicationAccessor.s_colorMode = originalColorMode;
        }
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MiniToolStripRenderer_RenderSplitButtonBackground_ColorModeChanges_UpdatesColors(bool darkMode)
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        var applicationAccessor = typeof(Application).TestAccessor.Dynamic;
        SystemColorMode? originalColorMode = applicationAccessor.s_colorMode;

        try
        {
            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Classic : SystemColorMode.Dark;

            var owner = (ToolStripTemplateNode)RuntimeHelpers.GetUninitializedObject(typeof(ToolStripTemplateNode));
            using ToolStrip toolStrip = new();
            using ToolStripSplitButton item = new()
            {
                AutoSize = false,
                Size = new Size(100, 25)
            };
            toolStrip.Items.Add(item);
            ToolStripTemplateNode.MiniToolStripRenderer renderer = new(owner)
            {
                State = (int)TemplateNodeSelectionState.DropDownSelected
            };
            using Bitmap bitmap = new(item.Width, item.Height);
            using Graphics graphics = Graphics.FromImage(bitmap);
            ToolStripItemRenderEventArgs eventArgs = new(graphics, item);

            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Dark : SystemColorMode.Classic;
            renderer.DrawSplitButton(eventArgs);

            Rectangle bounds = item.DropDownButtonBounds;
            bitmap.GetPixel(bounds.Left + 2, bounds.Top + 2).Should().Be(
                darkMode ? Color.FromArgb(70, 70, 70) : Color.FromArgb(193, 210, 238));
        }
        finally
        {
            applicationAccessor.s_colorMode = originalColorMode;
        }
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MiniToolStripRenderer_RenderItemText_ColorModeChanges_UpdatesTextColor(bool darkMode)
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        var applicationAccessor = typeof(Application).TestAccessor.Dynamic;
        SystemColorMode? originalColorMode = applicationAccessor.s_colorMode;

        try
        {
            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Classic : SystemColorMode.Dark;

            var owner = (ToolStripTemplateNode)RuntimeHelpers.GetUninitializedObject(typeof(ToolStripTemplateNode));
            using ToolStripLabel item = new("Type Here")
            {
                Name = ToolStripTemplateNode.CenterLabelName
            };
            ToolStripTemplateNode.MiniToolStripRenderer renderer = new(owner);
            using Bitmap bitmap = new(100, 25);
            using Graphics graphics = Graphics.FromImage(bitmap);
            ToolStripItemTextRenderEventArgs eventArgs = new(
                graphics, item, item.Text, new Rectangle(0, 0, bitmap.Width, bitmap.Height), Color.Magenta, item.Font, TextFormatFlags.Default);

            applicationAccessor.s_colorMode = darkMode ? SystemColorMode.Dark : SystemColorMode.Classic;
            renderer.DrawItemText(eventArgs);

            eventArgs.TextColor.Should().Be(darkMode ? Color.FromArgb(240, 240, 240) : Color.Magenta);
        }
        finally
        {
            applicationAccessor.s_colorMode = originalColorMode;
        }
    }
}
