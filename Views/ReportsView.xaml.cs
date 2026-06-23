using App2.ViewModels;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
        }

        private async void ReportsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ReportsViewModel viewModel)
            {
                await viewModel.LoadDataAsync();
            }
        }
    }
}
