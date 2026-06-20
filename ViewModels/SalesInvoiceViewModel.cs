using App2.Data;
using App2.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace App2.ViewModels
{
    public class SalesInvoiceViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private SalesInvoiceDocumentViewModel _activeInvoice = null!;

        public ObservableCollection<Customer> Customers { get; }
        public ObservableCollection<SalesInvoiceDocumentViewModel> Invoices { get; }

        public SalesInvoiceDocumentViewModel ActiveInvoice
        {
            get => _activeInvoice;
            set => SetProperty(ref _activeInvoice, value);
        }

        public System.Windows.Input.ICommand NewInvoiceCommand { get; }
        public System.Windows.Input.ICommand CloseInvoiceCommand { get; }

        public SalesInvoiceViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            var customersList = _dbContext.Customers.ToList();
            Customers = new ObservableCollection<Customer>(customersList);

            Invoices = new ObservableCollection<SalesInvoiceDocumentViewModel>();
            
            NewInvoiceCommand = new App2.Commands.RelayCommand(ExecuteNewInvoice);
            CloseInvoiceCommand = new App2.Commands.RelayCommand(ExecuteCloseInvoice);

            ExecuteNewInvoice(null);
        }

        private void ExecuteNewInvoice(object? parameter)
        {
            var newInvoice = new SalesInvoiceDocumentViewModel(_dbContext);
            Invoices.Add(newInvoice);
            ActiveInvoice = newInvoice;
        }

        private void ExecuteCloseInvoice(object? parameter)
        {
            if (parameter is SalesInvoiceDocumentViewModel invoiceToClose)
            {
                // التحقق مما إذا كانت الفاتورة تحتوي على بيانات
                bool hasData = invoiceToClose.Items.Any() || 
                               invoiceToClose.SelectedCustomer != null || 
                               !string.IsNullOrWhiteSpace(invoiceToClose.InvoiceNumber);

                if (hasData)
                {
                    var result = System.Windows.MessageBox.Show(
                        "هذه الفاتورة تحتوي على بيانات، هل أنت متأكد من أنك تريد إغلاقها؟",
                        "تأكيد الإغلاق",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning,
                        System.Windows.MessageBoxResult.No);

                    if (result != System.Windows.MessageBoxResult.Yes)
                    {
                        return; // إلغاء الإغلاق
                    }
                }

                Invoices.Remove(invoiceToClose);

                // إذا أغلقنا كل التبويبات، نفتح تبويب جديد فارغ
                if (!Invoices.Any())
                {
                    ExecuteNewInvoice(null);
                }
                // إذا أغلقنا التبويب النشط، نقوم بتفعيل تبويب آخر
                else if (ActiveInvoice == invoiceToClose)
                {
                    ActiveInvoice = Invoices.Last();
                }
            }
        }
    }
}
