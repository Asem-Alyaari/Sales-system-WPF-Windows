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

            if (_originalCustomer == null)
            {
                Customer = new Customer
                {
                    Name = NameTextBox.Text.Trim(),
                    Phone = PhoneTextBox.Text.Trim(),
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

                // التأكد من وجود حساب للعميل الحالي
                if (!_originalCustomer.AccountId.HasValue && _originalCustomer.Account == null)
                {
                    _originalCustomer.Account = new Account
                    {
                        Name = $"عميل - {_originalCustomer.Name}",
                        AccountType = AccountType.Asset,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                }
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
