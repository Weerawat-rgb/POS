using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Web.Models.Entities
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal VAT { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }
        
        public string PaymentMethod { get; set; } = string.Empty;
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }
}