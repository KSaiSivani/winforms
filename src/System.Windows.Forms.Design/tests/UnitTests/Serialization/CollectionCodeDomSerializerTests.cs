// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.Design.Serialization;

namespace System.Windows.Forms.Design.Serialization.Tests;

public class CollectionCodeDomSerializerTests
{
    [Fact]
    public void CollectionCodeDomSerializer_Constructor()
    {
        CollectionCodeDomSerializer underTest = CollectionCodeDomSerializer.Default;
        Assert.NotNull(underTest);
    }

    [Fact]
    public void CollectionCodeDomSerializer_Deserialize_NullManager_ThrowsArgumentNullException()
    {
        CollectionCodeDomSerializer underTest = CollectionCodeDomSerializer.Default;

        Assert.Throws<ArgumentNullException>(() => underTest.Deserialize(null, new object()));
    }

    [Fact]
    public void CollectionCodeDomSerializer_Deserialize_NullValue_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CollectionCodeDomSerializer underTest = CollectionCodeDomSerializer.Default;

        Assert.Throws<ArgumentNullException>(() => underTest.Deserialize(manager, null));
    }

    [Fact]
    public void CollectionCodeDomSerializer_Serialize_NullManager_ThrowsArgumentNullException()
    {
        CollectionCodeDomSerializer underTest = CollectionCodeDomSerializer.Default;

        Assert.Throws<ArgumentNullException>(() => underTest.Serialize(null, new object()));
    }

    [Fact]
    public void CollectionCodeDomSerializer_Serialize_NullValue_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CollectionCodeDomSerializer underTest = CollectionCodeDomSerializer.Default;

        Assert.Throws<ArgumentNullException>(() => underTest.Serialize(manager, null));
    }
}

