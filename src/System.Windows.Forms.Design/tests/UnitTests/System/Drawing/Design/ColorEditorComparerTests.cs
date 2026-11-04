// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;

namespace System.Drawing.Design.Tests;

public class ColorEditorComparerTests
{
    public static IEnumerable<object[]> StandardColorComparer_TestData()
    {
        // Each pair ties all preceding sort keys.
        yield return [Color.FromArgb(1, 255, 0, 0), Color.FromArgb(2, 255, 0, 0)];
        yield return [Color.Red, Color.Blue];
        yield return [Color.FromArgb(192, 64, 64), Color.Red];
        yield return [Color.FromArgb(128, 0, 0), Color.Red];
    }

    [Theory]
    [MemberData(nameof(StandardColorComparer_TestData))]
    public void StandardColorComparer_Compare_OrdersByAlphaHueSaturationThenBrightness(Color left, Color right)
    {
        Type type = typeof(ColorEditor).GetNestedType("StandardColorComparer", BindingFlags.NonPublic)!;
        var comparer = Assert.IsAssignableFrom<IComparer<Color>>(type.GetProperty("Instance")!.GetValue(null));

        Assert.True(comparer.Compare(left, right) < 0);
        Assert.True(comparer.Compare(right, left) > 0);
        Assert.Equal(0, comparer.Compare(left, left));
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void SystemColorComparer_Compare_OrdersByInvariantName(string cultureName)
    {
        Type type = typeof(ColorEditor).GetNestedType("SystemColorComparer", BindingFlags.NonPublic)!;
        var comparer = Assert.IsAssignableFrom<IComparer<Color>>(Activator.CreateInstance(type));
        Color[] colors = [SystemColors.ActiveBorder, SystemColors.ActiveCaption, SystemColors.InactiveBorder, SystemColors.Info];
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            for (int i = 0; i < colors.Length; i++)
            {
                for (int j = 0; j < colors.Length; j++)
                {
                    int expected = Math.Sign(string.Compare(colors[i].Name, colors[j].Name, false, CultureInfo.InvariantCulture));
                    Assert.Equal(expected, Math.Sign(comparer.Compare(colors[i], colors[j])));
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
