using App2.Models;
using System.Collections.ObjectModel;

namespace App2.ViewModels
{
    public class SalesReturnDetailsViewModel : ObservableObject
    {
        private SalesReturn _return;

        public SalesReturn Return
        {
            get => _return;
            set => SetProperty(ref _return, value);
        }

        public ObservableCollection<SalesReturnDetail> Items { get; } = new();
        
        public string PaymentMethodDisplayName
        {
            get
            {
                return _return.PaymentMethod switch
                {
                    ReturnPaymentMethod.ToCustomerAccount => "إلى حساب العميل",
                    ReturnPaymentMethod.Cash => "نقدي من الصندوق",
                    ReturnPaymentMethod.Transfer => "تحويل/شبكة",
                    _ => "غير محدد"
                };
            }
        }

        public SalesReturnDetailsViewModel(SalesReturn returnItem)
        {
            _return = returnItem;
            foreach (var item in returnItem.Details)
            {
                Items.Add(item);
            }
        }
    }
}
