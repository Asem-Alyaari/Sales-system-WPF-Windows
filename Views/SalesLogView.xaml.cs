using App2.ViewModels;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class SalesLogView : UserControl
    {
        public SalesLogView()
        {
            InitializeComponent();
            DataContext = new SalesLogViewModel();
        }
    }
}
