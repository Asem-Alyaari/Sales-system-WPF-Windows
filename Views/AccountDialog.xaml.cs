using App2.Models;
using System;
using System.Windows;

namespace App2.Views
{
    public partial class AccountDialog : Window
    {
        public Account? Account { get; private set; }
        private readonly Account? _originalAccount;

        public AccountDialog()
        {
            InitializeComponent();
            TypeComboBox.ItemsSource = Enum.GetValues(typeof(AccountType));
            TypeComboBox.SelectedIndex = 0;
        }

        public AccountDialog(Account account) : this()
        {
            _originalAccount = account;
            Account = account;

            NameTextBox.Text = account.Name;
            CodeTextBox.Text = account.Code;
            TypeComboBox.SelectedItem = account.AccountType;
            IsActiveCheckBox.IsChecked = account.IsActive;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم الحساب", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_originalAccount == null)
            {
                Account = new Account
                {
                    Name = NameTextBox.Text.Trim(),
                    Code = CodeTextBox.Text.Trim(),
                    AccountType = (AccountType)TypeComboBox.SelectedItem,
                    IsActive = IsActiveCheckBox.IsChecked ?? true,
                    CreatedDate = DateTime.Now
                };
            }
            else
            {
                _originalAccount.Name = NameTextBox.Text.Trim();
                _originalAccount.Code = CodeTextBox.Text.Trim();
                _originalAccount.AccountType = (AccountType)TypeComboBox.SelectedItem;
                _originalAccount.IsActive = IsActiveCheckBox.IsChecked ?? true;
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
