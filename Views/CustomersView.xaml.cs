using System.Windows.Controls;
using App2.ViewModels;

namespace App2.Views
{
    public partial class CustomersView : UserControl
    {
        public CustomersView()
        {
            InitializeComponent();
            DataContext = new CustomersViewModel();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // تحميل البيانات بعد فتح الواجهة
            if (DataContext is CustomersViewModel viewModel)
            {
                await viewModel.LoadDataAsync();
            }
        }
    }
}
