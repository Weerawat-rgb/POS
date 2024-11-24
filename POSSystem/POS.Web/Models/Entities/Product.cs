using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Web.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อสินค้า")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุบาร์โค้ด")]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999.99)]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        [Column(TypeName = "ntext")]
        public string? ImageBase64 { get; set; }

        [MaxLength(50)]
        public string? ImageType { get; set; }

        public bool Status { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }
}