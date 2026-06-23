using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App2.Models
{
    [Table("IssuedKeys")]
    public class IssuedKey
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LicenseKey { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
