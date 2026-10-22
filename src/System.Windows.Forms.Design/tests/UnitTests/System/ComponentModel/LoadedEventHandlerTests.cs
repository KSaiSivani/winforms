// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design.Tests;

/// <summary>
///  Tests invocation of the designer's loaded-event delegate.
/// </summary>
public class LoadedEventHandlerTests
{
    [Fact]
    public void LoadedEventHandler_Invoke_Object_LoadedEventArgs()
    {
        object expectedSender = new();
        LoadedEventArgs expectedEventArgs = new(succeeded: true, errors: null);
        int callCount = 0;

        LoadedEventHandler handler = (sender, e) =>
        {
            Assert.Same(expectedSender, sender);
            Assert.Same(expectedEventArgs, e);
            callCount++;
        };

        handler(expectedSender, expectedEventArgs);

        Assert.Equal(1, callCount);
    }
}
