using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Web.Models.Entities
{
    public class SaleDetail
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        
        public Sale? Sale { get; set; }
        public Product? Product { get; set; }
    }
}