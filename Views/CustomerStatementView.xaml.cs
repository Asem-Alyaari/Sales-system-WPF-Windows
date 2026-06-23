using App2.Models;
using App2.ViewModels;
using System.ComponentModel;

namespace App2.Views
{
    public partial class CustomerStatementView : System.Windows.Controls.UserControl
    {
        private CustomerStatementViewModel _viewModel;

        public CustomerStatementView()
        {
            InitializeComponent();
            _viewModel = new CustomerStatementViewModel();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CustomerStatementViewModel.SelectedAccount))
            {
                UpdateUIForAccount(_viewModel.SelectedAccount);
            }
            else if (e.PropertyName == nameof(CustomerStatementViewModel.SelectedCustomer))
            {
                UpdateUIForCustomer(_viewModel.SelectedCustomer);
            }
        }

        private void UpdateUIForAccount(Account? account)
        {
            if (account != null)
            {
                HeaderTitle.Text = $"كشف حساب {account.Name}";
                HeaderSubtitle.Text = "عرض جميع الحركات المالية للحساب";
                CustomerCombo.Visibility = System.Windows.Visibility.Collapsed;
                AccountNameText.Text = $"{account.Code} - {account.Name}";
                AccountNameText.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void UpdateUIForCustomer(Customer? customer)
        {
            if (customer != null)
            {
                HeaderTitle.Text = "كشف حساب العميل";
                HeaderSubtitle.Text = "عرض جميع الحركات المالية للعميل";
                CustomerCombo.Visibility = System.Windows.Visibility.Visible;
                AccountNameText.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.Refresh();
        }
    }
}
