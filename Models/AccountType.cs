namespace App2.Models
{
    public enum AccountType
    {
        Asset,      // أصول (صندوق، بنك، عملاء)
        Liability,  // خصوم (موردين، قروض)
        Equity,     // حقوق ملكية (رأس المال)
        Revenue,    // إيرادات (مبيعات)
        Expense     // مصروفات (رواتب، إيجار، مشتريات)
    }
}
