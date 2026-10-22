// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace System.Windows.Forms.Design.Serialization.Tests;

public class MemberCodeDomSerializerTests
{
    [Fact]
    public void MemberCodeDomSerializer_Constructor()
    {
        TestMemberCodeDomSerializer underTest = new();

        Assert.NotNull(underTest);
    }

    [Theory]
    [BoolData]
    public void MemberCodeDomSerializer_SerializeMember_RespectsShouldSerialize(bool expected)
    {
        TestMemberCodeDomSerializer underTest = new() { Result = expected };
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        var serializer = Assert.IsType<CodeDomSerializer>(
            manager.GetSerializer(typeof(TestData), typeof(CodeDomSerializer)));
        ((IDesignerSerializationManager)manager).AddSerializationProvider(new TestSerializationProvider(underTest));
        TestData value = new();
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(TestData))[nameof(TestData.Number)];

        CodeStatementCollection statements = serializer.SerializeMember(manager, value, descriptor);

        if (expected)
        {
            var statement = Assert.IsType<CodeCommentStatement>(Assert.Single(statements.Cast<CodeStatement>()));
            Assert.Equal(descriptor.Name, statement.Comment.Text);
        }
        else
        {
            Assert.Empty(statements);
        }

        Assert.Same(manager, underTest.Manager);
        Assert.Same(value, underTest.Value);
        Assert.Same(descriptor, underTest.Descriptor);
        Assert.Empty(manager.Errors);
    }

    /// <summary>
    ///  Supplies the member serializer whose serialization decision is being tested.
    /// </summary>
    private sealed class TestSerializationProvider(MemberCodeDomSerializer serializer) : IDesignerSerializationProvider
    {
        public object GetSerializer(
            IDesignerSerializationManager manager,
            object currentSerializer,
            Type objectType,
            Type serializerType)
            => serializerType == typeof(MemberCodeDomSerializer) ? serializer : null;
    }

    /// <summary>
    ///  Records member serialization requests and controls whether they produce a statement.
    /// </summary>
    private sealed class TestMemberCodeDomSerializer : MemberCodeDomSerializer
    {
        public bool Result { get; init; }

        public IDesignerSerializationManager Manager { get; private set; }

        public object Value { get; private set; }

        public MemberDescriptor Descriptor { get; private set; }

        public override void Serialize(
            IDesignerSerializationManager manager,
            object value,
            MemberDescriptor descriptor,
            CodeStatementCollection statements)
            => statements.Add(new CodeCommentStatement(descriptor.Name));

        public override bool ShouldSerialize(
            IDesignerSerializationManager manager,
            object value,
            MemberDescriptor descriptor)
        {
            Manager = manager;
            Value = value;
            Descriptor = descriptor;

            return Result;
        }
    }

    /// <summary>
    ///  Provides a property for member serialization.
    /// </summary>
    private sealed class TestData
    {
        public int Number { get; set; }
    }
}
