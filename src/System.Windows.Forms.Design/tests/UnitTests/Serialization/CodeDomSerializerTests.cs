// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using Moq;

namespace System.Windows.Forms.Design.Serialization.Tests;

public class CodeDomSerializerTests
{
    [Fact]
    public void CodeDomSerializer_Constructor()
    {
        CodeDomSerializer underTest = new();
        Assert.NotNull(underTest);
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_NullManager_ThrowsArgumentNullException()
    {
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("manager", () => underTest.Deserialize(null, new object()));
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_NullValue_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("codeObject", () => underTest.Deserialize(manager, null));
    }

    [Fact]
    public void CodeDomSerializer_Serialize_NullManager_ThrowsArgumentNullException()
    {
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("manager", () => underTest.Serialize(null, new object()));
    }

    [Fact]
    public void CodeDomSerializer_Serialize_NullValue_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("value", () => underTest.Serialize(manager, null));
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_NullManager_ThrowsArgumentNullException()
    {
        CodeDomSerializer underTest = new();
        PropertyDescriptor member = TypeDescriptor.GetProperties(typeof(Control))[nameof(Control.Text)];

        Assert.Throws<ArgumentNullException>("manager", () => underTest.SerializeMember(null, new Control(), member));
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_NullValue_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();
        PropertyDescriptor member = TypeDescriptor.GetProperties(typeof(Control))[nameof(Control.Text)];

        Assert.Throws<ArgumentNullException>("owningObject", () => underTest.SerializeMember(manager, null, member));
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_NullMember_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("member", () => underTest.SerializeMember(manager, new Control(), null));
    }
}
