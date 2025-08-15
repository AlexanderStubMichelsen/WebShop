using System.ComponentModel.DataAnnotations;

namespace Webshop.Api.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        
        [Required, StringLength(200)]
        public string ProductId { get; set; } = string.Empty;
        
        [Required, StringLength(200)]
        public string ProductName { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public int Quantity { get; set; }
        public long UnitPrice { get; set; }
        public long TotalPrice { get; set; }
        
        [StringLength(3)]
        public string Currency { get; set; } = string.Empty;
        
        // Navigation property
        public Order Order { get; set; } = null!;
    }
}