using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (ItemListBox != null && ItemListBox.Items.Count > 0)
                {
                    ItemListBox.Focus();
                    ItemListBox.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.SalesInvoiceViewModel vm && vm.ActiveInvoice != null)
                {
                    if (ItemListBox != null && ItemListBox.Items.Count == 1)
                    {
                        ItemListBox.SelectedIndex = 0;
                    }
                    if (vm.ActiveInvoice.SelectedBatch != null)
                    {
                        vm.ActiveInvoice.ConfirmSelection();
                        SearchBox.Focus();
                        e.Handled = true;
                    }
                }
            }
        }

        private void ItemListBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.SalesInvoiceViewModel vm && vm.ActiveInvoice != null)
                {
                    if (vm.ActiveInvoice.SelectedBatch != null)
                    {
                        vm.ActiveInvoice.ConfirmSelection();
                        SearchBox.Focus();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Up && ItemListBox.SelectedIndex == 0)
            {
                SearchBox.Focus();
                e.Handled = true;
            }
        }

        private void ItemListBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.SalesInvoiceViewModel vm && vm.ActiveInvoice != null)
            {
                if (vm.ActiveInvoice.SelectedBatch != null)
                {
                    vm.ActiveInvoice.ConfirmSelection();
                    SearchBox.Focus();
                }
            }
        }

        private void CustomerSearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (CustomerListBox != null && CustomerListBox.Items.Count > 0)
                {
                    CustomerListBox.Focus();
                    CustomerListBox.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.SalesInvoiceViewModel vm && vm.ActiveInvoice != null)
                {
                    if (CustomerListBox != null && CustomerListBox.Items.Count == 1)
                    {
                        CustomerListBox.SelectedIndex = 0;
                    }
                    if (vm.ActiveInvoice.SelectedCustomer != null)
                    {
                        vm.ActiveInvoice.ConfirmCustomerSelection();
                        CustomerSearchBox.Focus();
                        e.Handled = true;
                    }
                }
            }
        }

        private void CustomerListBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.SalesInvoiceViewModel vm && vm.ActiveInvoice != null)
                {
                    if (vm.ActiveInvoice.SelectedCustomer != null)
                    {
                        vm.ActiveInvoice.ConfirmCustomerSelection();
                        CustomerSearchBox.Focus();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Up && CustomerListBox.SelectedIndex == 0)
            {
                CustomerSearchBox.Focus();
                e.Handled = true;
            }
        }

        private void CustomerListBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.SalesInvoiceViewModel vm && vm.ActiveInvoice != null)
            {
                if (vm.ActiveInvoice.SelectedCustomer != null)
                {
                    vm.ActiveInvoice.ConfirmCustomerSelection();
                    CustomerSearchBox.Focus();
                }
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // السماح فقط بالأرقام والنقطة العشرية
            Regex regex = new Regex("[^0-9.]");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
