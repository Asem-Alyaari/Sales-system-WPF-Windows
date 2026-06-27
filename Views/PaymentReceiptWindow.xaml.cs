using App2.ViewModels;
using App2.Models;
using App2.Data;
using System.Windows;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class PaymentReceiptWindow : Window
    {
        private readonly PaymentReceiptViewModel _viewModel;
        
        public PaymentReceiptWindow(AppDbContext dbContext, Customer customer)
        {
            InitializeComponent();
            _viewModel = new PaymentReceiptViewModel(dbContext, customer);
            _viewModel.PaymentSaved += OnPaymentSaved;
            DataContext = _viewModel;
        }

        private void OnPaymentSaved()
        {
            _viewModel.PaymentSaved -= OnPaymentSaved;
            Close();
        }

        private void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is RadioButton radioButton && radioButton.IsChecked == true)
            {
                _viewModel.PaymentType = radioButton.Content.ToString();
            }
        }
    }
}
