using App2.ViewModels;
using App2.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace App2.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();

            if (DataContext is SettingsViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }

            // Hide license generator for non-admin users
            if (!SessionManager.IsAdmin)
            {
                var licenseCard = this.FindName("LicenseCard") as Border;
                if (licenseCard != null)
                {
                    licenseCard.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.StatusMessage) && sender is SettingsViewModel vm)
            {
                // تحديد أي بطاقة حالة يجب تحديثها
                // نستخدم IsSuccess + StatusMessage معاً
                UpdateStatus(PwdStatusBorder, PwdStatusText, vm.StatusMessage, vm.IsSuccess);
                UpdateStatus(UsernameStatusBorder, UsernameStatusText, vm.StatusMessage, vm.IsSuccess);
            }
        }

        private static void UpdateStatus(Border border, TextBlock text, string message, bool isSuccess)
        {
            if (string.IsNullOrEmpty(message))
            {
                border.Visibility = Visibility.Collapsed;
                return;
            }

            text.Text = message;
            border.Visibility = Visibility.Visible;

            if (isSuccess)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                border.BorderThickness = new Thickness(1);
                text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));
            }
            else
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                border.BorderThickness = new Thickness(1);
                text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
            }
        }

        private void CurrentPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
                vm.CurrentPassword = CurrentPwdBox.Password;
        }

        private void NewPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
                vm.NewPassword = NewPwdBox.Password;
        }

        private void ConfirmPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
                vm.ConfirmPassword = ConfirmPwdBox.Password;
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            // مسح حقول كلمة المرور بعد النقر
            if (DataContext is SettingsViewModel vm && vm.IsSuccess)
            {
                CurrentPwdBox.Clear();
                NewPwdBox.Clear();
                ConfirmPwdBox.Clear();
            }
        }

        //private void GenerateLicenseKey_Click(object sender, RoutedEventArgs e)
        //{
        //    var expiryDate = ExpiryDatePicker.SelectedDate;
        //    var licenseKey = LicenseService.GenerateLicenseKey(expiryDate);
        //    LicenseKeyTextBox.Text = licenseKey;
        //}
    }
}
