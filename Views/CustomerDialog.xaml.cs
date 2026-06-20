using App2.Models;
using System.Windows;

namespace App2.Views
{
    public partial class CustomerDialog : Window
    {
        public Customer? Customer { get; private set; }
        private readonly Customer? _originalCustomer;

        public CustomerDialog()
        {
            InitializeComponent();
        }

        public CustomerDialog(Customer customer) : this()
        {
            _originalCustomer = customer;
            Customer = customer;

            NameTextBox.Text = customer.Name;
            PhoneTextBox.Text = customer.Phone;
            AddressTextBox.Text = customer.Address;
            CreditLimitTextBox.Text = customer.CreditLimit.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم العميل", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("الرجاء إدخال رقم الجوال", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!decimal.TryParse(CreditLimitTextBox.Text, out var creditLimit))
            {
                MessageBox.Show("الرجاء إدخال حد ائتماني صحيح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_originalCustomer == null)
            {
                Customer = new Customer
                {
                    Name = NameTextBox.Text.Trim(),
                    Phone = PhoneTextBox.Text.Trim(),
                    Address = AddressTextBox.Text.Trim(),
                    CreditLimit = creditLimit,
                    Balance = 0,
                    AddedDate = DateTime.Now,
                    Account = new Account
                    {
                        Name = $"عميل - {NameTextBox.Text.Trim()}",
                        AccountType = AccountType.Asset,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    }
                };
            }
            else
            {
                _originalCustomer.Name = NameTextBox.Text.Trim();
                _originalCustomer.Phone = PhoneTextBox.Text.Trim();
                _originalCustomer.Address = AddressTextBox.Text.Trim();
                _originalCustomer.CreditLimit = creditLimit;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
