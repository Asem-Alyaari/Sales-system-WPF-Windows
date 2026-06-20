namespace App2.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Color { get; set; }
        public string ColorNumber { get; set; } = string.Empty;

        public Product()
        {
        }

        public Product(string? color, string colorNumber)
        {
            Color = color;
            ColorNumber = colorNumber;
        }
    }
}
