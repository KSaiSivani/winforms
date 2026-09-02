// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms;

public static unsafe partial class ControlPaint
{
    // Overloads that take an IDeviceContext always render with GDI, even when the given IDeviceContext is a
    // Graphics object. GDI has significantly less overhead than GDI+ and is what the framework itself uses for
    // the bulk of its rendering. As GDI has no notion of alpha blending, the alpha component of any given color
    // is ignored. When the IDeviceContext is a Graphics (or provides one, such as PaintEventArgs) the origin
    // transform and the clipping region are applied to the HDC via DeviceContextHdcScope.

    /// <summary>
    ///  Ternary raster operation that ANDs the currently selected brush pattern into the destination (DPa).
    /// </summary>
    private const ROP_CODE PatternAnd = (ROP_CODE)0x00A000C9;

    /// <summary>
    ///  Ternary raster operation that ORs the currently selected brush pattern into the destination (DPo).
    /// </summary>
    private const ROP_CODE PatternOr = (ROP_CODE)0x00FA0089;

    /// <summary>
    ///  The size of the monochrome patterns used to emulate GDI+ texture brushes.
    /// </summary>
    private const int PatternSize = 8;

    /// <summary>
    ///  Draws a border of the specified style and color on the given device context.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="bounds">The bounds of the border.</param>
    /// <param name="color">The color of the border. The alpha component is ignored.</param>
    /// <param name="style">The style of the border.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///  <para>
    ///   Unlike <see cref="DrawBorder(Graphics, Rectangle, Color, ButtonBorderStyle)"/> this always renders with
    ///   GDI, which does not support alpha blending.
    ///  </para>
    /// </remarks>
    public static void DrawBorder(IDeviceContext deviceContext, Rectangle bounds, Color color, ButtonBorderStyle style)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        switch (style)
        {
            case ButtonBorderStyle.None:
                // Nothing to draw.
                break;
            case ButtonBorderStyle.Dotted:
            case ButtonBorderStyle.Dashed:
            case ButtonBorderStyle.Solid:
                DrawBorderSimple(deviceContext, bounds, color, style);
                break;
            case ButtonBorderStyle.Inset:
            case ButtonBorderStyle.Outset:
                using (DeviceContextHdcScope hdc = deviceContext.ToHdcScope())
                {
                    DrawBorderComplex(hdc.HDC, bounds, color, style);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>
    ///  Draws a 3D style border at the given rectangle. The default 3D style of <see cref="Border3DStyle.Etched"/>
    ///  is used.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the border.</param>
    /// <param name="y">The y coordinate of the border.</param>
    /// <param name="width">The width of the border.</param>
    /// <param name="height">The height of the border.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawBorder3D(IDeviceContext deviceContext, int x, int y, int width, int height)
        => DrawBorder3D(
            deviceContext,
            x, y, width, height,
            Border3DStyle.Etched,
            Border3DSide.Left | Border3DSide.Top | Border3DSide.Right | Border3DSide.Bottom);

    /// <summary>
    ///  Draws a 3D style border at the given rectangle. You may specify the style of the 3D appearance.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the border.</param>
    /// <param name="y">The y coordinate of the border.</param>
    /// <param name="width">The width of the border.</param>
    /// <param name="height">The height of the border.</param>
    /// <param name="style">The 3D style of the border.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawBorder3D(IDeviceContext deviceContext, int x, int y, int width, int height, Border3DStyle style)
        => DrawBorder3D(
            deviceContext,
            x, y, width, height,
            style,
            Border3DSide.Left | Border3DSide.Top | Border3DSide.Right | Border3DSide.Bottom);

    /// <summary>
    ///  Draws a 3D style border at the given rectangle. You may specify the style of the 3D appearance, and which
    ///  sides of the 3D rectangle you wish to draw.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the border.</param>
    /// <param name="y">The y coordinate of the border.</param>
    /// <param name="width">The width of the border.</param>
    /// <param name="height">The height of the border.</param>
    /// <param name="style">The 3D style of the border.</param>
    /// <param name="sides">The sides of the rectangle to draw.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawBorder3D(
        IDeviceContext deviceContext,
        int x, int y, int width, int height,
        Border3DStyle style,
        Border3DSide sides)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        DRAWEDGE_FLAGS edge = (DRAWEDGE_FLAGS)((uint)style & 0x0F);
        DRAW_EDGE_FLAGS flags = (DRAW_EDGE_FLAGS)sides | (DRAW_EDGE_FLAGS)((uint)style & ~0x0F);

        RECT rectangle = new Rectangle(x, y, width, height);

        // Windows just draws the border to size, and then shrinks the rectangle so the user can paint the client
        // area. We can't really do that, so we do the opposite: We pre-calculate the size of the border and enlarge
        // the rectangle so the client size is preserved.
        if (flags.HasFlag((DRAW_EDGE_FLAGS)Border3DStyle.Adjust))
        {
            Size size = SystemInformation.Border3DSize;
            rectangle.left -= size.Width;
            rectangle.right += size.Width;
            rectangle.top -= size.Height;
            rectangle.bottom += size.Height;
            flags &= ~(DRAW_EDGE_FLAGS)Border3DStyle.Adjust;
        }

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        PInvoke.DrawEdge(hdc, ref rectangle, edge, flags);
    }

