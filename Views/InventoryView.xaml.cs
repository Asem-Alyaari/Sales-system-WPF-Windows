using App2.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
            var vm = new InventoryViewModel();
            DataContext = vm;

            // تأجيل تحميل البيانات حتى بعد ظهور النافذة
            Loaded += async (s, e) =>
            {
                await vm.LoadDataAsync();
            };
        }

        // عند اختيار صف يتم توسيع تفاصيل دفعاته تلقائياً
        private void InventoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            // طي الصفوف التي أُلغي تحديدها
            foreach (var removed in e.RemovedItems)
            {
                var row = dataGrid.ItemContainerGenerator.ContainerFromItem(removed) as DataGridRow;
                if (row != null)
                    row.DetailsVisibility = Visibility.Collapsed;
            }

            // توسيع الصف المحدد الجديد
            foreach (var added in e.AddedItems)
            {
                var row = dataGrid.ItemContainerGenerator.ContainerFromItem(added) as DataGridRow;
                if (row != null)
                    row.DetailsVisibility = Visibility.Visible;
            }
        }
    }
}
