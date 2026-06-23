using App2.ViewModels;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class SalesInvoiceDetailsView : UserControl
    {
        public SalesInvoiceDetailsView(SalesInvoiceDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
