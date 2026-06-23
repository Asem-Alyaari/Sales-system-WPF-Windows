using App2.ViewModels;
using System.Windows;

namespace App2.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
            
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.LoginSuccessful += OnLoginSuccessful;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        private void OnLoginSuccessful()
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
