﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort
{
    public static class ListContextSortHelpers
    {
        public static void SortList(List<SortDescription> listSorts, object items)
        {
            var collectionView = (CollectionView) CollectionViewSource.GetDefaultView(items);
            collectionView.SortDescriptions.Clear();

            if (listSorts == null || listSorts.Count < 1) return;

            foreach (var loopSorts in listSorts) collectionView.SortDescriptions.Add(loopSorts);
        }
    }
}