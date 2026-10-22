// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Text;
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
    public void CodeDomSerializer_Deserialize_NullCodeObject_ThrowsArgumentNullException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        Assert.Throws<ArgumentNullException>("codeObject", () => underTest.Deserialize(manager, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(42)]
    [InlineData("value")]
    public void CodeDomSerializer_Deserialize_PrimitiveExpression_ReturnsValue(object value)
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();

        object result = underTest.Deserialize(manager, new CodePrimitiveExpression(value));

        Assert.Equal(value, result);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_SingleStatement_ReturnsNull()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();

        object result = underTest.Deserialize(manager, new CodeExpressionStatement(new CodePrimitiveExpression(42)));

        Assert.Null(result);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_InvalidCodeObject_ThrowsArgumentException()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();
        using NoAssertContext context = new();

        Assert.Throws<ArgumentException>(() => underTest.Deserialize(manager, new object()));
    }

    [Fact]
    public void CodeDomSerializer_Deserialize_StatementsAfterDeclaration_SetsProperty()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();
        CodeStatementCollection statements =
        [
            new CodeVariableDeclarationStatement(
                typeof(StringBuilder),
                "value",
                new CodeObjectCreateExpression(typeof(StringBuilder))),
            new CodeAssignStatement(
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("value"), nameof(StringBuilder.Capacity)),
                new CodePrimitiveExpression(42))
        ];

        var result = Assert.IsType<StringBuilder>(underTest.Deserialize(manager, statements));

        Assert.Equal(42, result.Capacity);
        Assert.Same(result, ((IDesignerSerializationManager)manager).GetInstance("value"));
        Assert.Empty(manager.Errors);
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
    public void CodeDomSerializer_Serialize_Type_ReturnsTypeOfExpression()
    {
        DesignerSerializationManager manager = new();
        CodeDomSerializer underTest = new();

        var result = Assert.IsType<CodeTypeOfExpression>(underTest.Serialize(manager, typeof(string)));

        Assert.Equal(typeof(string).FullName, result.Type.BaseType);
    }

    [Fact]
    public void CodeDomSerializer_Serialize_CompleteObject_ReturnsCreationExpression()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();

        var result = Assert.IsType<CodeObjectCreateExpression>(underTest.Serialize(manager, new Point(42, 84)));

        Assert.Equal(typeof(Point).FullName, result.CreateType.BaseType);
        Assert.Collection(
            result.Parameters.Cast<CodeExpression>(),
            parameter => Assert.Equal(42, Assert.IsType<CodePrimitiveExpression>(parameter).Value),
            parameter => Assert.Equal(84, Assert.IsType<CodePrimitiveExpression>(parameter).Value));
        Assert.Empty(manager.Errors);
    }

    [Theory]
    [InlineData(null, "testdata1", "testdata2")]
    [InlineData("testdata1", "testdata1", "testdata2")]
    [InlineData("existingName", "existingName", "testdata1")]
    public void CodeDomSerializer_Serialize_MultipleObjects_PreservesUniqueNames(
        string existingName,
        string firstExpectedName,
        string secondExpectedName)
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        var underTest = Assert.IsType<CodeDomSerializer>(
            manager.GetSerializer(typeof(TestData), typeof(CodeDomSerializer)));
        TestData first = new() { Number = 42 };
        TestData second = new() { Number = 84 };
        var serializationManager = (IDesignerSerializationManager)manager;
        if (existingName is not null)
        {
            serializationManager.SetName(first, existingName);
        }

        object firstResult = underTest.Serialize(manager, first);
        object secondResult = underTest.Serialize(manager, second);

        AssertSerializedValue(firstResult, firstExpectedName, first.Number);
        AssertSerializedValue(secondResult, secondExpectedName, second.Number);
        Assert.NotEqual(firstExpectedName, secondExpectedName);
        Assert.Equal(firstExpectedName, serializationManager.GetName(first));
        Assert.Equal(secondExpectedName, serializationManager.GetName(second));
        Assert.Same(first, serializationManager.GetInstance(firstExpectedName));
        Assert.Same(second, serializationManager.GetInstance(secondExpectedName));
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
        var underTest = Assert.IsType<CodeDomSerializer>(
            manager.GetSerializer(typeof(TestData), typeof(CodeDomSerializer)));
        TestData value = new() { Number = 42 };
        PropertyDescriptor property = TypeDescriptor.GetProperties(value)[nameof(TestData.Number)];

        CodeStatementCollection statements = underTest.SerializeMember(manager, value, property);

        var assignment = Assert.IsType<CodeAssignStatement>(Assert.Single(statements.Cast<CodeStatement>()));
        AssertPropertyAssignment(assignment, "testdata1", 42);
        Assert.Same(value, ((IDesignerSerializationManager)manager).GetInstance("testdata1"));
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_Event_ReturnsAttachStatement()
    {
        TestData value = new();
        EventDescriptor descriptor = TypeDescriptor.GetEvents(value)[nameof(TestData.Changed)];
        Mock<PropertyDescriptor> eventProperty = new("Changed", Array.Empty<Attribute>());
        eventProperty.Setup(property => property.GetValue(value)).Returns("OnChanged");
        Mock<IEventBindingService> eventBindings = new();
        eventBindings.Setup(service => service.GetEventProperty(descriptor)).Returns(eventProperty.Object);
        using ServiceContainer services = new();
        services.AddService(typeof(IEventBindingService), eventBindings.Object);
        DesignerSerializationManager manager = new(services);
        using IDisposable session = manager.CreateSession();
        var underTest = Assert.IsType<CodeDomSerializer>(
            manager.GetSerializer(typeof(TestData), typeof(CodeDomSerializer)));

        CodeStatementCollection statements = underTest.SerializeMember(manager, value, descriptor);

        var statement = Assert.IsType<CodeAttachEventStatement>(Assert.Single(statements.Cast<CodeStatement>()));
        Assert.Equal(nameof(TestData.Changed), statement.Event.EventName);
        var target = Assert.IsType<CodeVariableReferenceExpression>(statement.Event.TargetObject);
        Assert.Equal("testdata1", target.VariableName);
        var listener = Assert.IsType<CodeDelegateCreateExpression>(statement.Listener);
        Assert.Equal(typeof(EventHandler).FullName, listener.DelegateType.BaseType);
        Assert.IsType<CodeThisReferenceExpression>(listener.TargetObject);
        Assert.Equal("OnChanged", listener.MethodName);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void CodeDomSerializer_SerializeMember_UnsupportedMember_ThrowsNotSupportedException()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeDomSerializer underTest = new();
        Mock<MemberDescriptor> member = new("Member");

        Assert.Throws<NotSupportedException>(() => underTest.SerializeMember(manager, new TestData(), member.Object));
    }

    private static void AssertSerializedValue(object result, string expectedName, int expectedNumber)
    {
        var statements = Assert.IsType<CodeStatementCollection>(result);

        Assert.Collection(
            statements.Cast<CodeStatement>(),
            statement =>
            {
                var declaration = Assert.IsType<CodeVariableDeclarationStatement>(statement);
                Assert.Equal(expectedName, declaration.Name);
                Assert.Equal(typeof(TestData).FullName, declaration.Type.BaseType);
                var creation = Assert.IsType<CodeObjectCreateExpression>(declaration.InitExpression);
                Assert.Equal(typeof(TestData).FullName, creation.CreateType.BaseType);
                Assert.Empty(creation.Parameters);
            },
            statement =>
            {
                var assignment = Assert.IsType<CodeAssignStatement>(statement);
                AssertPropertyAssignment(assignment, expectedName, expectedNumber);
            });
    }

    private static void AssertPropertyAssignment(CodeAssignStatement assignment, string expectedName, int expectedNumber)
    {
        var property = Assert.IsType<CodePropertyReferenceExpression>(assignment.Left);
        Assert.Equal(nameof(TestData.Number), property.PropertyName);
        var target = Assert.IsType<CodeVariableReferenceExpression>(property.TargetObject);
        Assert.Equal(expectedName, target.VariableName);
        Assert.Equal(expectedNumber, Assert.IsType<CodePrimitiveExpression>(assignment.Right).Value);
    }

    /// <summary>
    ///  Provides a constructible object with a serializable property.
    /// </summary>
    private sealed class TestData
    {
        [DefaultValue(0)]
        public int Number { get; set; }

        public event EventHandler Changed;
    }
}
