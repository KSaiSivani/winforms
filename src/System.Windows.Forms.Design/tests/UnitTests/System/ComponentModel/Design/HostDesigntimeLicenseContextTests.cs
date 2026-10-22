// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace System.ComponentModel.Design.Tests;

/// <summary>
///  Tests service forwarding through the designer's license context.
/// </summary>
public class HostDesigntimeLicenseContextTests
{
    [Theory]
    [BoolData]
    public void HostDesigntimeLicenseContext_GetService_Invoke_ForwardsToProvider(bool serviceAvailable)
    {
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        object? expected = serviceAvailable ? new object() : null;
        provider.Setup(p => p.GetService(typeof(ITypeResolutionService))).Returns(expected);
        HostDesigntimeLicenseContext context = new(provider.Object);

        Assert.Same(expected, context.GetService(typeof(ITypeResolutionService)));

        provider.Verify(p => p.GetService(typeof(ITypeResolutionService)), Times.Once);
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public void HostDesigntimeLicenseContext_Ctor_IServiceProvider()
    {
        Mock<IServiceProvider> serviceProvider = new();
        HostDesigntimeLicenseContext context = new(serviceProvider.Object);

        Assert.Equal(LicenseUsageMode.Designtime, context.UsageMode);
    }

    [Fact]
    public void HostDesigntimeLicenseContext_GetSavedLicenseKey_NoKey_ReturnsNull()
    {
        Mock<IServiceProvider> serviceProvider = new();
        HostDesigntimeLicenseContext context = new(serviceProvider.Object);

        Assert.Null(context.GetSavedLicenseKey(typeof(Component), resourceAssembly: null));
    }

    [Fact]
    public void HostDesigntimeLicenseContext_SetSavedLicenseKey_GetReturnsNull()
    {
        Mock<IServiceProvider> serviceProvider = new();
        HostDesigntimeLicenseContext context = new(serviceProvider.Object);
        context.SetSavedLicenseKey(typeof(Component), "licenseKey");

        Assert.Null(context.GetSavedLicenseKey(typeof(Component), resourceAssembly: null));
    }
}
