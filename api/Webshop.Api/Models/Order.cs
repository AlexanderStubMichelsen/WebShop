// Models/Order.cs
using System.ComponentModel.DataAnnotations;

namespace Webshop.Api.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required, StringLength(200)]
        public string SessionId { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? PaymentIntentId { get; set; }
        
        [Required, EmailAddress, StringLength(254)]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? CustomerName { get; set; }
        
        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        
        [Required, StringLength(3)]
        public string Currency { get; set; } = string.Empty;
        
        public long SubtotalAmount { get; set; }
        public long TaxAmount { get; set; }
        public long TotalAmount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property for order items
        public List<OrderItem> OrderItems { get; set; } = new();
        
        // JSON field for additional metadata
        [StringLength(2000)]
        public string? Metadata { get; set; }
    }
}