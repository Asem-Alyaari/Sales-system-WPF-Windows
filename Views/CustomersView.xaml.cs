using System.Windows.Controls;
using App2.ViewModels;

namespace App2.Views
{
    public partial class CustomersView : UserControl
    {
        public CustomersView()
        {
            InitializeComponent();
            DataContext = new CustomersViewModel();
        }
    }
}
