// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Windows.Forms.Metafiles;
using static System.Windows.Forms.Metafiles.DataHelpers;

namespace System.Windows.Forms.Tests;

public partial class ControlPaintTests
{
    [WinFormsFact]
    public void ControlPaint_DrawBorder_Solid_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, Color.Blue, ButtonBorderStyle.Solid);

        emf.Validate(
            state,
            Validate.Rectangle(
                // We match the legacy GDI+ rendering, where the right and bottom are drawn inside the bounds
                new Rectangle(10, 10, 9, 9),
                State.Pen(1, Color.Blue, PEN_STYLE.PS_SOLID)));
    }

    [WinFormsFact]
    public void ControlPaint_DrawBorder_Inset_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, Color.Gray, ButtonBorderStyle.Inset);

        // For whatever reason GDI+ renders as polylines scaled 16x with a 1/16th world transform applied.
        // For test readability we'll transform the points from our coordinates to the logical coordinates.
        Matrix3x2 oneSixteenth = Matrix3x2.CreateScale(0.0625f);
        Matrix3x2 times16 = Matrix3x2.CreateScale(16.0f);

        // This is the default pen style GDI+ renders polylines with
        PEN_STYLE penStyle = PEN_STYLE.PS_SOLID | PEN_STYLE.PS_JOIN_ROUND | PEN_STYLE.PS_COSMETIC |
            PEN_STYLE.PS_ENDCAP_FLAT | PEN_STYLE.PS_JOIN_MITER | PEN_STYLE.PS_GEOMETRIC;

        emf.Validate(
            state,
            // Top
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 19, 10),
                State.Pen(16, ControlPaint.DarkDark(Color.Gray), penStyle),
                State.Transform(oneSixteenth)),
            // Left
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 10, 19),
                State.Pen(16, ControlPaint.DarkDark(Color.Gray), penStyle),
                State.Transform(oneSixteenth)),
            // Bottom
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 19, 19, 19),
                State.Pen(16, ControlPaint.LightLight(Color.Gray), penStyle),
                State.Transform(oneSixteenth)),
            // Right
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 19, 10, 19, 19),
                State.Pen(16, ControlPaint.LightLight(Color.Gray), penStyle),
                State.Transform(oneSixteenth)),
            // Top inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 18, 11),
                State.Pen(16, ControlPaint.Light(Color.Gray), penStyle),
                State.Transform(oneSixteenth)),
            // Left inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 11, 18),
                State.Pen(16, ControlPaint.Light(Color.Gray), penStyle),
                State.Transform(oneSixteenth)));
    }

    [WinFormsFact]
    public void ControlPaint_DrawBorder_Inset_ControlColor_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, SystemColors.Control, ButtonBorderStyle.Inset);

        // For whatever reason GDI+ renders as polylines scaled 16x with a 1/16th world transform applied.
        // For test readability we'll transform the points from our coordinates to the logical coordinates.
        Matrix3x2 oneSixteenth = Matrix3x2.CreateScale(0.0625f);
        Matrix3x2 times16 = Matrix3x2.CreateScale(16.0f);

        // This is the default pen style GDI+ renders polylines with
        PEN_STYLE penStyle = PEN_STYLE.PS_SOLID | PEN_STYLE.PS_JOIN_ROUND | PEN_STYLE.PS_COSMETIC |
            PEN_STYLE.PS_ENDCAP_FLAT | PEN_STYLE.PS_JOIN_MITER | PEN_STYLE.PS_GEOMETRIC;

        emf.Validate(
            state,
            // Top
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 19, 10),
                State.Pen(16, ControlPaint.DarkDark(SystemColors.Control), penStyle),
                State.Transform(oneSixteenth)),
            // Left
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 10, 19),
                State.Pen(16, ControlPaint.DarkDark(SystemColors.Control), penStyle),
                State.Transform(oneSixteenth)),
            // Bottom
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 19, 19, 19),
                State.Pen(16, ControlPaint.LightLight(SystemColors.Control), penStyle),
                State.Transform(oneSixteenth)),
            // Right
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 19, 10, 19, 19),
                State.Pen(16, ControlPaint.LightLight(SystemColors.Control), penStyle),
                State.Transform(oneSixteenth)),
            // Top inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 18, 11),
                State.Pen(16, ControlPaint.Light(SystemColors.Control), penStyle),
                State.Transform(oneSixteenth)),
            // Left inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 11, 18),
                State.Pen(16, ControlPaint.Light(SystemColors.Control), penStyle),
                State.Transform(oneSixteenth)),

            // The bottom/right insets are only drawn if the original color was SystemColors.Control.

            // Bottom inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 18, 18, 18),
                State.Pen(16, SystemColors.ControlLight, penStyle),
                State.Transform(oneSixteenth)),
            // Right inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 18, 11, 18, 18),
                State.Pen(16, SystemColors.ControlLight, penStyle),
                State.Transform(oneSixteenth)));
    }

    [WinFormsFact]
    public void ControlPaint_DrawBorder_OutSet_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, Color.PeachPuff, ButtonBorderStyle.Outset);

        string dump = emf.RecordsToStringWithState(state);

        // For whatever reason GDI+ renders as polylines scaled 16x with a 1/16th world transform applied.
        // For test readability we'll transform the points from our coordinates to the logical coordinates.
        Matrix3x2 oneSixteenth = Matrix3x2.CreateScale(0.0625f);
        Matrix3x2 times16 = Matrix3x2.CreateScale(16.0f);

        // This is the default pen style GDI+ renders polylines with
        PEN_STYLE penStyle = PEN_STYLE.PS_SOLID | PEN_STYLE.PS_JOIN_ROUND | PEN_STYLE.PS_COSMETIC |
            PEN_STYLE.PS_ENDCAP_FLAT | PEN_STYLE.PS_JOIN_MITER | PEN_STYLE.PS_GEOMETRIC;

        emf.Validate(
            state,
            // Top
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 19, 10),
                State.Pen(16, ControlPaint.LightLight(Color.PeachPuff), penStyle),
                State.Transform(oneSixteenth)),
            // Left
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 10, 19),
                State.Pen(16, ControlPaint.LightLight(Color.PeachPuff), penStyle),
                State.Transform(oneSixteenth)),
            // Bottom
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 19, 19, 19),
                State.Pen(16, ControlPaint.DarkDark(Color.PeachPuff), penStyle),
                State.Transform(oneSixteenth)),
            // Right
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 19, 10, 19, 19),
                State.Pen(16, ControlPaint.DarkDark(Color.PeachPuff), penStyle),
                State.Transform(oneSixteenth)),
            // Top inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 18, 11),
                State.Pen(16, Color.PeachPuff, penStyle),
                State.Transform(oneSixteenth)),
            // Left inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 11, 18),
                State.Pen(16, Color.PeachPuff, penStyle),
                State.Transform(oneSixteenth)),
            // Bottom inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 18, 18, 18),
                State.Pen(16, ControlPaint.Dark(Color.PeachPuff), penStyle),
                State.Transform(oneSixteenth)),
            // Right inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 18, 11, 18, 18),
                State.Pen(16, ControlPaint.Dark(Color.PeachPuff), penStyle),
                State.Transform(oneSixteenth)));
    }

    [WinFormsFact]
    public void ControlPaint_DrawBorder_OutSet_ControlColor_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, SystemColors.Control, ButtonBorderStyle.Outset);

        string dump = emf.RecordsToStringWithState(state);

        // For whatever reason GDI+ renders as polylines scaled 16x with a 1/16th world transform applied.
        // For test readability we'll transform the points from our coordinates to the logical coordinates.
        Matrix3x2 oneSixteenth = Matrix3x2.CreateScale(0.0625f);
        Matrix3x2 times16 = Matrix3x2.CreateScale(16.0f);

        // This is the default pen style GDI+ renders polylines with
        PEN_STYLE penStyle = PEN_STYLE.PS_SOLID | PEN_STYLE.PS_JOIN_ROUND | PEN_STYLE.PS_COSMETIC |
            PEN_STYLE.PS_ENDCAP_FLAT | PEN_STYLE.PS_JOIN_MITER | PEN_STYLE.PS_GEOMETRIC;

        emf.Validate(
            state,
            // Top
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 19, 10),
                State.Pen(16, SystemColors.ControlLightLight, penStyle),
                State.Transform(oneSixteenth)),
            // Left
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 10, 10, 19),
                State.Pen(16, SystemColors.ControlLightLight, penStyle),
                State.Transform(oneSixteenth)),
            // Bottom
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 10, 19, 19, 19),
                State.Pen(16, SystemColors.ControlDarkDark, penStyle),
                State.Transform(oneSixteenth)),
            // Right
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 19, 10, 19, 19),
                State.Pen(16, SystemColors.ControlDarkDark, penStyle),
                State.Transform(oneSixteenth)),
            // Top inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 18, 11),
                State.Pen(16, SystemColors.Control, penStyle),
                State.Transform(oneSixteenth)),
            // Left inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 11, 11, 18),
                State.Pen(16, SystemColors.Control, penStyle),
                State.Transform(oneSixteenth)),
            // Bottom inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 11, 18, 18, 18),
                State.Pen(16, SystemColors.ControlDark, penStyle),
                State.Transform(oneSixteenth)),
            // Right inset
            Validate.Polyline16(
                bounds: null,
                PointArray(times16, 18, 11, 18, 18),
                State.Pen(16, SystemColors.ControlDark, penStyle),
                State.Transform(oneSixteenth)));
    }

    [WinFormsFact]
    public void ControlPaint_DrawBorder_Dotted_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, Color.Green, ButtonBorderStyle.Dotted);

        // For whatever reason GDI+ renders as polygons scaled 16x with a 1/16th world transform applied.
        Matrix3x2 oneSixteenth = Matrix3x2.CreateScale(0.0625f);

        // This is the default pen style GDI+ renders dotted lines with
        PEN_STYLE penStyle = PEN_STYLE.PS_SOLID | PEN_STYLE.PS_JOIN_ROUND | PEN_STYLE.PS_COSMETIC |
            PEN_STYLE.PS_ENDCAP_FLAT | PEN_STYLE.PS_JOIN_BEVEL | PEN_STYLE.PS_GEOMETRIC;

        emf.Validate(
        state,
            Validate.PolyPolygon16(
                new Rectangle(8, 8, 13, 13),
                polyCount: 18,
                State.Transform(oneSixteenth),
                State.Pen(16, Color.Green, penStyle),
                State.Brush(Color.Green, BRUSH_STYLE.BS_SOLID)));
    }

    [WinFormsFact]
    public void ControlPaint_DrawBorder_Dashed_Rendering()
    {
        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        using Graphics graphics = Graphics.FromHdc((IntPtr)emf.HDC);

        Rectangle bounds = new(10, 10, 10, 10);
        ControlPaint.DrawBorder(graphics, bounds, Color.Pink, ButtonBorderStyle.Dashed);

        // For whatever reason GDI+ renders as polygons scaled 16x with a 1/16th world transform applied.
        Matrix3x2 oneSixteenth = Matrix3x2.CreateScale(0.0625f);

        // This is the default pen style GDI+ renders dotted lines with
        PEN_STYLE penStyle = PEN_STYLE.PS_SOLID | PEN_STYLE.PS_JOIN_ROUND | PEN_STYLE.PS_COSMETIC |
            PEN_STYLE.PS_ENDCAP_FLAT | PEN_STYLE.PS_JOIN_BEVEL | PEN_STYLE.PS_GEOMETRIC;

        emf.Validate(
        state,
            Validate.PolyPolygon16(
                new Rectangle(8, 8, 13, 13),
                polyCount: 9,
                State.Transform(oneSixteenth),
                State.Pen(16, Color.Pink, penStyle),
                State.Brush(Color.Pink, BRUSH_STYLE.BS_SOLID)));
    }

