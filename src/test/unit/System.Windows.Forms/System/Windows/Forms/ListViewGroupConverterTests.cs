// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Moq;

namespace System.Windows.Forms.Tests;

// NB: doesn't require thread affinity
public class ListViewGroupConverterTests
{
    public static TheoryData<Type, bool> CanConvertFromData =>
        CommonTestHelper.GetConvertFromTheoryData();

    [Theory]
    [MemberData(nameof(CanConvertFromData))]
    [InlineData(typeof(ListViewGroup), false)]
    [InlineData(typeof(string), false)]
    public void ListViewGroupConverter_CanConvertFrom_Invoke_ReturnsExpected(Type sourceType, bool expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    public static IEnumerable<object[]> CanConvertFrom_Context_TestData()
    {
        yield return new object[] { null, false };
        yield return new object[] { new(), false };
        yield return new object[] { new ListViewItem(), true };

        ListView listView = new();
        ListViewItem item1 = new();
        listView.Items.Add(item1);
        yield return new object[] { item1, true };

        ListView listViewWithGroups = new();
        ListViewGroup group = new("name", "header");
        listViewWithGroups.Groups.Add(group);
        ListViewItem item2 = new();
        listViewWithGroups.Items.Add(item2);
        yield return new object[] { item2, true };

        ListViewItem item3 = new();
        listViewWithGroups.Items.Add(item3);
        yield return new object[] { new object[] { item2, item3 }, true };
        yield return new object[] { Array.Empty<object>(), false };
        yield return new object[] { new object[] { item2, item1 }, false };
        yield return new object[] { new object[] { item2, new ListViewItem() }, false };
        yield return new object[] { new object[] { item2, new object() }, false };
        yield return new object[] { new object[] { item2, null }, false };
    }

    [Theory]
    [MemberData(nameof(CanConvertFrom_Context_TestData))]
    public void ListViewGroupConverter_CanConvertFrom_Context_ReturnsExpected(object instance, bool expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext
            .Setup(c => c.Instance)
            .Returns(instance);
        Assert.Equal(expected, converter.CanConvertFrom(mockContext.Object, typeof(string)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("(none)")]
    public void ListViewGroupConverter_ConvertFrom_EmptyValue_ReturnsNull(string value)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Null(converter.ConvertFrom(value));
    }

    [Theory]
    [InlineData("header", false)]
    [InlineData("  header  ", false)]
    [InlineData("header", true)]
    [InlineData("  header  ", true)]
    public void ListViewGroupConverter_ConvertFrom_ValidContext_ReturnsExpected(string value, bool multipleItems)
    {
        ListView listView = new();
        ListViewGroup group = new("name", "header");
        listView.Groups.Add(group);
        ListViewItem item = new();
        listView.Items.Add(item);
        ListViewItem otherItem = new();
        listView.Items.Add(otherItem);

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext
            .Setup(c => c.Instance)
            .Returns(multipleItems ? new object[] { item, otherItem } : item);
        Assert.Same(group, converter.ConvertFrom(mockContext.Object, null, value));
    }

    [WinFormsTheory]
    [InlineData(null)]
    [InlineData("(none)")]
    public void ListViewGroupConverter_ConvertFrom_MultipleItemsEmptyValue_ReturnsNull(string value)
    {
        using ListView listView = new();
        ListViewGroup group = new("header");
        listView.Groups.Add(group);
        ListViewItem item1 = new() { Group = group };
        ListViewItem item2 = new() { Group = group };
        listView.Items.AddRange([item1, item2]);

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> context = new(MockBehavior.Strict);
        context.Setup(c => c.Instance).Returns(new object[] { item1, item2 });

        Assert.Null(converter.ConvertFrom(context.Object, null, value));
    }

    [WinFormsTheory]
    [InlineData("unknown")]
    [InlineData("HEADER")]
    public void ListViewGroupConverter_ConvertFrom_MultipleItemsUnknownHeader_ThrowsNotSupportedException(string value)
    {
        using ListView listView = new();
        listView.Groups.Add(new ListViewGroup("header"));
        ListViewItem item1 = new();
        ListViewItem item2 = new();
        listView.Items.AddRange([item1, item2]);

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> context = new(MockBehavior.Strict);
        context.Setup(c => c.Instance).Returns(new object[] { item1, item2 });

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context.Object, null, value));
    }

