// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

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
    public void CodeDomSerializer_Deserialize_CodePrimitiveExpression_ReturnsValue()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();
        CodePrimitiveExpression expression = new(42);

        object result = underTest.Deserialize(manager, expression);

        Assert.Equal(42, result);
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_CodeStatementCollection_ReturnsDeclaredValue()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();
        CodeStatementCollection statements =
        [
            new CodeVariableDeclarationStatement(typeof(int), "value", new CodePrimitiveExpression(42))
        ];

        object result = underTest.Deserialize(manager, statements);

        Assert.Equal(42, result);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_CodeStatement_ReturnsNull()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();
        CodeExpressionStatement statement = new(new CodePrimitiveExpression(42));

        object result = underTest.Deserialize(manager, statement);

        Assert.Null(result);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_NullValue_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("codeObject", () => underTest.Deserialize(manager, null));
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_InvalidCodeObject_ThrowsException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

#if DEBUG
        Assert.Throws<InvalidOperationException>(() => underTest.Deserialize(manager, new object()));
#else
        Assert.Throws<ArgumentException>(() => underTest.Deserialize(manager, new object()));
#endif
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
    public void CodeDomSerializer_Serialize_Type_ReturnsCodeTypeOfExpression()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        CodeTypeOfExpression result = Assert.IsType<CodeTypeOfExpression>(
            underTest.Serialize(manager, typeof(string)));

        Assert.Equal(typeof(string).FullName, result.Type.BaseType);
    }

    [Fact]
    public void CodeDomSerializer_Serialize_CompleteObject_ReturnsCreationExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();

        CodeObjectCreateExpression result = Assert.IsType<CodeObjectCreateExpression>(
            underTest.Serialize(manager, new CompleteData(42)));

        Assert.Equal(typeof(CompleteData).FullName, result.CreateType.BaseType);
        CodePrimitiveExpression argument = Assert.IsType<CodePrimitiveExpression>(
            Assert.Single(result.Parameters.Cast<CodeExpression>()));
        Assert.Equal(42, argument.Value);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_Serialize_IncompleteObject_ReturnsDeclarationAndPropertyAssignment()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();

        CodeStatementCollection result = Assert.IsType<CodeStatementCollection>(
            underTest.Serialize(manager, new TestData { Number = 42 }));

        Assert.Collection(
            result.Cast<CodeStatement>(),
            statement =>
            {
                CodeVariableDeclarationStatement declaration = Assert.IsType<CodeVariableDeclarationStatement>(statement);
                Assert.Equal("testdata1", declaration.Name);
                Assert.IsType<CodeObjectCreateExpression>(declaration.InitExpression);
            },
            statement =>
            {
                CodeAssignStatement assignment = Assert.IsType<CodeAssignStatement>(statement);
                CodePropertyReferenceExpression property = Assert.IsType<CodePropertyReferenceExpression>(assignment.Left);
                Assert.Equal(nameof(TestData.Number), property.PropertyName);
                CodeVariableReferenceExpression target = Assert.IsType<CodeVariableReferenceExpression>(property.TargetObject);
                Assert.Equal("testdata1", target.VariableName);
                Assert.Equal(42, Assert.IsType<CodePrimitiveExpression>(assignment.Right).Value);
            });
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_NullManager_ThrowsArgumentNullException()
    {
        CodeDomSerializer underTest = new();
        PropertyDescriptor member = TypeDescriptor.GetProperties(typeof(TestData))[nameof(TestData.Number)];

        Assert.Throws<ArgumentNullException>("manager", () => underTest.SerializeMember(null, new TestData(), member));
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_NullOwningObject_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();
        PropertyDescriptor member = TypeDescriptor.GetProperties(typeof(TestData))[nameof(TestData.Number)];

        Assert.Throws<ArgumentNullException>("owningObject", () => underTest.SerializeMember(manager, null, member));
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_NullMember_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("member", () => underTest.SerializeMember(manager, new TestData(), null));
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_Property_ReturnsAssignment()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = Assert.IsType<CodeDomSerializer>(
            manager.GetSerializer(typeof(TestData), typeof(CodeDomSerializer)));
        TestData value = new() { Number = 42 };
        PropertyDescriptor property = TypeDescriptor.GetProperties(value)[nameof(TestData.Number)];

        CodeStatementCollection statements = underTest.SerializeMember(manager, value, property);

        Assert.Empty(manager.Errors);
        CodeAssignStatement assignment = Assert.IsType<CodeAssignStatement>(
            Assert.Single(statements.Cast<CodeStatement>()));
        CodePropertyReferenceExpression target = Assert.IsType<CodePropertyReferenceExpression>(assignment.Left);
        Assert.Equal(nameof(TestData.Number), target.PropertyName);
        CodeVariableReferenceExpression targetObject = Assert.IsType<CodeVariableReferenceExpression>(target.TargetObject);
        Assert.Equal("testdata1", targetObject.VariableName);
        CodePrimitiveExpression assignedValue = Assert.IsType<CodePrimitiveExpression>(assignment.Right);
        Assert.Equal(42, assignedValue.Value);
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_Event_ReturnsAttachStatement()
    {
        TestEventBindingService eventBindingService = new("OnChanged");
        DesignerSerializationManager manager = new(new TestServiceProvider(eventBindingService));
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = Assert.IsType<CodeDomSerializer>(
            manager.GetSerializer(typeof(TestData), typeof(CodeDomSerializer)));
        TestData value = new();
        EventDescriptor @event = TypeDescriptor.GetEvents(value)[nameof(TestData.Changed)];

        CodeStatementCollection statements = underTest.SerializeMember(manager, value, @event);

        Assert.Empty(manager.Errors);
        CodeAttachEventStatement statement = Assert.IsType<CodeAttachEventStatement>(
            Assert.Single(statements.Cast<CodeStatement>()));
        Assert.Equal(nameof(TestData.Changed), statement.Event.EventName);
        CodeVariableReferenceExpression target = Assert.IsType<CodeVariableReferenceExpression>(
            statement.Event.TargetObject);
        Assert.Equal("testdata1", target.VariableName);
        CodeDelegateCreateExpression listener = Assert.IsType<CodeDelegateCreateExpression>(statement.Listener);
        Assert.IsType<CodeThisReferenceExpression>(listener.TargetObject);
        Assert.Equal("OnChanged", listener.MethodName);
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_UnsupportedMember_ThrowsNotSupportedException()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();

        Assert.Throws<NotSupportedException>(
            () => underTest.SerializeMember(manager, new TestData(), new TestMemberDescriptor()));
    }

    [TypeConverter(typeof(CompleteDataConverter))]
    public sealed class CompleteData
    {
        public CompleteData(int number) => Number = number;

        public int Number { get; }
    }

    public sealed class CompleteDataConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor) && value is CompleteData data)
            {
                ConstructorInfo constructor = typeof(CompleteData).GetConstructor([typeof(int)]);
                return new InstanceDescriptor(constructor, [data.Number], isComplete: true);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    private sealed class TestData
    {
        [DefaultValue(0)]
        public int Number { get; set; }

        public event EventHandler Changed;
    }

    private sealed class TestEventBindingService(string methodName) : IEventBindingService
    {
        private readonly PropertyDescriptor _eventProperty = new TestEventPropertyDescriptor(methodName);

        public string CreateUniqueMethodName(IComponent component, EventDescriptor e) => throw new NotSupportedException();

        public ICollection GetCompatibleMethods(EventDescriptor e) => throw new NotSupportedException();

        public EventDescriptor GetEvent(PropertyDescriptor property) => throw new NotSupportedException();

        public PropertyDescriptorCollection GetEventProperties(EventDescriptorCollection events)
            => throw new NotSupportedException();

        public PropertyDescriptor GetEventProperty(EventDescriptor e) => _eventProperty;

        public bool ShowCode() => throw new NotSupportedException();

        public bool ShowCode(int lineNumber) => throw new NotSupportedException();

        public bool ShowCode(IComponent component, EventDescriptor e) => throw new NotSupportedException();
    }

    private sealed class TestEventPropertyDescriptor(string methodName) : PropertyDescriptor("Changed", [])
    {
        public override Type ComponentType => typeof(TestData);

        public override bool IsReadOnly => false;

        public override Type PropertyType => typeof(string);

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => methodName;

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
        }

        public override bool ShouldSerializeValue(object component) => false;
    }

    private sealed class TestMemberDescriptor() : MemberDescriptor("Member")
    {
    }

    private sealed class TestServiceProvider(object service) : IServiceProvider
    {
        public object GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }
}
