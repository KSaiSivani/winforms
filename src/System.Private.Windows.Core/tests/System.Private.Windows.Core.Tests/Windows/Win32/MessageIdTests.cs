// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Windows.Win32.Tests;

public class MessageIdTests
{
    [Theory]
    [InlineData(PInvokeCore.WM_SYNCPAINT, "WM_SYNCPAINT")]
    [InlineData(PInvokeCore.WM_NCXBUTTONDOWN, "WM_NCXBUTTONDOWN")]
    [InlineData(PInvokeCore.WM_NCXBUTTONUP, "WM_NCXBUTTONUP")]
    [InlineData(PInvokeCore.WM_NCXBUTTONDBLCLK, "WM_NCXBUTTONDBLCLK")]
    [InlineData(PInvokeCore.WM_IME_REQUEST, "WM_IME_REQUEST")]
    [InlineData(PInvokeCore.WM_INPUT, "WM_INPUT")]
    [InlineData(PInvokeCore.WM_INPUT_DEVICE_CHANGE, "WM_INPUT_DEVICE_CHANGE")]
    [InlineData(PInvokeCore.WM_TOUCH, "WM_TOUCH")]
    [InlineData(PInvokeCore.WM_TOUCHHITTESTING, "WM_TOUCHHITTESTING")]
    [InlineData(PInvokeCore.WM_POINTERUPDATE, "WM_POINTERUPDATE")]
    [InlineData(PInvokeCore.WM_POINTERDOWN, "WM_POINTERDOWN")]
    [InlineData(PInvokeCore.WM_POINTERUP, "WM_POINTERUP")]
    [InlineData(PInvokeCore.WM_THEMECHANGED, "WM_THEMECHANGED")]
    [InlineData(PInvokeCore.WM_CLIPBOARDUPDATE, "WM_CLIPBOARDUPDATE")]
    [InlineData(PInvokeCore.WM_DWMCOMPOSITIONCHANGED, "WM_DWMCOMPOSITIONCHANGED")]
    [InlineData(PInvokeCore.WM_GETTITLEBARINFOEX, "WM_GETTITLEBARINFOEX")]
    [InlineData(PInvokeCore.WM_DPICHANGED, "WM_DPICHANGED")]
    [InlineData(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, "WM_DPICHANGED_BEFOREPARENT")]
    [InlineData(PInvokeCore.WM_DPICHANGED_AFTERPARENT, "WM_DPICHANGED_AFTERPARENT")]
    [InlineData(PInvokeCore.WM_GETDPISCALEDSIZE, "WM_GETDPISCALEDSIZE")]
    [InlineData(PInvokeCore.WM_WTSSESSION_CHANGE, "WM_WTSSESSION_CHANGE")]
    [InlineData(PInvokeCore.WM_APPCOMMAND, "WM_APPCOMMAND")]
    [InlineData(PInvokeCore.WM_DDE_INITIATE, "WM_DDE_INITIATE")]
    [InlineData(PInvokeCore.WM_DDE_TERMINATE, "WM_DDE_TERMINATE")]
    [InlineData(PInvokeCore.WM_DDE_ADVISE, "WM_DDE_ADVISE")]
    [InlineData(PInvokeCore.WM_DDE_UNADVISE, "WM_DDE_UNADVISE")]
    [InlineData(PInvokeCore.WM_DDE_ACK, "WM_DDE_ACK")]
    [InlineData(PInvokeCore.WM_DDE_DATA, "WM_DDE_DATA")]
    [InlineData(PInvokeCore.WM_DDE_REQUEST, "WM_DDE_REQUEST")]
    [InlineData(PInvokeCore.WM_DDE_POKE, "WM_DDE_POKE")]
    [InlineData(PInvokeCore.WM_DDE_EXECUTE, "WM_DDE_EXECUTE")]
    // Regression: WM_HOTKEY was previously duplicated in the switch (once between WM_TIMER and
    // WM_HSCROLL, and again in its correct position after WM_PALETTECHANGED), which caused a
    // "pattern is unreachable" compile error (CS8510).
    [InlineData(PInvokeCore.WM_HOTKEY, "WM_HOTKEY")]
    // Regression: WM_KEYLAST and WM_UNICHAR share the same underlying value (265); only
    // WM_KEYLAST is mapped, consistent with how other same-valued aliases are handled elsewhere
    // in this switch.
    [InlineData(PInvokeCore.WM_KEYLAST, "WM_KEYLAST")]
    public void ToString_ReturnsExpectedName(uint id, string expected) =>
        ((MessageId)id).ToString().Should().Be(expected);

    [Fact]
    public void ToString_UnknownId_ReturnsHexFallback()
    {
        const uint UnknownMessageId = 0x0000_1234;
        ((MessageId)UnknownMessageId).ToString().Should().Be($"Id: {UnknownMessageId} (0x{UnknownMessageId:X8})");
    }
}
