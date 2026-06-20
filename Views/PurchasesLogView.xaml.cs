using App2.ViewModels;
using System.Windows.Controls;

namespace App2.Views
{
    public partial class PurchasesLogView : UserControl
    {
        public PurchasesLogView()
        {
            InitializeComponent();
            DataContext = new PurchasesLogViewModel();
        }
    }
}
