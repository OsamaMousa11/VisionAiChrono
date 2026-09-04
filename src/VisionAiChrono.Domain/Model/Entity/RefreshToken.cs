using Microsoft.EntityFrameworkCore;
using System;

namespace CleanArchitectureTemplate_Domain.Model.Entity
{
    [Owned]
    public class RefreshToken
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiredOn { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiredOn;
        public DateTime CreatedOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        public bool IsActive => !RevokedOn.HasValue && !IsExpired;
    }
}
