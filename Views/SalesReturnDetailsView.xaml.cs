using App2.ViewModels;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class SalesReturnDetailsView : UserControl
    {
        public SalesReturnDetailsView(SalesReturnDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