    /// <summary>
    ///  Draws a 3D style border at the given rectangle. The default 3D style of <see cref="Border3DStyle.Etched"/>
    ///  is used.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the border.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawBorder3D(IDeviceContext deviceContext, Rectangle rectangle)
        => DrawBorder3D(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    /// <summary>
    ///  Draws a 3D style border at the given rectangle. You may specify the style of the 3D appearance.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the border.</param>
    /// <param name="style">The 3D style of the border.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawBorder3D(IDeviceContext deviceContext, Rectangle rectangle, Border3DStyle style)
        => DrawBorder3D(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, style);

    /// <summary>
    ///  Draws a 3D style border at the given rectangle. You may specify the style of the 3D appearance, and which
    ///  sides of the 3D rectangle you wish to draw.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the border.</param>
    /// <param name="style">The 3D style of the border.</param>
    /// <param name="sides">The sides of the rectangle to draw.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawBorder3D(IDeviceContext deviceContext, Rectangle rectangle, Border3DStyle style, Border3DSide sides)
        => DrawBorder3D(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, style, sides);

    /// <summary>
    ///  Draws a Win32 button control in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the button.</param>
    /// <param name="y">The y coordinate of the button.</param>
    /// <param name="width">The width of the button.</param>
    /// <param name="height">The height of the button.</param>
    /// <param name="state">The state of the button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawButton(IDeviceContext deviceContext, int x, int y, int width, int height, ButtonState state)
        => DrawFrameControl(
            deviceContext,
            x, y, width, height,
            DFC_TYPE.DFC_BUTTON,
            DFCS_STATE.DFCS_BUTTONPUSH | (DFCS_STATE)state);

    /// <summary>
    ///  Draws a Win32 button control in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the button.</param>
    /// <param name="state">The state of the button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawButton(IDeviceContext deviceContext, Rectangle rectangle, ButtonState state)
        => DrawButton(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, state);

    /// <summary>
    ///  Draws a Win32 window caption button in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the caption button.</param>
    /// <param name="y">The y coordinate of the caption button.</param>
    /// <param name="width">The width of the caption button.</param>
    /// <param name="height">The height of the caption button.</param>
    /// <param name="button">The type of caption button to draw.</param>
    /// <param name="state">The state of the caption button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawCaptionButton(
        IDeviceContext deviceContext,
        int x, int y, int width, int height,
        CaptionButton button,
        ButtonState state)
        => DrawFrameControl(
            deviceContext,
            x, y, width, height,
            DFC_TYPE.DFC_CAPTION,
            (DFCS_STATE)button | (DFCS_STATE)state);

