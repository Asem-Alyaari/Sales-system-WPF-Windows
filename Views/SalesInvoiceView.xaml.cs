using System.Windows.Controls;

namespace App2.Views
{
    public partial class SalesInvoiceView : UserControl
    {
        private ViewModels.SalesInvoiceViewModel _viewModel;

        public SalesInvoiceView()
        {
            InitializeComponent();
            _viewModel = new ViewModels.SalesInvoiceViewModel();
            DataContext = _viewModel;
        }
    }
}
