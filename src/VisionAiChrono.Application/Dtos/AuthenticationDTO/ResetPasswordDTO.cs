using System.Text.Json.Serialization;

namespace Karaakeb.Core.DTO.AuthenticationDTO
{
    public class ResetPasswordDTO
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
