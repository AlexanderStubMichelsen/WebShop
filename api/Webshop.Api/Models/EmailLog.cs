using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Webshop.Api.Models
{
    [Index(nameof(SessionId), IsUnique = true)]
    public class EmailLog
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string SessionId { get; set; } = default!;

        [Required, MaxLength(254)]
        public string To { get; set; } = default!;

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    }
}
