// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Windows.Win32;
using Windows.Win32.Foundation;

namespace System.Windows.Forms.Design.Tests;

/// <summary>
///  Owns and completes native dialogs opened by design-time editors on the test thread.
/// </summary>
internal sealed class EditorDialogTestHost : DialogHostForm
{
    private readonly Func<CommonDialog?>? _getDialog;
    private readonly Action<HWND>? _onDialogIdle;
    private readonly HashSet<CommonDialog> _dialogs = [];
    private readonly bool _accept;
    private readonly HashSet<HWND> _completedDialogs = [];

    public EditorDialogTestHost(
        Func<CommonDialog?>? getDialog = null,
        bool accept = false,
        Action<HWND>? onDialogIdle = null)
    {
        _getDialog = getDialog;
        _accept = accept;
        _onDialogIdle = onDialogIdle;
        Application.EnterThreadModal += OnEnterThreadModal;
        Show();
        Activate();
    }

    public int DialogCount { get; private set; }

    private void OnEnterThreadModal(object? sender, EventArgs e)
    {
        if (_getDialog?.Invoke() is not { } dialog)
        {
            return;
        }

        _dialogs.Add(dialog);
        if (dialog is FileDialog fileDialog)
        {
            // Legacy file dialogs notify their owner through WM_ENTERIDLE.
            fileDialog.AutoUpgradeEnabled = false;
            fileDialog.RestoreDirectory = true;
        }
    }

    protected override void OnDialogIdle(HWND dialogHandle)
    {
        DialogCount++;
        _onDialogIdle?.Invoke(dialogHandle);
        if (_accept)
        {
            if (!_completedDialogs.Add(dialogHandle))
            {
                return;
            }

            if (_getDialog?.Invoke() is FileDialog fileDialog)
            {
                // CDM_SETCONTROLTEXT updates the native filename edit after shell initialization.
                const uint SetControlText = 0x0468;
                const int FileNameControl = 0x0480;
                PInvokeCore.SendMessage(dialogHandle, SetControlText, (WPARAM)FileNameControl, $"\"{fileDialog.FileName}\"");
            }

            Accept(dialogHandle);
        }
        else
        {
            base.OnDialogIdle(dialogHandle);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Application.EnterThreadModal -= OnEnterThreadModal;
            foreach (CommonDialog dialog in _dialogs)
            {
                dialog.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