    /// <summary>
    ///  Draws a Win32 window caption button in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the caption button.</param>
    /// <param name="button">The type of caption button to draw.</param>
    /// <param name="state">The state of the caption button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawCaptionButton(
        IDeviceContext deviceContext,
        Rectangle rectangle,
        CaptionButton button,
        ButtonState state)
        => DrawCaptionButton(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, button, state);

    /// <summary>
    ///  Draws a Win32 checkbox control in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the check box.</param>
    /// <param name="y">The y coordinate of the check box.</param>
    /// <param name="width">The width of the check box.</param>
    /// <param name="height">The height of the check box.</param>
    /// <param name="state">The state of the check box.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawCheckBox(IDeviceContext deviceContext, int x, int y, int width, int height, ButtonState state)
    {
        // We overwrite the windows checkbox
        if ((state & ButtonState.Flat) == ButtonState.Flat)
        {
            ArgumentNullException.ThrowIfNull(deviceContext);
            DrawFlatCheckBox(deviceContext, new Rectangle(x, y, width, height), state);
        }
        else
        {
            DrawFrameControl(
                deviceContext,
                x, y, width, height,
                DFC_TYPE.DFC_BUTTON,
                DFCS_STATE.DFCS_BUTTONCHECK | (DFCS_STATE)state);
        }
    }

    /// <summary>
    ///  Draws a Win32 checkbox control in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the check box.</param>
    /// <param name="state">The state of the check box.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawCheckBox(IDeviceContext deviceContext, Rectangle rectangle, ButtonState state)
        => DrawCheckBox(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, state);

    /// <summary>
    ///  Draws the drop down button of a Win32 combo box in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the combo button.</param>
    /// <param name="y">The y coordinate of the combo button.</param>
    /// <param name="width">The width of the combo button.</param>
    /// <param name="height">The height of the combo button.</param>
    /// <param name="state">The state of the combo button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawComboButton(IDeviceContext deviceContext, int x, int y, int width, int height, ButtonState state)
        => DrawFrameControl(
            deviceContext,
            x, y, width, height,
            DFC_TYPE.DFC_SCROLL,
            DFCS_STATE.DFCS_SCROLLCOMBOBOX | (DFCS_STATE)state);

    /// <summary>
    ///  Draws the drop down button of a Win32 combo box in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the combo button.</param>
    /// <param name="state">The state of the combo button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawComboButton(IDeviceContext deviceContext, Rectangle rectangle, ButtonState state)
        => DrawComboButton(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, state);

    /// <summary>
    ///  Draws a container control grab handle glyph inside the given rectangle.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="bounds">The bounds of the grab handle.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawContainerGrabHandle(IDeviceContext deviceContext, Rectangle bounds)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();

        using (CreateBrushScope brush = new(Color.White))
        {
            hdc.FillRectangle(
                new Rectangle(bounds.Left + 1, bounds.Top + 1, bounds.Width - 2, bounds.Height - 2),
                brush);
        }

        int midX = bounds.X + (bounds.Width / 2);
        int midY = bounds.Y + (bounds.Height / 2);

        using CreatePenScope pen = new(Color.Black);

        // GDI does not draw the last point of a line, so one is added to every end point to match GDI+.
        hdc.DrawLines(pen,
        [
            // The bounding rect without the four corners.
            bounds.X + 1, bounds.Y, bounds.Right - 1, bounds.Y,
            bounds.X + 1, bounds.Bottom - 1, bounds.Right - 1, bounds.Bottom - 1,
            bounds.X, bounds.Y + 1, bounds.X, bounds.Bottom - 1,
            bounds.Right - 1, bounds.Y + 1, bounds.Right - 1, bounds.Bottom - 1,

            // Vertical and horizontal lines.
            midX, bounds.Y, midX, bounds.Bottom - 1,
            bounds.X, midY, bounds.Right - 1, midY,

            // Top hash.
            midX - 1, bounds.Y + 2, midX + 2, bounds.Y + 2,
            midX - 2, bounds.Y + 3, midX + 3, bounds.Y + 3,

            // Left hash.
            bounds.X + 2, midY - 1, bounds.X + 2, midY + 2,
            bounds.X + 3, midY - 2, bounds.X + 3, midY + 3,

            // Right hash.
            bounds.Right - 3, midY - 1, bounds.Right - 3, midY + 2,
            bounds.Right - 4, midY - 2, bounds.Right - 4, midY + 3,

            // Bottom hash.
            midX - 1, bounds.Bottom - 3, midX + 2, bounds.Bottom - 3,
            midX - 2, bounds.Bottom - 4, midX + 3, bounds.Bottom - 4
        ]);
    }

    /// <summary>
    ///  Draws a focus rectangle. A focus rectangle is a dotted rectangle that Windows uses to indicate what
    ///  control has the current keyboard focus.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the focus rectangle.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawFocusRectangle(IDeviceContext deviceContext, Rectangle rectangle)
        => DrawFocusRectangle(deviceContext, rectangle, SystemColors.ControlText, SystemColors.Control);

    /// <summary>
    ///  Draws a focus rectangle. A focus rectangle is a dotted rectangle that Windows uses to indicate what
    ///  control has the current keyboard focus.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the focus rectangle.</param>
    /// <param name="foreColor">This parameter is not used.</param>
    /// <param name="backColor">The background color the focus rectangle is drawn on.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawFocusRectangle(IDeviceContext deviceContext, Rectangle rectangle, Color foreColor, Color backColor)
        => DrawFocusRectangle(deviceContext, rectangle, backColor, highContrast: false);

    /// <summary>
    ///  Draws a standard selection grab handle with the given dimensions. Grab handles are used by components to
    ///  indicate to the user that they can be directly manipulated.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the grab handle.</param>
    /// <param name="primary"><see langword="true"/> to draw the primary grab handle.</param>
    /// <param name="enabled"><see langword="true"/> to draw the grab handle in the enabled state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawGrabHandle(IDeviceContext deviceContext, Rectangle rectangle, bool primary, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        Color fillColor = enabled
            ? primary ? Color.White : Color.Black
            : SystemColors.Control;

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();

        using (CreateBrushScope brush = new(fillColor))
        {
            hdc.FillRectangle(
                new Rectangle(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 1, rectangle.Height - 1),
                brush);
        }

        using CreatePenScope pen = new(primary ? Color.Black : Color.White);
        hdc.DrawRectangle(rectangle, pen);
    }

    /// <summary>
    ///  Draws a grid of one pixel dots in the given rectangle.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="area">The area to fill with the grid.</param>
    /// <param name="pixelsBetweenDots">The spacing between the dots.</param>
    /// <param name="backColor">
    ///  The background color the grid is drawn on. Determines whether black or white dots are used.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="pixelsBetweenDots"/> has a width or height that is not positive.
    /// </exception>
    public static void DrawGrid(IDeviceContext deviceContext, Rectangle area, Size pixelsBetweenDots, Color backColor)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);
        if (pixelsBetweenDots.Width <= 0 || pixelsBetweenDots.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsBetweenDots));
        }

        // Dark backgrounds get light dots and vice versa.
        bool invert = backColor.GetBrightness() < .5f;
        Color dotColor = invert ? Color.White : Color.Black;

        // Round the pattern size up to a multiple of the requested spacing so that the pattern tiles seamlessly.
        const int IdealSize = 16;
        int width = ((IdealSize / pixelsBetweenDots.Width) + 1) * pixelsBetweenDots.Width;
        int height = ((IdealSize / pixelsBetweenDots.Height) + 1) * pixelsBetweenDots.Height;
        int stride = GetPatternStride(width);

        using BufferScope<ushort> buffer = new(stackalloc ushort[32], stride * height);
        Span<ushort> bits = ((Span<ushort>)buffer)[..(stride * height)];
        bits.Fill(ushort.MaxValue);

        for (int y = 0; y < height; y += pixelsBetweenDots.Height)
        {
            for (int x = 0; x < width; x += pixelsBetweenDots.Width)
            {
                SetPatternForegroundPixel(bits, stride, x, y);
            }
        }

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        HBRUSH pattern = CreateMonochromePatternBrush(width, height, bits);
        using ObjectScope brush = new(pattern);
        FillTransparentPattern(hdc.HDC, pattern, dotColor, [area]);
    }

    /// <summary>
    ///  Draws a locked selection frame around the given rectangle.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the frame.</param>
    /// <param name="primary"><see langword="true"/> to draw the primary frame.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawLockedFrame(IDeviceContext deviceContext, Rectangle rectangle, bool primary)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();

        using (CreatePenScope pen = new(primary ? Color.White : Color.Black))
        {
            hdc.DrawRectangle(rectangle, pen);
            rectangle.Inflate(-1, -1);
            hdc.DrawRectangle(rectangle, pen);
        }

        rectangle.Inflate(-1, -1);
        using CreatePenScope innerPen = new(primary ? Color.Black : Color.White);
        hdc.DrawRectangle(rectangle, innerPen);
    }

    /// <summary>
    ///  Draws a menu glyph for a Win32 menu in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the glyph.</param>
    /// <param name="y">The y coordinate of the glyph.</param>
    /// <param name="width">The width of the glyph.</param>
    /// <param name="height">The height of the glyph.</param>
    /// <param name="glyph">The glyph to draw.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawMenuGlyph(IDeviceContext deviceContext, int x, int y, int width, int height, MenuGlyph glyph)
        => DrawFrameControl(deviceContext, x, y, width, height, DFC_TYPE.DFC_MENU, (DFCS_STATE)glyph);

    /// <summary>
    ///  Draws a menu glyph for a Win32 menu in the given rectangle with the given state. White is replaced with
    ///  <paramref name="backColor"/>, black is replaced with <paramref name="foreColor"/>.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the glyph.</param>
    /// <param name="y">The y coordinate of the glyph.</param>
    /// <param name="width">The width of the glyph.</param>
    /// <param name="height">The height of the glyph.</param>
    /// <param name="glyph">The glyph to draw.</param>
    /// <param name="foreColor">The color that replaces black. The alpha component is ignored.</param>
    /// <param name="backColor">The color that replaces white. The alpha component is ignored.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawMenuGlyph(
        IDeviceContext deviceContext,
        int x, int y, int width, int height,
        MenuGlyph glyph,
        Color foreColor,
        Color backColor)
        => DrawFrameControl(
            deviceContext,
            x, y, width, height,
            DFC_TYPE.DFC_MENU,
            (DFCS_STATE)glyph,
            foreColor,
            backColor);

    /// <summary>
    ///  Draws a menu glyph for a Win32 menu in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the glyph.</param>
    /// <param name="glyph">The glyph to draw.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawMenuGlyph(IDeviceContext deviceContext, Rectangle rectangle, MenuGlyph glyph)
        => DrawMenuGlyph(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, glyph);

    /// <summary>
    ///  Draws a menu glyph for a Win32 menu in the given rectangle with the given state. White is replaced with
    ///  <paramref name="backColor"/>, black is replaced with <paramref name="foreColor"/>.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the glyph.</param>
    /// <param name="glyph">The glyph to draw.</param>
    /// <param name="foreColor">The color that replaces black. The alpha component is ignored.</param>
    /// <param name="backColor">The color that replaces white. The alpha component is ignored.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawMenuGlyph(
        IDeviceContext deviceContext,
        Rectangle rectangle,
        MenuGlyph glyph,
        Color foreColor,
        Color backColor)
        => DrawMenuGlyph(
            deviceContext,
            rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
            glyph,
            foreColor,
            backColor);

    /// <summary>
    ///  Draws a Win32 three state checkbox control in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the check box.</param>
    /// <param name="y">The y coordinate of the check box.</param>
    /// <param name="width">The width of the check box.</param>
    /// <param name="height">The height of the check box.</param>
    /// <param name="state">The state of the check box.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawMixedCheckBox(IDeviceContext deviceContext, int x, int y, int width, int height, ButtonState state)
        => DrawFrameControl(
            deviceContext,
            x, y, width, height,
            DFC_TYPE.DFC_BUTTON,
            DFCS_STATE.DFCS_BUTTON3STATE | (DFCS_STATE)state);

    /// <summary>
    ///  Draws a Win32 three state checkbox control in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the check box.</param>
    /// <param name="state">The state of the check box.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawMixedCheckBox(IDeviceContext deviceContext, Rectangle rectangle, ButtonState state)
        => DrawMixedCheckBox(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, state);

    /// <summary>
    ///  Draws a Win32 radio button in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the radio button.</param>
    /// <param name="y">The y coordinate of the radio button.</param>
    /// <param name="width">The width of the radio button.</param>
    /// <param name="height">The height of the radio button.</param>
    /// <param name="state">The state of the radio button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative.
    /// </exception>
    public static void DrawRadioButton(IDeviceContext deviceContext, int x, int y, int width, int height, ButtonState state)
        => DrawFrameControl(
            deviceContext,
            x, y, width, height,
            DFC_TYPE.DFC_BUTTON,
            DFCS_STATE.DFCS_BUTTONRADIO | (DFCS_STATE)state);

    /// <summary>
    ///  Draws a Win32 radio button in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the radio button.</param>
    /// <param name="state">The state of the radio button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawRadioButton(IDeviceContext deviceContext, Rectangle rectangle, ButtonState state)
        => DrawRadioButton(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, state);

    /// <summary>
    ///  Draws a button for a Win32 scroll bar in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="x">The x coordinate of the scroll button.</param>
    /// <param name="y">The y coordinate of the scroll button.</param>
    /// <param name="width">The width of the scroll button.</param>
    /// <param name="height">The height of the scroll button.</param>
    /// <param name="button">The type of scroll button to draw.</param>
    /// <param name="state">The state of the scroll button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="width"/> or <paramref name="height"/> is negative, or <paramref name="button"/> is not a
    ///  valid <see cref="ScrollButton"/> when dark mode is enabled.
    /// </exception>
    public static void DrawScrollButton(
        IDeviceContext deviceContext,
        int x, int y, int width, int height,
        ScrollButton button,
        ButtonState state)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        if (width == 0 || height == 0)
        {
            throw new ArgumentException(message: null);
        }

        // If dark mode is enabled, use the new modern rendering
        if (Application.IsDarkModeEnabled)
        {
            ModernControlButtonState controlButtonState = state switch
            {
                ButtonState.Pushed => ModernControlButtonState.Pressed,
                ButtonState.Inactive => ModernControlButtonState.Disabled,
                _ => ModernControlButtonState.Normal
            };

            ModernControlButtonStyle modernControlButton = button switch
            {
                ScrollButton.Up => ModernControlButtonStyle.Up,
                ScrollButton.Down => ModernControlButtonStyle.Down,
                ScrollButton.Left => ModernControlButtonStyle.Left,
                ScrollButton.Right => ModernControlButtonStyle.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
            };

            DrawModernControlButton(
                deviceContext,
                new Rectangle(x, y, width, height),
                modernControlButton,
                controlButtonState,
                isDarkMode: true);
        }
        else
        {
            // Fall back to classic Windows rendering
            DrawFrameControl(
                deviceContext,
                x, y, width, height,
                DFC_TYPE.DFC_SCROLL,
                (DFCS_STATE)button | (DFCS_STATE)state);
        }
    }

    /// <summary>
    ///  Draws a button for a Win32 scroll bar in the given rectangle with the given state.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="rectangle">The bounds of the scroll button.</param>
    /// <param name="button">The type of scroll button to draw.</param>
    /// <param name="state">The state of the scroll button.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawScrollButton(
        IDeviceContext deviceContext,
        Rectangle rectangle,
        ScrollButton button,
        ButtonState state)
        => DrawScrollButton(deviceContext, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, button, state);

    /// <summary>
    ///  Draws a standard selection frame. A selection frame is a frame that is drawn around a selected component
    ///  at design time.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="active"><see langword="true"/> to draw the active frame.</param>
    /// <param name="outsideRect">The outside bounds of the frame.</param>
    /// <param name="insideRect">The bounds that are excluded from the frame.</param>
    /// <param name="backColor">
    ///  The background color the frame is drawn on. Determines the color of the frame pattern.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawSelectionFrame(
        IDeviceContext deviceContext,
        bool active,
        Rectangle outsideRect,
        Rectangle insideRect,
        Color backColor)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        Color frameColor = backColor.GetBrightness() <= .5 ? SystemColors.ControlLight : SystemColors.ControlDark;

        Span<ushort> bits = stackalloc ushort[PatternSize];
        bits.Fill(ushort.MaxValue);

        if (active)
        {
            // Diagonal lines with a period of four pixels.
            for (int y = 0; y < PatternSize; y++)
            {
                for (int x = -y; x < PatternSize; x += 4)
                {
                    if (x >= 0)
                    {
                        SetPatternForegroundPixel(bits, stride: 1, x, y);
                    }
                }
            }
        }
        else
        {
            // Every other pixel of every other column.
            int start = 0;
            for (int x = 0; x < PatternSize; x += 2)
            {
                for (int y = start; y < PatternSize; y += 2)
                {
                    SetPatternForegroundPixel(bits, stride: 1, x, y);
                }

                start ^= 1;
            }
        }

        // GDI has no equivalent to Graphics.ExcludeClip, fill the area between the two rectangles instead.
        Span<Rectangle> frame = stackalloc Rectangle[4];
        frame = frame[..SubtractRectangle(outsideRect, insideRect, frame)];

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        HBRUSH pattern = CreateMonochromePatternBrush(PatternSize, PatternSize, bits);
        using ObjectScope brush = new(pattern);
        FillTransparentPattern(hdc.HDC, pattern, frameColor, frame);
    }

    /// <summary>
    ///  Draws the border used by controls that render with visual styles.
    /// </summary>
    /// <param name="deviceContext">The device context to draw on.</param>
    /// <param name="bounds">The bounds of the border.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceContext"/> is <see langword="null"/>.</exception>
    public static void DrawVisualStyleBorder(IDeviceContext deviceContext, Rectangle bounds)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        using CreatePenScope pen = new(VisualStyles.VisualStyleInformation.TextControlBorder);

        // GDI+ includes the right and bottom edges when drawing a rectangle, GDI does not.
        hdc.DrawRectangle(bounds.Left, bounds.Top, bounds.Right + 1, bounds.Bottom + 1, pen);
    }

    /// <summary>
    ///  Helper function that draws a more complex border. This is used by
    ///  <see cref="DrawBorder(IDeviceContext, Rectangle, Color, ButtonBorderStyle)"/> for less common rendering
    ///  cases.
    /// </summary>
    private static void DrawBorderComplex(HDC hdc, Rectangle bounds, Color color, ButtonBorderStyle style)
    {
        // GDI does not draw the last point of a line, so one is added to every end point to match GDI+.
        int right = bounds.X + bounds.Width;
        int bottom = bounds.Y + bounds.Height;

        if (style == ButtonBorderStyle.Inset)
        {
            // Button being pushed
            HLSColor hls = new(color);

            // Top + left
            using (CreatePenScope darkPen = new(hls.Darker(1.0f)))
            {
                hdc.DrawLines(darkPen,
                [
                    bounds.X, bounds.Y, right, bounds.Y,
                    bounds.X, bounds.Y, bounds.X, bottom
                ]);
            }

            // Bottom + right
            using (CreatePenScope lightPen = new(hls.Lighter(1.0f)))
            {
                hdc.DrawLines(lightPen,
                [
                    bounds.X, bottom - 1, right, bottom - 1,
                    right - 1, bounds.Y, right - 1, bottom
                ]);
            }

            // Top + left inset
            using (CreatePenScope mediumPen = new(hls.Lighter(0.5f)))
            {
                hdc.DrawLines(mediumPen,
                [
                    bounds.X + 1, bounds.Y + 1, right - 1, bounds.Y + 1,
                    bounds.X + 1, bounds.Y + 1, bounds.X + 1, bottom - 1
                ]);
            }

            // Bottom + right inset
            if (color.ToKnownColor() == SystemColors.Control.ToKnownColor())
            {
                using CreatePenScope pen = new(SystemColors.ControlLight);
                hdc.DrawLines(pen,
                [
                    bounds.X + 1, bottom - 2, right - 1, bottom - 2,
                    right - 2, bounds.Y + 1, right - 2, bottom - 1
                ]);
            }
        }
        else
        {
            // Standard button
            Debug.Assert(style == ButtonBorderStyle.Outset, "Caller should have known how to use us.");

            bool stockColor = color.ToKnownColor() == SystemColors.Control.ToKnownColor();
            HLSColor hls = new(color);

            // Top + left
            using (CreatePenScope lightPen = new(stockColor ? SystemColors.ControlLightLight : hls.Lighter(1.0f)))
            {
                hdc.DrawLines(lightPen,
                [
                    bounds.X, bounds.Y, right, bounds.Y,
                    bounds.X, bounds.Y, bounds.X, bottom
                ]);
            }

            // Bottom + right
            using (CreatePenScope darkPen = new(stockColor ? SystemColors.ControlDarkDark : hls.Darker(1.0f)))
            {
                hdc.DrawLines(darkPen,
                [
                    bounds.X, bottom - 1, right, bottom - 1,
                    right - 1, bounds.Y, right - 1, bottom
                ]);
            }

            // Top + left inset
            using (CreatePenScope topLeftPen = new(!stockColor
                ? color
                : SystemInformation.HighContrast
                    ? SystemColors.ControlLightLight
                    : SystemColors.Control))
            {
                hdc.DrawLines(topLeftPen,
                [
                    bounds.X + 1, bounds.Y + 1, right - 1, bounds.Y + 1,
                    bounds.X + 1, bounds.Y + 1, bounds.X + 1, bottom - 1
                ]);
            }

            // Bottom + right inset
            using CreatePenScope bottomRightPen = new(stockColor ? SystemColors.ControlDark : hls.Darker(0.5f));
            hdc.DrawLines(bottomRightPen,
            [
                bounds.X + 1, bottom - 2, right - 1, bottom - 2,
                right - 2, bounds.Y + 1, right - 2, bottom - 1
            ]);
        }
    }

    /// <summary>
    ///  Draws a Win32 frame control on the given device context.
    /// </summary>
    private static void DrawFrameControl(
        IDeviceContext deviceContext,
        int x, int y, int width, int height,
        DFC_TYPE kind,
        DFCS_STATE state)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        if (width == 0 || height == 0)
        {
            throw new ArgumentException(message: null);
        }

        RECT bounds = new Rectangle(x, y, width, height);
        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        PInvoke.DrawFrameControl(hdc, ref bounds, (uint)kind, (uint)state);
    }

    /// <summary>
    ///  Draws a Win32 frame control on the given device context, replacing black with
    ///  <paramref name="foreColor"/> and white with <paramref name="backColor"/>.
    /// </summary>
    private static void DrawFrameControl(
        IDeviceContext deviceContext,
        int x, int y, int width, int height,
        DFC_TYPE kind,
        DFCS_STATE state,
        Color foreColor,
        Color backColor)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        if (width == 0 || height == 0)
        {
            throw new ArgumentException(message: null);
        }

        if (foreColor.IsEmpty || backColor.IsEmpty)
        {
            DrawFrameControl(deviceContext, x, y, width, height, kind, state);
            return;
        }

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        using CreateDcScope glyphDc = new(hdc);
        using CreateBitmapScope glyph = new(width, height, nPlanes: 1, nBitCount: 1, lpvBits: null);
        using SelectObjectScope glyphSelection = new(glyphDc, glyph);

        RenderFrameControlGlyph(glyphDc, width, height, kind, state);

        // Monochrome bitmaps are converted using the destination text color for the black (zero) bits and the
        // background color for the white (one) bits, which gives the same result as the GDI+ color remap table.
        using SetTextColorScope textColor = new(hdc, foreColor);
        using SetBackgroundColorScope backgroundColor = new(hdc, backColor);
        PInvokeCore.BitBlt(hdc, x, y, width, height, glyphDc, 0, 0, ROP_CODE.SRCCOPY);
    }

    /// <summary>
    ///  Draws a Win32 frame control in <paramref name="color"/>, leaving the pixels the frame control did not
    ///  render on untouched.
    /// </summary>
    private static void DrawFrameControlMasked(
        HDC hdc,
        Rectangle bounds,
        DFC_TYPE kind,
        DFCS_STATE state,
        Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using CreateDcScope glyphDc = new(hdc);
        using CreateBitmapScope glyph = new(bounds.Width, bounds.Height, nPlanes: 1, nBitCount: 1, lpvBits: null);
        using SelectObjectScope glyphSelection = new(glyphDc, glyph);

        RenderFrameControlGlyph(glyphDc, bounds.Width, bounds.Height, kind, state);
        BlendMask(hdc, bounds, glyphDc, color);
    }

    /// <summary>
    ///  Renders the given frame control into the monochrome bitmap selected into <paramref name="hdc"/>. The
    ///  glyph is represented by zero (black) bits, the background by one (white) bits.
    /// </summary>
    private static void RenderFrameControlGlyph(HDC hdc, int width, int height, DFC_TYPE kind, DFCS_STATE state)
    {
        // Newly created bitmaps are not initialized, start out with every bit set.
        PInvoke.PatBlt(hdc, 0, 0, width, height, ROP_CODE.WHITENESS);

        RECT bounds = new(0, 0, width, height);
        PInvoke.DrawFrameControl(hdc, ref bounds, (uint)kind, (uint)state);
    }

    /// <summary>
    ///  Draws a flat checkbox. This gives a better looking render than what Windows provides.
    /// </summary>
    private static void DrawFlatCheckBox(IDeviceContext deviceContext, Rectangle rectangle, ButtonState state)
    {
        if (rectangle.Width < 0 || rectangle.Height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rectangle));
        }

        if (rectangle.Width == 0 || rectangle.Height == 0)
        {
            throw new ArgumentException(message: null);
        }

        bool inactive = (state & ButtonState.Inactive) == ButtonState.Inactive;

        // Background color of checkbox
        Color background = inactive ? SystemColors.Control : SystemColors.Window;
        Color foreground = inactive
            ? SystemInformation.HighContrast ? SystemColors.GrayText : SystemColors.ControlDark
            : SystemColors.ControlText;

        Rectangle offsetRectangle = new(
            rectangle.X + 1,
            rectangle.Y + 1,
            rectangle.Width - 2,
            rectangle.Height - 2);

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();

        using (CreateBrushScope brush = new(background))
        {
            hdc.FillRectangle(offsetRectangle, brush);
        }

        if ((state & ButtonState.Checked) == ButtonState.Checked)
        {
            // The checkmark is drawn slightly off center to eliminate 3-D border artifacts.
            DrawFrameControlMasked(
                hdc.HDC,
                new Rectangle(rectangle.X + 1, rectangle.Y, rectangle.Width, rectangle.Height),
                DFC_TYPE.DFC_MENU,
                DFCS_STATE.DFCS_MENUCHECK,
                foreground);
        }

        // Surrounding border. We inset this by one pixel so we match how the 3D checkbox is drawn.
        using CreatePenScope pen = new(SystemColors.ControlDark);
        hdc.DrawRectangle(offsetRectangle, pen);
    }

    /// <summary>
    ///  Draws a focus rectangle with GDI.
    /// </summary>
    private static void DrawFocusRectangle(
        IDeviceContext deviceContext,
        Rectangle rectangle,
        Color color,
        bool highContrast,
        bool blackAndWhite = false)
    {
        ArgumentNullException.ThrowIfNull(deviceContext);

        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        (Color background, Color foreground) = GetFocusRectangleColors(color, highContrast, blackAndWhite);

        // The pattern is aligned so that the corners of the rectangle always get a dot, matching the pen based
        // rendering of the Graphics overloads.
        bool evenOrigin = (rectangle.X + rectangle.Y) % 2 == 0;

        Span<ushort> bits = stackalloc ushort[PatternSize];
        bits.Fill(ushort.MaxValue);
        for (int y = 0; y < PatternSize; y++)
        {
            for (int x = (y + (evenOrigin ? 0 : 1)) % 2; x < PatternSize; x += 2)
            {
                SetPatternForegroundPixel(bits, stride: 1, x, y);
            }
        }

        Span<Rectangle> edges = stackalloc Rectangle[4];
        int count = 0;
        edges[count++] = new(rectangle.X, rectangle.Y, rectangle.Width, 1);

        if (rectangle.Height > 1)
        {
            edges[count++] = new(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1);
        }

        if (rectangle.Height > 2)
        {
            edges[count++] = new(rectangle.X, rectangle.Y + 1, 1, rectangle.Height - 2);

            if (rectangle.Width > 1)
            {
                edges[count++] = new(rectangle.Right - 1, rectangle.Y + 1, 1, rectangle.Height - 2);
            }
        }

        edges = edges[..count];

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();
        HBRUSH pattern = CreateMonochromePatternBrush(PatternSize, PatternSize, bits);
        using ObjectScope brush = new(pattern);

        if (background.IsFullyTransparent())
        {
            FillTransparentPattern(hdc.HDC, pattern, foreground, edges);
            return;
        }

        using SelectObjectScope brushSelection = new(hdc, pattern);
        using SetTextColorScope textColor = new(hdc, foreground);
        using SetBackgroundColorScope backgroundColor = new(hdc, background);

        foreach (Rectangle edge in edges)
        {
            PInvoke.PatBlt(hdc, edge.X, edge.Y, edge.Width, edge.Height, ROP_CODE.PATCOPY);
        }
    }

    /// <summary>
    ///  Gets the two colors a focus rectangle alternates between.
    /// </summary>
    /// <returns>
    ///  The background color, which is <see cref="Color.Transparent"/> when the existing background should show
    ///  through, and the color of the focus dots.
    /// </returns>
    private static (Color Background, Color Foreground) GetFocusRectangleColors(
        Color baseColor,
        bool highContrast,
        bool blackAndWhite)
    {
        Color background = Color.Transparent;
        Color foreground;

        if (highContrast)
        {
            // In high contrast mode "baseColor" itself is used as the focus color.
            foreground = baseColor;
        }
        else if (blackAndWhite)
        {
            background = Color.White;
            foreground = Color.Black;
        }
        else
        {
            // In non-high contrast mode "baseColor" is used to calculate the focus colors. In this mode
            // "baseColor" is expected to contain the background color of the control to do this calculation
            // properly.
            foreground = Color.Black;

            if (IsDark(baseColor))
            {
                background = foreground;
                foreground = baseColor.InvertColor();
            }
            else if (baseColor == Color.Transparent)
            {
                background = Color.White;
            }
        }

        return (background, foreground);
    }

    /// <summary>
    ///  Draws a modern control button with GDI. Only the styles used by
    ///  <see cref="DrawScrollButton(IDeviceContext, int, int, int, int, ScrollButton, ButtonState)"/> are supported.
    /// </summary>
    private static void DrawModernControlButton(
        IDeviceContext deviceContext,
        Rectangle bounds,
        ModernControlButtonStyle button,
        ModernControlButtonState state,
        bool isDarkMode)
    {
        (Color backgroundColor, _, Color arrowColor) = GetModernControlButtonColors(state, isDarkMode);

        using DeviceContextHdcScope hdc = deviceContext.ToHdcScope();

        using (CreateBrushScope backgroundBrush = new(backgroundColor))
        {
            hdc.FillRectangle(bounds, backgroundBrush);
        }

        int centerX = bounds.X + (bounds.Width / 2);
        int centerY = bounds.Y + (bounds.Height / 2);

        // Apply pressed offset
        if (state == ModernControlButtonState.Pressed)
        {
            centerX++;
            centerY++;
        }

        int size = ScaleSymbolSize(bounds);
        int half = size / 2;

        ReadOnlySpan<Point> points = button switch
        {
            ModernControlButtonStyle.Up =>
            [
                new(centerX, centerY - half),
                new(centerX - half, centerY + half),
                new(centerX + half, centerY + half)
            ],
            ModernControlButtonStyle.Down =>
            [
                new(centerX, centerY + half),
                new(centerX - half, centerY - half),
                new(centerX + half, centerY - half)
            ],
            ModernControlButtonStyle.Left =>
            [
                new(centerX - half, centerY),
                new(centerX + half, centerY - half),
                new(centerX + half, centerY + half)
            ],
            ModernControlButtonStyle.Right =>
            [
                new(centerX + half, centerY),
                new(centerX - half, centerY - half),
                new(centerX - half, centerY + half)
            ],
            _ => default
        };

        if (!points.IsEmpty)
        {
            FillPolygon(hdc.HDC, points, arrowColor);
        }
    }

    /// <summary>
    ///  Fills the given polygon with <paramref name="color"/>, without drawing an outline.
    /// </summary>
    private static void FillPolygon(HDC hdc, ReadOnlySpan<Point> points, Color color)
    {
        using CreateBrushScope brush = new(color);
        using SelectObjectScope brushSelection = new(hdc, brush);
        using SelectObjectScope penSelection = new(hdc, PInvokeCore.GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_PEN));

        fixed (Point* p = points)
        {
            PInvoke.Polygon(hdc, p, points.Length);
        }
    }

    /// <summary>
    ///  Draws the given monochrome mask in <paramref name="color"/>, leaving the pixels that map to the white
    ///  (one) bits of the mask untouched.
    /// </summary>
    private static void BlendMask(HDC hdc, Rectangle bounds, HDC mask, Color color)
    {
        // Two passes are needed to leave the background untouched. The first pass ANDs black into every pixel
        // the mask covers, the second ORs the requested color into those same pixels.
        using (SetTextColorScope textColor = new(hdc, (COLORREF)0x00000000))
        using (SetBackgroundColorScope backgroundColor = new(hdc, (COLORREF)0x00FFFFFF))
        {
            PInvokeCore.BitBlt(hdc, bounds.X, bounds.Y, bounds.Width, bounds.Height, mask, 0, 0, ROP_CODE.SRCAND);
        }

        using (SetTextColorScope textColor = new(hdc, color))
        using (SetBackgroundColorScope backgroundColor = new(hdc, (COLORREF)0x00000000))
        {
            PInvokeCore.BitBlt(hdc, bounds.X, bounds.Y, bounds.Width, bounds.Height, mask, 0, 0, ROP_CODE.SRCPAINT);
        }
    }

    /// <summary>
    ///  Fills the given rectangles with the currently given monochrome pattern brush, drawing the black (zero)
    ///  bits in <paramref name="color"/> and leaving the pixels that map to white (one) bits untouched.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This gives the same result as filling with a GDI+ texture brush that has a transparent background.
    ///  </para>
    /// </remarks>
    private static void FillTransparentPattern(HDC hdc, HBRUSH pattern, Color color, ReadOnlySpan<Rectangle> rectangles)
    {
        if (rectangles.IsEmpty)
        {
            return;
        }

        using SelectObjectScope brushSelection = new(hdc, pattern);

        using (SetTextColorScope textColor = new(hdc, (COLORREF)0x00000000))
        using (SetBackgroundColorScope backgroundColor = new(hdc, (COLORREF)0x00FFFFFF))
        {
            foreach (Rectangle rectangle in rectangles)
            {
                PInvoke.PatBlt(hdc, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, PatternAnd);
            }
        }

        using (SetTextColorScope textColor = new(hdc, color))
        using (SetBackgroundColorScope backgroundColor = new(hdc, (COLORREF)0x00000000))
        {
            foreach (Rectangle rectangle in rectangles)
            {
                PInvoke.PatBlt(hdc, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, PatternOr);
            }
        }
    }

    /// <summary>
    ///  Number of <see cref="ushort"/> values in a scan line of a monochrome bitmap of the given width.
    /// </summary>
    private static int GetPatternStride(int width) => (width + 15) / 16;

    /// <summary>
    ///  Clears the bit for the given pixel, which makes it render with the device context text color.
    /// </summary>
    /// <param name="bits">The bitmap bits, <paramref name="stride"/> values per scan line.</param>
    /// <param name="stride">The number of <see cref="ushort"/> values in a scan line.</param>
    /// <param name="x">The x coordinate of the pixel.</param>
    /// <param name="y">The y coordinate of the pixel.</param>
    private static void SetPatternForegroundPixel(Span<ushort> bits, int stride, int x, int y)
    {
        // Scan lines are word aligned and the most significant bit of every byte is the leftmost pixel.
        int index = (y * stride) + (x / 16);
        int shift = (((x % 16) / 8) * 8) + (7 - (x % 8));
        bits[index] &= (ushort)~(1 << shift);
    }

    /// <summary>
    ///  Creates a monochrome pattern brush from the given bits.
    /// </summary>
    private static HBRUSH CreateMonochromePatternBrush(int width, int height, ReadOnlySpan<ushort> bits)
    {
        fixed (ushort* b = bits)
        {
            using CreateBitmapScope bitmap = new(width, height, nPlanes: 1, nBitCount: 1, b);
            return PInvoke.CreatePatternBrush(bitmap);
        }
    }

    /// <summary>
    ///  Splits the area of <paramref name="outer"/> that is not covered by <paramref name="inner"/> into up to
    ///  four rectangles.
    /// </summary>
    /// <returns>The number of rectangles written to <paramref name="results"/>.</returns>
    private static int SubtractRectangle(Rectangle outer, Rectangle inner, Span<Rectangle> results)
    {
        inner = Rectangle.Intersect(outer, inner);

        if (inner.Width <= 0 || inner.Height <= 0)
        {
            results[0] = outer;
            return 1;
        }

        int count = 0;

        if (inner.Top > outer.Top)
        {
            results[count++] = new(outer.X, outer.Y, outer.Width, inner.Top - outer.Top);
        }

        if (inner.Bottom < outer.Bottom)
        {
            results[count++] = new(outer.X, inner.Bottom, outer.Width, outer.Bottom - inner.Bottom);
        }

        if (inner.Left > outer.Left)
        {
            results[count++] = new(outer.X, inner.Y, inner.Left - outer.Left, inner.Height);
        }

        if (inner.Right < outer.Right)
        {
            results[count++] = new(inner.Right, inner.Y, outer.Right - inner.Right, inner.Height);
        }

        return count;
    }
}
