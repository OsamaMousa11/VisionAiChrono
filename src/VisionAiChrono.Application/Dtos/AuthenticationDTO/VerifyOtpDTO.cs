using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karaakeb.Core.DTO.AuthenticationDTO
{
    public class VerifyOtpDTO
    {
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    }
}
