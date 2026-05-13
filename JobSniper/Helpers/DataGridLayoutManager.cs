using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace JobSniper.Helpers
{
    public static class DataGridLayoutManager
    {
        public static readonly DependencyProperty LayoutIdProperty = DependencyProperty.RegisterAttached(
            "LayoutId",
            typeof(string),
            typeof(DataGridLayoutManager),
            new PropertyMetadata(null, OnLayoutIdChanged));

        public static void SetLayoutId(DependencyObject element, string value) => element.SetValue(LayoutIdProperty, value);
        public static string GetLayoutId(DependencyObject element) => (string)element.GetValue(LayoutIdProperty);

        private static void OnLayoutIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid && e.NewValue is string layoutId && !string.IsNullOrWhiteSpace(layoutId))
            {
                dataGrid.Loaded -= DataGrid_Loaded;
                dataGrid.Loaded += DataGrid_Loaded;
            }
        }

        private static void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DataGrid dataGrid) return;

            LoadState(dataGrid);

            var window = Window.GetWindow(dataGrid);
            if (window != null)
            {
                window.Closing -= Window_Closing;
                window.Closing += Window_Closing;
            }
        }

        private static void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is not Window window) return;

            var grids = FindVisualChildren<DataGrid>(window).Where(g => !string.IsNullOrEmpty(GetLayoutId(g)));

            foreach (var grid in grids)
            {
                SaveState(grid);
            }
        }

        private static void SaveState(DataGrid dataGrid)
        {
            var layoutId = GetLayoutId(dataGrid);
            var filePath = GetFilePath(layoutId);

            var columnsState = dataGrid.Columns.Select(c => new ColumnState
            {
                // Použijeme hlavičku nebo SortMemberPath jako unikátní ID sloupce
                Identifier = c.Header?.ToString() ?? c.SortMemberPath ?? Guid.NewGuid().ToString(),
                WidthValue = c.Width.Value,
                WidthType = c.Width.UnitType,
                DisplayIndex = c.DisplayIndex
            }).ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = JsonSerializer.Serialize(columnsState, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private static void LoadState(DataGrid dataGrid)
        {
            var layoutId = GetLayoutId(dataGrid);
            var filePath = GetFilePath(layoutId);

            if (!File.Exists(filePath)) return;

            try
            {
                var json = File.ReadAllText(filePath);
                var columnsState = JsonSerializer.Deserialize<List<ColumnState>>(json);

                if (columnsState == null) return;

                foreach (var col in dataGrid.Columns)
                {
                    var id = col.Header?.ToString() ?? col.SortMemberPath;
                    var state = columnsState.FirstOrDefault(s => s.Identifier == id);

                    if (state != null)
                    {
                        col.Width = new DataGridLength(state.WidthValue, state.WidthType);
                        col.DisplayIndex = state.DisplayIndex;
                    }
                }
            }
            catch
            {
                // Pokud uživatel smaže JSON nebo dojde k chybě, Grid se načte ve výchozím stavu XAMLu
            }
        }

        private static string GetFilePath(string layoutId)
        {
            // Ukládá do C:\Users\<Ty>\AppData\Local\JobSniper\GridStates\...
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "JobSniper", "GridStates", $"{layoutId}.json");
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }
    }
}