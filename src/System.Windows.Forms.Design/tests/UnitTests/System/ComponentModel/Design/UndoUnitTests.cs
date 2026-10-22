// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.Design.Serialization;
using Moq;

namespace System.ComponentModel.Design.Tests;

public class UndoUnitTests : UndoEngine
{
    public UndoUnitTests() : base(GetServiceProvider())
    {
    }

    private static IServiceProvider GetServiceProvider()
    {
        Mock<IServiceProvider> mockServiceProvider = new();
        Mock<IDesignerHost> mockDesignerHost = new();
        Mock<IComponentChangeService> mockComponentChangeService = new();
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IDesignerHost)))
            .Returns(mockDesignerHost.Object);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IComponentChangeService)))
            .Returns(mockComponentChangeService.Object);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(ComponentSerializationService)))
            .Returns(new CodeDomComponentSerializationService());
        return mockServiceProvider.Object;
    }

    [Theory]
    [NormalizedStringData]
    public void UndoUnit_Ctor_UndoEngine_String(string name, string expectedName)
    {
        SubUndoUnit unit = new(this, name);
        Assert.Same(this, unit.UndoEngine);
        Assert.Equal(expectedName, unit.Name);
        Assert.True(unit.IsEmpty);
    }

    [Fact]
    public void UndoUnit_NullEngine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("engine", () => new UndoUnit(null, "name"));
    }

    [Fact]
    public void UndoEngine_Ctor_IServiceProvider()
    {
        Assert.True(Enabled);
        Assert.False(UndoInProgress);
    }

    [Fact]
    public void UndoEngine_DiscardUndoUnit_UndoUnit()
    {
        SubUndoUnit unit = new(this, "name");

        DiscardUndoUnit(unit);

        Assert.True(unit.IsEmpty);
    }

    [Theory]
    [BoolData]
    public void UndoEngine_Dispose_Boolean_DetachesServicesOnlyWhenDisposing(bool disposing)
    {
        var host = (IDesignerHost)GetService(typeof(IDesignerHost));
        var changeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
        var hostMock = Mock.Get(host);
        var changeServiceMock = Mock.Get(changeService);

        Dispose(disposing);

        Times expected = disposing ? Times.Once() : Times.Never();
        hostMock.VerifyRemove(h => h.TransactionOpening -= It.IsAny<EventHandler>(), expected);
        hostMock.VerifyRemove(h => h.TransactionClosed -= It.IsAny<DesignerTransactionCloseEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentAdding -= It.IsAny<ComponentEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentChanging -= It.IsAny<ComponentChangingEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentRemoving -= It.IsAny<ComponentEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentAdded -= It.IsAny<ComponentEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentChanged -= It.IsAny<ComponentChangedEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentRemoved -= It.IsAny<ComponentEventHandler>(), expected);
        changeServiceMock.VerifyRemove(s => s.ComponentRename -= It.IsAny<ComponentRenameEventHandler>(), expected);
        Assert.Same(disposing ? null : host, GetService(typeof(IDesignerHost)));
        Assert.Same(disposing ? null : changeService, GetService(typeof(IComponentChangeService)));
    }

    [Fact]
    public void UndoEngine_Get_Enabled()
    {
        Assert.True(Enabled);
    }

    [Fact]
    public void UndoEngine_Get_UndoInProgress()
    {
        Assert.False(UndoInProgress);
    }

    [Fact]
    public void UndoEngine_GetService_Type()
    {
        object service = GetService(typeof(IDesignerHost));

        Assert.NotNull(service);
    }

    [Theory]
    [BoolData]
    public void UndoEngine_Set_Enabled_Boolean(bool value)
    {
        Enabled = value;

        Assert.Equal(value, Enabled);
    }

    protected override void AddUndoUnit(UndoUnit unit)
    {
    }

    protected class SubUndoUnit : UndoUnit
    {
        public SubUndoUnit(UndoEngine engine, string name) : base(engine, name)
        {
        }

        public new UndoEngine UndoEngine => base.UndoEngine;
    }
}
