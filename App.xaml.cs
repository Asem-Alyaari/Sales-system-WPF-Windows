using System.Configuration;
using System.Data;
using System.Windows;
using App2.ViewModels;

namespace App2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel();
            mainWindow.Show();
        }
    }

}
