using System;
using System.Collections.Generic;

namespace Karaakeb.Core.DTO.AuthenticationDTO
{
    public class MeResponseDTO
    {
        public bool IsAuthenticated { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool RememberMe { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
