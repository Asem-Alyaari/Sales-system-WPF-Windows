using System.Windows.Controls;
using System.Windows.Input;

namespace App2.Views
{
    public partial class SalesReturnView : UserControl
    {
        private ViewModels.SalesReturnViewModel _viewModel;

        public SalesReturnView()
        {
            InitializeComponent();
            _viewModel = new ViewModels.SalesReturnViewModel();
            DataContext = _viewModel;
        }

        private void SearchInvoiceBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (InvoiceListBox != null && InvoiceListBox.Items.Count > 0)
                {
                    InvoiceListBox.Focus();
                    InvoiceListBox.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (InvoiceListBox != null && InvoiceListBox.Items.Count == 1)
                {
                    InvoiceListBox.SelectedIndex = 0;
                }
                if (_viewModel.SelectedInvoice != null)
                {
                    SearchInvoiceBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void InvoiceListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.SelectedInvoice != null)
                {
                    SearchInvoiceBox.Focus();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up && InvoiceListBox.SelectedIndex == 0)
            {
                SearchInvoiceBox.Focus();
                e.Handled = true;
            }
        }

        private void InvoiceListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedInvoice != null)
            {
                SearchInvoiceBox.Focus();
            }
        }

        private void SearchItemBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (ItemListBox != null && ItemListBox.Items.Count > 0)
                {
                    ItemListBox.Focus();
                    ItemListBox.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (ItemListBox != null && ItemListBox.Items.Count == 1)
                {
                    ItemListBox.SelectedIndex = 0;
                }
                if (_viewModel.SelectedBatch != null)
                {
                    _viewModel.ConfirmSelection();
                    SearchItemBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void ItemListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.SelectedBatch != null)
                {
                    _viewModel.ConfirmSelection();
                    SearchItemBox.Focus();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up && ItemListBox.SelectedIndex == 0)
            {
                SearchItemBox.Focus();
                e.Handled = true;
            }
        }

        private void ItemListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedBatch != null)
            {
                _viewModel.ConfirmSelection();
                SearchItemBox.Focus();
            }
        }
    }
}
