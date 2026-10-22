// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace System.ComponentModel.Design.Tests;

/// <summary>
///  Tests filtering of references by their assignable types.
/// </summary>
public class ReferenceServiceTests
{
    [Fact]
    public void ReferenceService_GetReferences_Type_FiltersAssignableReferences()
    {
        using Container container = new();
        using Component component = new();
        using BackgroundWorker worker = new();
        container.Add(component, "component");
        container.Add(worker, "worker");

        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Equal([worker], service.GetReferences(typeof(BackgroundWorker)));
        Assert.Equal([component, worker], service.GetReferences(typeof(Component)));
        Assert.Equal([component, worker], service.GetReferences(typeof(IDisposable)));
        Assert.Empty(service.GetReferences(typeof(string)));
    }

    [Fact]
    public void ReferenceService_Ctor_IServiceProvider()
    {
        using Container container = new();
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService service = new(provider.Object);

        Assert.NotNull(service);
    }

    [Fact]
    public void ReferenceService_GetReference_String_ReturnsExpected()
    {
        using Container container = new();
        using Component component = new();
        container.Add(component, "component");
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Same(component, service.GetReference("component"));
    }

    [Fact]
    public void ReferenceService_GetReference_InvalidName_ReturnsNull()
    {
        using Container container = new();
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Null(service.GetReference("invalidName"));
    }

    [Fact]
    public void ReferenceService_GetName_Object_ReturnsExpected()
    {
        using Container container = new();
        using Component component = new();
        container.Add(component, "component");
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Equal("component", service.GetName(component));
    }

    [Fact]
    public void ReferenceService_GetComponent_Object_ReturnsExpected()
    {
        using Container container = new();
        using Component component = new();
        container.Add(component, "component");
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Same(component, service.GetComponent(component));
    }

    [Fact]
    public void ReferenceService_GetReferences_ReturnsExpected()
    {
        using Container container = new();
        using Component component = new();
        container.Add(component, "component");
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Contains(component, service.GetReferences());
    }

    [Fact]
    public void ReferenceService_GetReferences_Type_ReturnsExpected()
    {
        using Container container = new();
        using Component component = new();
        container.Add(component, "component");
        Mock<IServiceProvider> provider = GetServiceProvider(container);
        using ReferenceService referenceService = new(provider.Object);
        IReferenceService service = referenceService;

        Assert.Contains(component, service.GetReferences(typeof(Component)));
    }

    private static Mock<IServiceProvider> GetServiceProvider(IContainer container)
    {
        Mock<IServiceProvider> provider = new();
        Mock<IComponentChangeService> changeService = new();
        provider.Setup(p => p.GetService(typeof(IContainer))).Returns(container);
        provider.Setup(p => p.GetService(typeof(IComponentChangeService))).Returns(changeService.Object);

        return provider;
    }
}
