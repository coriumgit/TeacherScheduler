using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml;
using System.Windows.Data;
using System.Data;

namespace TeacherScheduler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constants      
        public const int HOURS_NR = 8;
        public static readonly string[] DAYS_LABELS = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        #endregion        

        public App() {}

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppView appView = new AppView();
            appView.DataContext = new AppViewModel();
            appView.Show();
        }

        private void onScheduleTableCellMouseDownEnter(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null)
            {
                getParent<DataGrid>(cell).BeginEdit();                
                TextBlock textBlock = cell.Content as TextBlock;
                if (textBlock != null)
                    textBlock.Text = (!bool.Parse(textBlock.Text)).ToString();
            }
        }
     
        public void onScheduleTableColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            var header = sender as DataGridColumnHeader;
            if (header != null)
            {
                DataGridColumn column = header.Column;
                DataGrid owninDataGrid = column.GetType().GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(column, null) as DataGrid;
                foreach (var item in owninDataGrid.Items)
                {
                    TextBlock textBlock = (TextBlock)column.GetCellContent((DataGridRow)owninDataGrid.ItemContainerGenerator.ContainerFromItem(item));
                    textBlock.Text = (!bool.Parse(textBlock.Text)).ToString();
                }
            }
        }

        public void onScheduleTableHourClicked(object sender, RoutedEventArgs e)
        {
            DataGridCell dataGridCell = sender as DataGridCell;
            if (dataGridCell != null)
            {
                DataGridRow row = DataGridRow.GetRowContainingElement(dataGridCell);
                DataGrid dataGrid = getParent<DataGrid>(row);

                ObservableCollection<DataGridColumn> columns = (dataGrid).Columns;
                for (int columnIdx = 1; columnIdx < columns.Count; columnIdx++)
                {
                    TextBlock textBlock = (TextBlock)columns[columnIdx].GetCellContent(row);
                    textBlock.Text = (!bool.Parse(textBlock.Text)).ToString();
                }
            }
        }

        private static T getParent<T>(DependencyObject obj) where T : class
        {
            while (obj != null && !(obj is T))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            return obj as T;
        }

        private void onScheduleTableCellMouseDownEnter(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}
