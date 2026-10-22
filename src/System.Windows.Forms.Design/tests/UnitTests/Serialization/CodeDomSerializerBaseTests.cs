// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace System.Windows.Forms.Design.Serialization.Tests;

public class CodeDomSerializerBaseTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(42)]
    [InlineData("value")]
    public void CodeDomSerializerBase_DeserializeExpression_CodePrimitiveExpression_ReturnsValue(object value)
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();

        object result = underTest.InvokeDeserializeExpression(manager, new CodePrimitiveExpression(value));

        Assert.Equal(value, result);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializerBase_SetExpression_GetExpression_ReturnsSameExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        object value = new();
        CodeExpression expression = new CodeVariableReferenceExpression("value");

        underTest.InvokeSetExpression(manager, value, expression);

        Assert.Same(expression, underTest.InvokeGetExpression(manager, value));
    }

    [Fact]
    public void CodeDomSerializerBase_GetExpression_RootContext_ReturnsRootExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        object value = new();
        CodeExpression expression = new CodeThisReferenceExpression();
        ((IDesignerSerializationManager)manager).Context.Push(new RootContext(expression, value));

        Assert.Same(expression, underTest.InvokeGetExpression(manager, value));
    }

    [Fact]
    public void CodeDomSerializerBase_GetExpression_UnknownValue_ReturnsNull()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();

        Assert.Null(underTest.InvokeGetExpression(manager, new object()));
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializerBase_GetExpression_PresetValue_ReturnsContextExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        object value = new();
        CodeExpression expression = new CodeVariableReferenceExpression("existing");
        ((IDesignerSerializationManager)manager).Context.Push(new ExpressionContext(expression, typeof(object), value, value));

        Assert.Same(expression, underTest.InvokeGetExpression(manager, value));
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializerBase_GetUniqueName_UsesExistingName()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        object value = new();
        ((IDesignerSerializationManager)manager).SetName(value, "existingName");

        string result = underTest.InvokeGetUniqueName(manager, value);

        Assert.Equal("existingName", result);
    }

    [Fact]
    public void CodeDomSerializerBase_GetUniqueName_CreatesAndRegistersName()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        TestData value = new();

        string result = underTest.InvokeGetUniqueName(manager, value);

        Assert.Equal("testdata1", result);
        Assert.Same(value, ((IDesignerSerializationManager)manager).GetInstance(result));
    }

    [Fact]
    public void CodeDomSerializerBase_SerializeCreationExpression_ParameterlessConstructor_ReturnsIncompleteExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();

        (CodeExpression expression, bool isComplete) = underTest.InvokeSerializeCreationExpression(manager, new TestData());

        CodeObjectCreateExpression creation = Assert.IsType<CodeObjectCreateExpression>(expression);
        Assert.Equal(typeof(TestData).FullName, creation.CreateType.BaseType);
        Assert.Empty(creation.Parameters);
        Assert.False(isComplete);
    }

    [Fact]
    public void CodeDomSerializerBase_SerializeCreationExpression_PresetValue_ReturnsContextExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        TestData value = new();
        CodeExpression expected = new CodeVariableReferenceExpression("existing");
        ((IDesignerSerializationManager)manager).Context.Push(new ExpressionContext(expected, typeof(TestData), value, value));

        (CodeExpression expression, bool isComplete) = underTest.InvokeSerializeCreationExpression(manager, value);

        Assert.Same(expected, expression);
        Assert.False(isComplete);
        Assert.Empty(manager.Errors);
    }

    [Theory]
    [BoolData]
    public void CodeDomSerializerBase_SerializeProperties_RespectsShouldSerialize(bool shouldSerialize)
    {
        DesignerSerializationManager manager = CreateManagerWithRecordingMemberSerializer(shouldSerialize);
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        CodeStatementCollection statements = [];

        underTest.InvokeSerializeProperties(manager, statements, new TestData(), [BrowsableAttribute.Yes]);

        if (shouldSerialize)
        {
            var statement = Assert.IsType<CodeCommentStatement>(Assert.Single(statements.Cast<CodeStatement>()));
            Assert.Equal(nameof(TestData.Number), statement.Comment.Text);
        }
        else
        {
            Assert.Empty(statements);
        }

        Assert.Null(((IDesignerSerializationManager)manager).Context.Current);
        Assert.Empty(manager.Errors);
    }

    [Theory]
    [BoolData]
    public void CodeDomSerializerBase_SerializeEvents_RespectsShouldSerialize(bool shouldSerialize)
    {
        DesignerSerializationManager manager = CreateManagerWithRecordingMemberSerializer(shouldSerialize);
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        CodeStatementCollection statements = [];

        underTest.InvokeSerializeEvents(manager, statements, new TestData(), [BrowsableAttribute.Yes]);

        if (shouldSerialize)
        {
            var statement = Assert.IsType<CodeCommentStatement>(Assert.Single(statements.Cast<CodeStatement>()));
            Assert.Equal(nameof(TestData.Changed), statement.Comment.Text);
        }
        else
        {
            Assert.Empty(statements);
        }

        Assert.Null(((IDesignerSerializationManager)manager).Context.Current);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializerBase_SerializeToExpression_Primitive_ReturnsPrimitiveExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();

        CodePrimitiveExpression result = Assert.IsType<CodePrimitiveExpression>(
            underTest.InvokeSerializeToExpression(manager, 42));

        Assert.Equal(42, result.Value);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializerBase_SerializeToExpression_ExistingExpression_ReturnsSameExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        TestCodeDomSerializerBase underTest = new();
        object value = new();
        CodeExpression expression = new CodeVariableReferenceExpression("existing");
        underTest.InvokeSetExpression(manager, value, expression);

        Assert.Same(expression, underTest.InvokeSerializeToExpression(manager, value));
        Assert.Empty(manager.Errors);
    }

    private static DesignerSerializationManager CreateManagerWithRecordingMemberSerializer(bool shouldSerialize)
    {
        DesignerSerializationManager manager = new();
        ((IDesignerSerializationManager)manager).AddSerializationProvider(new RecordingSerializationProvider(shouldSerialize));

        return manager;
    }

    /// <summary>
    ///  Exposes protected serialization helpers for testing.
    /// </summary>
    private sealed class TestCodeDomSerializerBase : CodeDomSerializerBase
    {
        public object InvokeDeserializeExpression(IDesignerSerializationManager manager, CodeExpression expression)
            => base.DeserializeExpression(manager, null, expression);

        public CodeExpression InvokeGetExpression(IDesignerSerializationManager manager, object value)
            => base.GetExpression(manager, value);

        public string InvokeGetUniqueName(IDesignerSerializationManager manager, object value)
            => base.GetUniqueName(manager, value);

        public (CodeExpression Expression, bool IsComplete) InvokeSerializeCreationExpression(
            IDesignerSerializationManager manager,
            object value)
        {
            CodeExpression expression = base.SerializeCreationExpression(manager, value, out bool isComplete);

            return (expression, isComplete);
        }

        public void InvokeSerializeEvents(
            IDesignerSerializationManager manager,
            CodeStatementCollection statements,
            object value,
            Attribute[] filter)
            => base.SerializeEvents(manager, statements, value, filter);

        public void InvokeSerializeProperties(
            IDesignerSerializationManager manager,
            CodeStatementCollection statements,
            object value,
            Attribute[] filter)
            => base.SerializeProperties(manager, statements, value, filter);

        public CodeExpression InvokeSerializeToExpression(IDesignerSerializationManager manager, object value)
            => base.SerializeToExpression(manager, value);

        public void InvokeSetExpression(IDesignerSerializationManager manager, object value, CodeExpression expression)
            => base.SetExpression(manager, value, expression);
    }

    /// <summary>
    ///  Supplies a member serializer with a configurable serialization decision.
    /// </summary>
    private sealed class RecordingSerializationProvider(bool shouldSerialize) : IDesignerSerializationProvider
    {
        private readonly RecordingMemberCodeDomSerializer _serializer = new(shouldSerialize);

        public object GetSerializer(
            IDesignerSerializationManager manager,
            object currentSerializer,
            Type objectType,
            Type serializerType)
            => serializerType == typeof(MemberCodeDomSerializer) ? _serializer : null;
    }

    /// <summary>
    ///  Emits a marker for each member selected for serialization.
    /// </summary>
    private sealed class RecordingMemberCodeDomSerializer(bool shouldSerialize) : MemberCodeDomSerializer
    {
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
            => shouldSerialize;
    }

    /// <summary>
    ///  Provides property and event descriptors for serialization.
    /// </summary>
    private sealed class TestData
    {
        [Browsable(true)]
        public int Number { get; set; }

        [Browsable(true)]
        public event EventHandler Changed;
    }
}
