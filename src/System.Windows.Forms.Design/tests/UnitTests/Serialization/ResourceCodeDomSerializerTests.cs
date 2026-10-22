// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Resources;

namespace System.Windows.Forms.Design.Serialization.Tests;

public class ResourceCodeDomSerializerTests
{
    [Fact]
    public void ResourceCodeDomSerializer_Default_ReturnsSameInstance()
    {
        Assert.Same(ResourceCodeDomSerializer.Default, ResourceCodeDomSerializer.Default);
    }

    [Theory]
    [InlineData(true, "field")]
    [InlineData(false, "variable")]
    public void ResourceCodeDomSerializer_GetTargetComponentName_ApplyResources_ReturnsTargetName(
        bool useField,
        string expected)
    {
        ResourceCodeDomSerializer underTest = ResourceCodeDomSerializer.Default;
        CodeExpression target = useField
            ? new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), expected)
            : new CodeVariableReferenceExpression(expected);
        CodeExpressionStatement statement = new(
            new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression("resources"),
                "ApplyResources",
                target,
                new CodePrimitiveExpression(expected)));

        string result = underTest.GetTargetComponentName(statement, null, null);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResourceCodeDomSerializer_Deserialize_ResourceDeclaration_ReturnsNamedResourceManager()
    {
        DesignerSerializationManager manager = new();
        using IDisposable session = manager.CreateSession();
        CodeStatementCollection statements =
        [
            new CodeVariableDeclarationStatement(
                typeof(ComponentResourceManager),
                "resources",
                new CodeObjectCreateExpression(typeof(ComponentResourceManager)))
        ];

        object result = ResourceCodeDomSerializer.Default.Deserialize(manager, statements);

        Assert.IsType<ResourceCodeDomSerializer.SerializationResourceManager>(result);
        Assert.Same(result, ((IDesignerSerializationManager)manager).GetInstance("resources"));
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void ResourceCodeDomSerializer_Serialize_String_WritesResourceAndReturnsGetStringExpression()
    {
        TestResourceService resourceService = new();
        DesignerSerializationManager manager = new(new TestServiceProvider(resourceService));
        using IDisposable session = manager.CreateSession();
        object owner = new();
        IDesignerSerializationManager serializationManager = manager;
        serializationManager.SetName(owner, "component");
        CodeStatementCollection statements = [];
        serializationManager.Context.Push(statements);
        serializationManager.Context.Push(
            new ExpressionContext(
                new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression("component"),
                    "Text"),
                typeof(string),
                owner,
                "Hello"));

        CodeMethodInvokeExpression result = Assert.IsType<CodeMethodInvokeExpression>(
            ResourceCodeDomSerializer.Default.Serialize(manager, "Hello"));

        CodeVariableDeclarationStatement declaration = Assert.IsType<CodeVariableDeclarationStatement>(
            Assert.Single(statements.Cast<CodeStatement>()));
        Assert.Equal("resources", declaration.Name);
        Assert.Equal("GetString", result.Method.MethodName);
        Assert.Equal(
            "component.Text",
            Assert.IsType<CodePrimitiveExpression>(Assert.Single(result.Parameters.Cast<CodeExpression>())).Value);
        Assert.Equal("Hello", resourceService.Resources["component.Text"]);
        Assert.Empty(manager.Errors);
    }

    [Fact]
    public void ResourceCodeDomSerializer_DeserializeInvariant_ReturnsInvariantResource()
    {
        TestResourceService resourceService = new();
        resourceService.Resources["name"] = "value";
        DesignerSerializationManager manager = new(new TestServiceProvider(resourceService));
        using IDisposable session = manager.CreateSession();

        object result = ResourceCodeDomSerializer.DeserializeInvariant(manager, "name");

        Assert.Equal("value", result);
        Assert.Empty(manager.Errors);
    }

    private sealed class TestServiceProvider(object service) : IServiceProvider
    {
        public object GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }

    private sealed class TestResourceService : IResourceService
    {
        public Dictionary<string, object> Resources { get; } = [];

        public IResourceReader GetResourceReader(CultureInfo info) => new TestResourceReader(Resources);

        public IResourceWriter GetResourceWriter(CultureInfo info) => new TestResourceWriter(Resources);
    }

    private sealed class TestResourceReader(Dictionary<string, object> resources) : IResourceReader
    {
        public void Close()
        {
        }

        public IDictionaryEnumerator GetEnumerator() => ((IDictionary)resources).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
        }
    }

    private sealed class TestResourceWriter(Dictionary<string, object> resources) : IResourceWriter
    {
        public void AddResource(string name, string value) => resources[name] = value;

        public void AddResource(string name, object value) => resources[name] = value;

        public void AddResource(string name, byte[] value) => resources[name] = value;

        public void Close()
        {
        }

        public void Generate()
        {
        }

        public void Dispose()
        {
        }
    }
}
