// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.Windows.Forms;

/// <summary>
///  ListViewGroupConverter is a class that can be used to convert  ListViewGroup objects
///  from one data type to another. Access this class through the TypeDescriptor.
/// </summary>
internal class ListViewGroupConverter : TypeConverter
{
    /// <summary>
    ///  Determines if this converter can convert an object in the given source type to
    ///  the native type of the converter.
    /// </summary>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string) && context is not null && GetListViewFromContext(context) is not null)
        {
            return true;
        }

        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>
    ///  Gets a value indicating whether this converter can convert an object to the given
    ///  destination type using the context.
    /// </summary>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        if (destinationType == typeof(InstanceDescriptor))
        {
            return true;
        }

        if (destinationType == typeof(string) && context is not null && GetListViewFromContext(context) is not null)
        {
            return true;
        }

        return base.CanConvertTo(context, destinationType);
    }

    /// <summary>
    ///  Converts the given object to the converter's native type.
    /// </summary>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            string text = stringValue.Trim();

            if (string.IsNullOrEmpty(text) || text.Equals("None", StringComparison.OrdinalIgnoreCase) || text.Equals(SR.toStringNone, StringComparison.OrdinalIgnoreCase))
                return null;

            ListView? listView = GetListViewFromContext(context!);
            if (listView is not null)
            {
                foreach (ListViewGroup group in listView.Groups)
                {
                    if (string.Equals(group.Header, text, StringComparison.OrdinalIgnoreCase))
                        return group;
                }
            }

            return null;
        }

    return base.ConvertFrom(context, culture, value);
}


    /// <summary>
    ///  Converts the given object to another type. The most common types to convert
    ///  are to and from a string object. The default implementation will make a call
    ///  to ToString on the object if the object is valid and if the destination
    ///  type is string. If this cannot convert to the destination type, this will
    ///  throw a NotSupportedException.
    /// </summary>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);

        if (destinationType == typeof(InstanceDescriptor) && value is ListViewGroup group)
        {
            // Header
            ConstructorInfo ctor = typeof(ListViewGroup).GetConstructor([typeof(string), typeof(HorizontalAlignment)])!;
            Debug.Assert(ctor is not null, "Expected the constructor to exist.");
            return new InstanceDescriptor(ctor, new object[] { group.Header, group.HeaderAlignment }, false);
        }

        if (destinationType == typeof(string) && value is null)
        {
            return SR.toStringNone;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>
    ///  Retrieves a collection containing a set of standard values for the data type this
    ///  validator is designed for. This will return null if the data type does not support
    ///  a standard set of values.
    /// </summary>
    public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context)
    {
        ListView? listView = GetListViewFromContext(context!);
        if (listView is not null)
        {
            var list = new List<ListViewGroup?>();
            foreach (ListViewGroup group in listView.Groups)
            {
                list.Add(group);
            }

            list.Add(null);

            return new StandardValuesCollection(list);
        }

        return null;
    }

    /// <summary>
    ///  Gets the owning ListView from the provided type descriptor context.
    ///  If the context.Instance is a single ListViewItem, returns its ListView.
    ///  If the context.Instance is an array (multi-selection), returns the first
    ///  ListView found among the items. Returns null if no ListView is available.
    /// </summary>
    private static ListView? GetListViewFromContext(ITypeDescriptorContext context)
    {
        object? instance = context.Instance;
        if (instance is ListViewItem item)
        {
            return item.ListView;
        }
     
        if (instance is Array arr)
        {
            foreach (object? obj in arr)
            {
                if (obj is ListViewItem subItem && subItem.ListView is not null)
                {
                    return subItem.ListView;
                }
            }
        }
        return null;
    }

    /// <summary>
    ///  Determines if the list of standard values returned from GetStandardValues is an
    ///  exclusive list. If the list is exclusive, then no other values are valid, such as
    ///  in an enum data type. If the list is not exclusive, then there are other valid values
    ///  besides the list of standard values GetStandardValues provides.
    /// </summary>
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
    {
        return true;
    }

    /// <summary>
    ///  Determines if this object supports a standard set of values that can be picked
    ///  from a list.
    /// </summary>
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
    {
        return true;
    }
}
