using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App2.Models
{
    [Table("Licenses")]
    public class License
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LicenseKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string HardwareId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        public DateTime FirstActivatedDate { get; set; }

        public DateTime? LastActivatedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
