namespace App2.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal ?Balance { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public int? AccountId { get; set; }
        public Account? Account { get; set; }

        public Customer()
        {
        }

        public Customer(string name, string phone, decimal? balance)
        {
            Name = name;
            Phone = phone;
            Balance = balance;
            AddedDate = DateTime.Now;
        }
    }
}
