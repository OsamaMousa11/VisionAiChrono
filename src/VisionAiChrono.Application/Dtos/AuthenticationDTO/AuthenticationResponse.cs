using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karaakeb.Core.DTO.AuthenticationDTO;

public class AuthenticationResponse
{
    public string Message { get; set; }
    public string? ErrorField { get; set; }
    public bool IsAuthenticated { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; }
    public string Token { get; set; }
    //  [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiration { get; set; }
}
