// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Forms;

public partial class DataGridViewRowCollection
{
    private class RowList : List<DataGridViewRow>
    {
        private readonly DataGridViewRowCollection _owner;
        private int[]? _originalRowIndexes;
        private RowComparer? _rowComparer;

        public RowList(DataGridViewRowCollection owner)
        {
            _owner = owner;
        }

        public void CustomSort(RowComparer rowComparer)
        {
            Debug.Assert(rowComparer is not null);
            Debug.Assert(Count > 0);

            _rowComparer = rowComparer;
            _originalRowIndexes = new int[Count];
            for (int i = 0; i < Count; i++)
            {
                _originalRowIndexes[i] = i;
            }

            try
            {
                CustomQuickSort(0, Count - 1);
            }
            finally
            {
                _originalRowIndexes = null;
                _rowComparer = null;
            }
        }

        private void CustomQuickSort(int left, int right)
        {
            // Custom recursive QuickSort needed because of the notion of shared rows.
            // The indexes of the compared rows are required to do the comparisons.
            // For a study comparing the iterative and recursive versions of the QuickSort
            // see https://web.archive.org/web/20051125015050/http://www.mathcs.carleton.edu/courses/course_resources/cs227_w96/wightmaj/data.html
            // Is the recursive version going to cause trouble with large dataGridViews?
            do
            {
                if (right - left < 2) // sort subarray of two elements
                {
                    if (right - left > 0 && CompareRows(left, right) > 0)
                    {
                        SwapRows(left, right);
                    }

                    return;
                }

                int k = (left + right) >> 1;
                object? x = Pivot(left, k, right);
                int i = left + 1;
                int j = right - 1;
                do
                {
                    while (k != i && CompareRows(i, _rowComparer!.GetComparedObject(i), k, x) < 0)
                    {
                        i++;
                    }

                    while (k != j && CompareRows(k, x, j, _rowComparer!.GetComparedObject(j)) < 0)
                    {
                        j--;
                    }

                    Debug.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                    if (i > j)
                    {
                        break;
                    }

                    if (i < j)
                    {
                        SwapRows(i, j);
                        if (i == k)
                        {
                            k = j;
                        }
                        else if (j == k)
                        {
                            k = i;
                        }
                    }

                    i++;
                    j--;
                }
                while (i <= j);

                if (j - left <= right - i)
                {
                    if (left < j)
                    {
                        CustomQuickSort(left, j);
                    }

                    left = i;
                }
                else
                {
                    if (i < right)
                    {
                        CustomQuickSort(i, right);
                    }

                    right = j;
                }
            }
            while (left < right);
        }

        private object? Pivot(int left, int center, int right)
        {
            // find median-of-3 (left, center and right) and sort these 3 elements
            if (CompareRows(left, center) > 0)
            {
                SwapRows(left, center);
            }

            if (CompareRows(left, right) > 0)
            {
                SwapRows(left, right);
            }

            if (CompareRows(center, right) > 0)
            {
                SwapRows(center, right);
            }

            return _rowComparer!.GetComparedObject(center);
        }

        private int CompareRows(int rowIndex1, int rowIndex2) =>
            CompareRows(
                rowIndex1,
                _rowComparer!.GetComparedObject(rowIndex1),
                rowIndex2,
                _rowComparer.GetComparedObject(rowIndex2));

        private int CompareRows(int rowIndex1, object? value1, int rowIndex2, object? value2) =>
            _rowComparer!.CompareObjects(
                value1,
                value2,
                rowIndex1,
                rowIndex2,
                _originalRowIndexes![rowIndex1],
                _originalRowIndexes[rowIndex2]);

        private void SwapRows(int rowIndex1, int rowIndex2)
        {
            _owner.SwapSortedRows(rowIndex1, rowIndex2);
            (_originalRowIndexes![rowIndex1], _originalRowIndexes[rowIndex2]) =
                (_originalRowIndexes[rowIndex2], _originalRowIndexes[rowIndex1]);
        }
    }
}
