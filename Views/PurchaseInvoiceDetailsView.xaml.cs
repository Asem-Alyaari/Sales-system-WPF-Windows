using App2.ViewModels;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class PurchaseInvoiceDetailsView : UserControl
    {
        public PurchaseInvoiceDetailsView(PurchaseInvoiceDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