#nullable enable
    public static IEnumerable<object[]> DeviceContextDrawings()
    {
        yield return Drawing("Border", (dc, r) => ControlPaint.DrawBorder(dc, r, Color.Black, ButtonBorderStyle.Solid));
        yield return Drawing("BorderEdges", (dc, r) => ControlPaint.DrawBorder(
            dc, r,
            Color.Black, 1, ButtonBorderStyle.Dotted,
            Color.Black, 1, ButtonBorderStyle.Dashed,
            Color.Black, 1, ButtonBorderStyle.Solid,
            Color.Black, 1, ButtonBorderStyle.Inset));
        yield return Drawing("Border3DRectangle", (dc, r) => ControlPaint.DrawBorder3D(dc, r));
        yield return Drawing("Border3DRectangleStyle", (dc, r) => ControlPaint.DrawBorder3D(dc, r, Border3DStyle.Raised));
        yield return Drawing("Border3DRectangleSides", (dc, r) => ControlPaint.DrawBorder3D(dc, r, Border3DStyle.Raised, Border3DSide.Top));
        yield return Drawing("Border3DCoordinates", (dc, r) => ControlPaint.DrawBorder3D(dc, r.X, r.Y, r.Width, r.Height));
        yield return Drawing("Border3DCoordinatesStyle", (dc, r) => ControlPaint.DrawBorder3D(dc, r.X, r.Y, r.Width, r.Height, Border3DStyle.Raised));
        yield return Drawing("Border3DCoordinatesSides", (dc, r) => ControlPaint.DrawBorder3D(dc, r.X, r.Y, r.Width, r.Height, Border3DStyle.Raised, Border3DSide.Top));
        yield return Drawing("ButtonRectangle", (dc, r) => ControlPaint.DrawButton(dc, r, ButtonState.Normal));
        yield return Drawing("ButtonCoordinates", (dc, r) => ControlPaint.DrawButton(dc, r.X, r.Y, r.Width, r.Height, ButtonState.Normal));
        yield return Drawing("CaptionRectangle", (dc, r) => ControlPaint.DrawCaptionButton(dc, r, CaptionButton.Close, ButtonState.Normal));
        yield return Drawing("CaptionCoordinates", (dc, r) => ControlPaint.DrawCaptionButton(dc, r.X, r.Y, r.Width, r.Height, CaptionButton.Close, ButtonState.Normal));
        yield return Drawing("CheckBoxRectangle", (dc, r) => ControlPaint.DrawCheckBox(dc, r, ButtonState.Checked));
        yield return Drawing("CheckBoxCoordinates", (dc, r) => ControlPaint.DrawCheckBox(dc, r.X, r.Y, r.Width, r.Height, ButtonState.Checked));
        yield return Drawing("ComboRectangle", (dc, r) => ControlPaint.DrawComboButton(dc, r, ButtonState.Normal));
        yield return Drawing("ComboCoordinates", (dc, r) => ControlPaint.DrawComboButton(dc, r.X, r.Y, r.Width, r.Height, ButtonState.Normal));
        yield return Drawing("Container", ControlPaint.DrawContainerGrabHandle);
        yield return Drawing("Focus", (dc, r) => ControlPaint.DrawFocusRectangle(dc, r));
        yield return Drawing("FocusColors", (dc, r) => ControlPaint.DrawFocusRectangle(dc, r, Color.Black, Color.White));
        yield return Drawing("GrabHandle", (dc, r) => ControlPaint.DrawGrabHandle(dc, r, true, true));
        yield return Drawing("Grid", (dc, r) => ControlPaint.DrawGrid(dc, r, new Size(3, 5), Color.White));
        yield return Drawing("LockedFrame", (dc, r) => ControlPaint.DrawLockedFrame(dc, r, true));
        yield return Drawing("MenuRectangle", (dc, r) => ControlPaint.DrawMenuGlyph(dc, r, MenuGlyph.Arrow));
        yield return Drawing("MenuCoordinates", (dc, r) => ControlPaint.DrawMenuGlyph(dc, r.X, r.Y, r.Width, r.Height, MenuGlyph.Arrow));
        yield return Drawing("MenuRectangleColors", (dc, r) => ControlPaint.DrawMenuGlyph(dc, r, MenuGlyph.Arrow, Color.Blue, Color.Yellow));
        yield return Drawing("MenuCoordinatesColors", (dc, r) => ControlPaint.DrawMenuGlyph(dc, r.X, r.Y, r.Width, r.Height, MenuGlyph.Arrow, Color.Blue, Color.Yellow));
        yield return Drawing("MixedRectangle", (dc, r) => ControlPaint.DrawMixedCheckBox(dc, r, ButtonState.Checked));
        yield return Drawing("MixedCoordinates", (dc, r) => ControlPaint.DrawMixedCheckBox(dc, r.X, r.Y, r.Width, r.Height, ButtonState.Checked));
        yield return Drawing("RadioRectangle", (dc, r) => ControlPaint.DrawRadioButton(dc, r, ButtonState.Checked));
        yield return Drawing("RadioCoordinates", (dc, r) => ControlPaint.DrawRadioButton(dc, r.X, r.Y, r.Width, r.Height, ButtonState.Checked));
        yield return Drawing("ScrollRectangle", (dc, r) => ControlPaint.DrawScrollButton(dc, r, ScrollButton.Down, ButtonState.Normal));
        yield return Drawing("ScrollCoordinates", (dc, r) => ControlPaint.DrawScrollButton(dc, r.X, r.Y, r.Width, r.Height, ScrollButton.Down, ButtonState.Normal));
        yield return Drawing("Selection", (dc, r) => ControlPaint.DrawSelectionFrame(dc, true, r, Rectangle.Inflate(r, -3, -3), Color.White));
        yield return Drawing("SizeGripRectangle", (dc, r) => ControlPaint.DrawSizeGrip(dc, Color.White, r));
        yield return Drawing("SizeGripCoordinates", (dc, r) => ControlPaint.DrawSizeGrip(dc, Color.White, r.X, r.Y, r.Width, r.Height));
        yield return Drawing("VisualStyle", ControlPaint.DrawVisualStyleBorder);
        yield return Drawing("BorderInset", (dc, r) => ControlPaint.DrawBorder(dc, r, Color.Gray, ButtonBorderStyle.Inset));
        yield return Drawing("BorderOutset", (dc, r) => ControlPaint.DrawBorder(dc, r, SystemColors.Control, ButtonBorderStyle.Outset));
        yield return Drawing("FlatChecked", (dc, r) => ControlPaint.DrawCheckBox(dc, r, ButtonState.Flat | ButtonState.Checked));
        yield return Drawing("InactiveSelection", (dc, r) => ControlPaint.DrawSelectionFrame(dc, false, r, Rectangle.Inflate(r, -3, -3), Color.Black));

        static object[] Drawing(string name, Action<IDeviceContext, Rectangle> draw) => [name, draw];
    }

    [WinFormsTheory]
    [MemberData(nameof(DeviceContextDrawings))]
    public void ControlPaint_IDeviceContext_RendersWithoutCreatingGraphics(string name, Action<IDeviceContext, Rectangle> draw)
    {
        using Bitmap bitmap = new(64, 64, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        Rectangle bounds = new(12, 14, 24, 24);
        IntPtr hdc = graphics.GetHdc();
        try
        {
            using PaintEventArgs e = new((HDC)hdc, new Rectangle(0, 0, 64, 64));
            Assert.Null(e.TryGetGraphics());
            draw(e, bounds);
            Assert.Null(e.TryGetGraphics());
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
        Assert.True(ContainsChangedPixel(bitmap, bounds), name);
    }

    [WinFormsTheory]
    [MemberData(nameof(DeviceContextDrawings))]
    public void ControlPaint_IDeviceContext_RawHdcRendersAndReleases(string name, Action<IDeviceContext, Rectangle> draw)
    {
        using Bitmap bitmap = new(64, 64, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        Rectangle bounds = new(12, 14, 24, 24);
        IntPtr hdc = graphics.GetHdc();
        try
        {
            using BorrowedDeviceContext dc = new(hdc);
            PInvokeCore.SetTextColor((HDC)hdc, Color.Blue);
            PInvokeCore.SetBkColor((HDC)hdc, Color.Green);
            DeviceContextState before = new((HDC)hdc);
            draw(dc, bounds);
            DeviceContextState after = new((HDC)hdc);
            Assert.Equal(before.BackColor, after.BackColor);
            Assert.Equal(before.TextColor, after.TextColor);
            Assert.Equal(before.Rop2Mode, after.Rop2Mode);
            Assert.Equal(before.BackgroundMode, after.BackgroundMode);
            Assert.Equal(before.BrushOrigin, after.BrushOrigin);
            Assert.Equal(before.SelectedPen, after.SelectedPen);
            Assert.Equal(before.SelectedBrush, after.SelectedBrush);
            Assert.True(dc.Acquisitions > 0, name);
            Assert.Equal(dc.Acquisitions, dc.Releases);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
        Assert.True(ContainsChangedPixel(bitmap, bounds), name);
    }

    [WinFormsTheory]
    [MemberData(nameof(DeviceContextDrawings))]
    public void ControlPaint_IDeviceContext_NullContextThrows(string name, Action<IDeviceContext, Rectangle> draw)
    {
        _ = name;
        Assert.Throws<ArgumentNullException>("deviceContext", () => draw(null!, new Rectangle(1, 2, 20, 20)));
    }

    [WinFormsTheory]
    [InlineData(ButtonBorderStyle.Solid)]
    [InlineData(ButtonBorderStyle.Dashed)]
    [InlineData(ButtonBorderStyle.Dotted)]
    public void ControlPaint_IDeviceContext_BorderPreservesBoundsStyleAndIgnoresAlpha(ButtonBorderStyle style)
    {
        using Bitmap bitmap = new(40, 40, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        IntPtr hdc = graphics.GetHdc();
        try
        {
            using BorrowedDeviceContext dc = new(hdc);
            ControlPaint.DrawBorder(dc, new Rectangle(5, 6, 25, 20), Color.FromArgb(0, 255, 0, 0), style);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        int red = 0;
        int white = 0;
        for (int x = 6; x < 28; x++)
        {
            if (bitmap.GetPixel(x, 6).ToArgb() == Color.Red.ToArgb())
            {
                red++;
            }
            else if (bitmap.GetPixel(x, 6).ToArgb() == Color.White.ToArgb())
            {
                white++;
            }
        }

        Assert.True(red > 0);
        Assert.Equal(style != ButtonBorderStyle.Solid, white > 0);
        Assert.True(ContainsChangedPixel(bitmap, new Rectangle(29, 6, 1, 20), Color.White));
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(30, 7).ToArgb());
    }

    [WinFormsFact]
    public void ControlPaint_IDeviceContext_GraphicsAppliesClipAndTranslationWithoutAlphaBlending()
    {
        using Bitmap bitmap = new(32, 32, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.TranslateTransform(5, 7);
        Rectangle clip = new(0, 0, 8, 8);
        graphics.SetClip(clip);
        ControlPaint.DrawBorder((IDeviceContext)graphics, new Rectangle(0, 0, 12, 12), Color.FromArgb(0, 255, 0, 0), ButtonBorderStyle.Solid);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(14, 7).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
        Assert.Equal(clip, graphics.ClipBounds);
        Assert.Equal(5, graphics.Transform.OffsetX);
        Assert.Equal(7, graphics.Transform.OffsetY);

        graphics.ResetClip();
        graphics.ResetTransform();
        graphics.FillRectangle(Brushes.Blue, 0, 0, 1, 1);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [WinFormsTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ControlPaint_IDeviceContext_SelectionFrameLeavesInteriorAndExteriorUntouched(bool active)
    {
        using Bitmap bitmap = new(32, 32, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        ControlPaint.DrawSelectionFrame((IDeviceContext)graphics, active, new Rectangle(4, 4, 24, 24), new Rectangle(8, 8, 16, 16), Color.White);
        Assert.True(ContainsChangedPixel(bitmap, new Rectangle(4, 4, 24, 4)));
        Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(12, 12).ToArgb());
        Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
    }

    [WinFormsFact]
    public void ControlPaint_IDeviceContext_GridPreservesSpacingAndBackground()
    {
        using Bitmap bitmap = new(20, 20, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        ControlPaint.DrawGrid((IDeviceContext)graphics, new Rectangle(2, 2, 16, 16), new Size(3, 5), Color.White);
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                Color expected = x >= 2 && x < 18 && y >= 2 && y < 18 && x % 3 == 0 && y % 5 == 0
                    ? Color.Black
                    : Color.Magenta;
                Assert.Equal(expected.ToArgb(), bitmap.GetPixel(x, y).ToArgb());
            }
        }
    }

    [WinFormsFact]
    public void ControlPaint_IDeviceContext_DefaultBorder3DDoesNotFillInterior()
    {
        using Bitmap bitmap = new(32, 32, PixelFormat.Format32bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        ControlPaint.DrawBorder3D((IDeviceContext)graphics, new Rectangle(4, 4, 24, 24));
        Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(16, 16).ToArgb());
        Assert.True(ContainsChangedPixel(bitmap, new Rectangle(4, 4, 24, 2)));
    }

    public static IEnumerable<object[]> DeviceContextGraphicsPairs()
    {
        yield return Pair("Button", (g, r) => ControlPaint.DrawButton(g, r, ButtonState.Normal), (dc, r) => ControlPaint.DrawButton(dc, r, ButtonState.Normal));
        yield return Pair("Caption", (g, r) => ControlPaint.DrawCaptionButton(g, r, CaptionButton.Close, ButtonState.Normal), (dc, r) => ControlPaint.DrawCaptionButton(dc, r, CaptionButton.Close, ButtonState.Normal));
        yield return Pair("CheckBox", (g, r) => ControlPaint.DrawCheckBox(g, r, ButtonState.Checked), (dc, r) => ControlPaint.DrawCheckBox(dc, r, ButtonState.Checked));
        yield return Pair("FlatCheckBox", (g, r) => ControlPaint.DrawCheckBox(g, r, ButtonState.Flat | ButtonState.Checked), (dc, r) => ControlPaint.DrawCheckBox(dc, r, ButtonState.Flat | ButtonState.Checked));
        yield return Pair("Combo", (g, r) => ControlPaint.DrawComboButton(g, r, ButtonState.Normal), (dc, r) => ControlPaint.DrawComboButton(dc, r, ButtonState.Normal));
        yield return Pair("MenuColors", (g, r) => ControlPaint.DrawMenuGlyph(g, r, MenuGlyph.Arrow, Color.Blue, Color.Yellow), (dc, r) => ControlPaint.DrawMenuGlyph(dc, r, MenuGlyph.Arrow, Color.Blue, Color.Yellow));
        yield return Pair("MixedCheckBox", (g, r) => ControlPaint.DrawMixedCheckBox(g, r, ButtonState.Checked), (dc, r) => ControlPaint.DrawMixedCheckBox(dc, r, ButtonState.Checked));
        yield return Pair("Radio", (g, r) => ControlPaint.DrawRadioButton(g, r, ButtonState.Checked), (dc, r) => ControlPaint.DrawRadioButton(dc, r, ButtonState.Checked));
        yield return Pair("Container", ControlPaint.DrawContainerGrabHandle, ControlPaint.DrawContainerGrabHandle);
        yield return Pair("Grab", (g, r) => ControlPaint.DrawGrabHandle(g, r, true, true), (dc, r) => ControlPaint.DrawGrabHandle(dc, r, true, true));
        yield return Pair("Locked", (g, r) => ControlPaint.DrawLockedFrame(g, r, true), (dc, r) => ControlPaint.DrawLockedFrame(dc, r, true));
        yield return Pair("Inset", (g, r) => ControlPaint.DrawBorder(g, r, Color.Gray, ButtonBorderStyle.Inset), (dc, r) => ControlPaint.DrawBorder(dc, r, Color.Gray, ButtonBorderStyle.Inset));
        yield return Pair("Outset", (g, r) => ControlPaint.DrawBorder(g, r, SystemColors.Control, ButtonBorderStyle.Outset), (dc, r) => ControlPaint.DrawBorder(dc, r, SystemColors.Control, ButtonBorderStyle.Outset));
        yield return Pair("Focus", (g, r) => ControlPaint.DrawFocusRectangle(g, r, Color.Black, Color.White), (dc, r) => ControlPaint.DrawFocusRectangle(dc, r, Color.Black, Color.White));
        yield return Pair("FocusDark", (g, r) => ControlPaint.DrawFocusRectangle(g, r, Color.White, Color.Blue), (dc, r) => ControlPaint.DrawFocusRectangle(dc, r, Color.White, Color.Blue));
        yield return Pair("Selection", (g, r) => ControlPaint.DrawSelectionFrame(g, true, r, Rectangle.Inflate(r, -3, -3), Color.White), (dc, r) => ControlPaint.DrawSelectionFrame(dc, true, r, Rectangle.Inflate(r, -3, -3), Color.White));
        yield return Pair("InactiveSelection", (g, r) => ControlPaint.DrawSelectionFrame(g, false, r, Rectangle.Inflate(r, -3, -3), Color.White), (dc, r) => ControlPaint.DrawSelectionFrame(dc, false, r, Rectangle.Inflate(r, -3, -3), Color.White));
        yield return Pair("SizeGrip", (g, r) => ControlPaint.DrawSizeGrip(g, Color.White, r), (dc, r) => ControlPaint.DrawSizeGrip(dc, Color.White, r));
        yield return Pair("VisualStyle", ControlPaint.DrawVisualStyleBorder, ControlPaint.DrawVisualStyleBorder);

        static object[] Pair(string name, Action<Graphics, Rectangle> legacy, Action<IDeviceContext, Rectangle> gdi) => [name, legacy, gdi];
    }

    [WinFormsTheory]
    [MemberData(nameof(DeviceContextGraphicsPairs))]
    public void ControlPaint_IDeviceContext_MatchesOpaqueGraphicsRendering(
        string name,
        Action<Graphics, Rectangle> legacy,
        Action<IDeviceContext, Rectangle> gdi)
    {
        using Bitmap expected = new(40, 40, PixelFormat.Format32bppRgb);
        using Bitmap actual = new(40, 40, PixelFormat.Format32bppRgb);
        ReadOnlySpan<Point> locations = [new(5, 6), new(6, 6)];
        foreach (Point location in locations)
        {
            Rectangle bounds = new(location, new Size(24, 24));
            using (Graphics graphics = Graphics.FromImage(expected))
            {
                graphics.Clear(Color.Magenta);
                legacy(graphics, bounds);
            }

            using (Graphics graphics = Graphics.FromImage(actual))
            {
                graphics.Clear(Color.Magenta);
                gdi(graphics, bounds);
            }

            for (int y = 0; y < actual.Height; y++)
            {
                for (int x = 0; x < actual.Width; x++)
                {
                    Assert.True(
                        expected.GetPixel(x, y).ToArgb() == actual.GetPixel(x, y).ToArgb(),
                        $"{name} at {location}: pixel ({x}, {y}) expected {expected.GetPixel(x, y)}, actual {actual.GetPixel(x, y)}.");
                }
            }
        }
    }

    [WinFormsTheory]
    [InlineData(-1, 10, "width")]
    [InlineData(10, -1, "height")]
    public void ControlPaint_IDeviceContext_FrameControlsValidateSizeBeforeAcquiringHdc(int width, int height, string parameter)
    {
        using BorrowedDeviceContext dc = new(IntPtr.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => ControlPaint.DrawButton(dc, 0, 0, width, height, ButtonState.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => ControlPaint.DrawCaptionButton(dc, 0, 0, width, height, CaptionButton.Close, ButtonState.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => ControlPaint.DrawCheckBox(dc, 0, 0, width, height, ButtonState.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(parameter, () => ControlPaint.DrawComboButton(dc, 0, 0, width, height, ButtonState.Normal));
        Assert.Equal(0, dc.Acquisitions);
    }

    private static bool ContainsChangedPixel(Bitmap bitmap, Rectangle bounds, Color? background = null)
    {
        int original = (background ?? Color.Magenta).ToArgb();
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != original)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///  Exposes a borrowed HDC without providing a Graphics or taking ownership of the handle.
    /// </summary>
    private sealed class BorrowedDeviceContext(IntPtr hdc) : IDeviceContext
    {
        public int Acquisitions { get; private set; }
        public int Releases { get; private set; }

        public IntPtr GetHdc()
        {
            Acquisitions++;
            return hdc;
        }

        public void ReleaseHdc() => Releases++;
        public void Dispose() { }
    }
#nullable disable
}
