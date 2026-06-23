using System.Windows;
using App2.Services;

namespace App2.Views
{
    public partial class LicenseActivationView : Window
    {
        public bool ActivationSuccessful { get; private set; } = false;

        public LicenseActivationView()
        {
            InitializeComponent();
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            var licenseKey = LicenseKeyTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                MessageBox.Show("يرجى إدخال مفتاح الترخيص", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = LicenseService.ActivateLicense(licenseKey);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ActivationSuccessful = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(result.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