    [WinFormsTheory]
    [InlineData("First header", 0)]
    [InlineData("Second header", 1)]
    [InlineData("  Second header  ", 1)]
    public void ListViewGroupConverter_ConvertFrom_MultipleItemsMultipleGroups_ReturnsExpected(string value, int groupIndex)
    {
        using ListView listView = new();
        ListViewGroup group1 = new("firstName", "First header");
        ListViewGroup group2 = new("secondName", "Second header");
        listView.Groups.AddRange([group1, group2]);
        ListViewItem item1 = new() { Group = group1 };
        ListViewItem item2 = new() { Group = group2 };
        listView.Items.AddRange([item1, item2]);

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> context = new(MockBehavior.Strict);
        context.Setup(c => c.Instance).Returns(new object[] { item1, item2 });

        Assert.Same(listView.Groups[groupIndex], converter.ConvertFrom(context.Object, null, value));
    }

    public static IEnumerable<object[]> ConvertFrom_InvalidContext_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { new() };
        yield return new object[] { new ListViewItem() };

        ListView listView = new();
        ListViewItem item1 = new();
        listView.Items.Add(item1);
        yield return new object[] { item1 };

        ListView listViewWithGroups = new();
        listViewWithGroups.Groups.Add(new ListViewGroup("name", "header"));
        ListViewItem item2 = new();
        listViewWithGroups.Items.Add(item2);
        yield return new object[] { item2 };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_InvalidContext_TestData))]
    public void ListViewGroupConverter_ConvertFrom_InvalidContext_ThrowsNotSupportedException(object instance)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext
            .Setup(c => c.Instance)
            .Returns(instance);

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(mockContext.Object, null, "value"));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(mockContext.Object, null, "HEADER"));
    }

    [Theory]
    [InlineData("(NONE)")]
    [InlineData("value")]
    [InlineData(1)]
    public void ListViewGroupConverter_ConvertFrom_InvalidValue_ThrowsNotSupportedException(object value)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value));
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), true)]
    [InlineData(typeof(ListViewGroup), false)]
    [InlineData(typeof(int), false)]
    [InlineData(null, false)]
    public void ListViewGroupConverter_CanConvertTo_Invoke_ReturnsExpected(Type destinationType, bool expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Equal(expected, converter.CanConvertTo(destinationType));
    }

    public static IEnumerable<object[]> CanConvertTo_Context_TestData()
    {
        yield return new object[] { null, true };
        yield return new object[] { new(), true };
        yield return new object[] { new ListViewItem(), true };

        ListView listView = new();
        ListViewItem item1 = new();
        listView.Items.Add(item1);
        yield return new object[] { item1, true };

        ListView listViewWithGroups = new();
        ListViewGroup group = new("name", "header");
        listViewWithGroups.Groups.Add(group);
        ListViewItem item2 = new();
        listViewWithGroups.Items.Add(item2);
        yield return new object[] { item2, true };
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_Context_TestData))]
    public void ListViewGroupConverter_CanConvertTo_Context_ReturnsExpected(object instance, bool expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext
            .Setup(c => c.Instance)
            .Returns(instance);
        Assert.Equal(expected, converter.CanConvertTo(mockContext.Object, typeof(string)));
    }

    public static IEnumerable<object[]> ConvertTo_InstanceDescriptor_TestData()
    {
        yield return new object[]
        {
            new ListViewGroup(),
            new Type[] { typeof(string), typeof(HorizontalAlignment) },
            new object[] { "ListViewGroup", HorizontalAlignment.Left }
        };
        yield return new object[]
        {
            new ListViewGroup("key", "headerText"),
            new Type[] { typeof(string), typeof(HorizontalAlignment) },
            new object[] { "headerText", HorizontalAlignment.Left }
        };
        yield return new object[]
        {
            new ListViewGroup("header"),
            new Type[] { typeof(string), typeof(HorizontalAlignment) },
            new object[] { "header", HorizontalAlignment.Left }
        };
        yield return new object[]
        {
            new ListViewGroup("header", HorizontalAlignment.Center),
            new Type[] { typeof(string), typeof(HorizontalAlignment) },
            new object[] { "header", HorizontalAlignment.Center }
        };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InstanceDescriptor_TestData))]
    public void ListViewGroupConverter_ConvertTo_InstanceDescriptor_ReturnsExpected(ListViewGroup value, Type[] parameterTypes, object[] arguments)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(value, typeof(InstanceDescriptor)));
        Assert.Equal(typeof(ListViewGroup).GetConstructor(parameterTypes), descriptor.MemberInfo);
        Assert.Equal(arguments, descriptor.Arguments);
    }

    [Theory]
    [InlineData(null, "(none)")]
    [InlineData(1, "1")]
    public void ListViewGroupConverter_ConvertTo_String_ReturnsExpected(object value, string expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Equal(expected, converter.ConvertTo(value, typeof(string)));
    }

    [Fact]
    public void ListViewGroupConverter_ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(new object(), null));
    }

    [Fact]
    public void ListViewGroupConverter_ConvertTo_ValueNotThrowsNotSupportedException()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(1, typeof(InstanceDescriptor)));
    }

    [Theory]
    [InlineData(typeof(ListViewGroup))]
    [InlineData(typeof(int))]
    public void ListViewGroupConverter_ConvertTo_InvalidDestinationType_ThrowsNotSupportedException(Type destinationType)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new ListViewGroup(), destinationType));
    }

    [Fact]
    public void ListViewGroupConverter_GetPropertiesSupported_Invoke_ReturnsFalse()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.False(converter.GetPropertiesSupported(null));
    }

    [Fact]
    public void ListViewGroupConverter_GetProperties_Invoke_ReturnsNull()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        ListViewGroup item = new();
        Assert.Null(converter.GetProperties(null, item, null));
    }

    public static IEnumerable<object[]> GetStandardValues_TestData()
    {
        yield return new object[] { null, null };
        yield return new object[] { new(), null };
        yield return new object[] { new ListViewItem(), null };

        ListView listView = new();
        ListViewItem item1 = new();
        listView.Items.Add(item1);
        yield return new object[] { item1, new object[] { null } };

        ListView listViewWithGroups = new();
        ListViewGroup group = new("name", "header");
        listViewWithGroups.Groups.Add(group);
        ListViewItem item2 = new();
        listViewWithGroups.Items.Add(item2);
        yield return new object[] { item2, new object[] { group, null } };

        ListViewItem item3 = new();
        listViewWithGroups.Items.Add(item3);
        yield return new object[] { new object[] { item2, item3 }, new object[] { group, null } };
        yield return new object[] { new object[] { item2, item1 }, null };
        yield return new object[] { Array.Empty<object>(), null };
        yield return new object[] { new object[] { item2, new ListViewItem() }, null };
        yield return new object[] { new object[] { item2, new object() }, null };
    }

    [Theory]
    [MemberData(nameof(GetStandardValues_TestData))]
    public void ListViewGroupConverter_GetStandardValues_HasContext_ReturnsExpected(object instance, object[] expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> mockContext = new(MockBehavior.Strict);
        mockContext
            .Setup(c => c.Instance)
            .Returns(instance);
        Assert.Equal(expected, converter.GetStandardValues(mockContext.Object)?.Cast<object>());
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListViewGroupConverter_GetStandardValues_MultipleItems_ReturnsGroupsAndNone(bool hasGroups)
    {
        using ListView listView = new();
        ListViewGroup group1 = new("First header");
        ListViewGroup group2 = new("Second header");
        if (hasGroups)
        {
            listView.Groups.AddRange([group1, group2]);
        }

        ListViewItem item1 = new();
        ListViewItem item2 = new();
        listView.Items.AddRange([item1, item2]);

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Mock<ITypeDescriptorContext> context = new(MockBehavior.Strict);
        context.Setup(c => c.Instance).Returns(new object[] { item1, item2 });

        TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context.Object);
        Assert.NotNull(values);
        object[] expected = hasGroups ? [group1, group2, null] : [null];
        Assert.Equal(expected, values.Cast<object>());
        Assert.Equal("(none)", converter.ConvertTo(context.Object, null, values[values.Count - 1], typeof(string)));
    }

    [Fact]
    public void ListViewGroupConverter_GetStandardValues_NullContext_ReturnsNull()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.Null(converter.GetStandardValues(null));
    }

    [Fact]
    public void ListViewGroupConverter_GetStandardValuesExclusive_Invoke_ReturnsTrue()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.True(converter.GetStandardValuesExclusive(null));
    }

    [Fact]
    public void ListViewGroupConverter_GetStandardValuesSupported_Invoke_ReturnsTrue()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(ListViewGroup));
        Assert.True(converter.GetStandardValuesSupported(null));
    }
}
