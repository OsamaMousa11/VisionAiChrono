using System;
using System.Collections.Generic;

namespace Karaakeb.Core.DTO.AuthenticationDTO
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiration { get; set; }
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }
}
