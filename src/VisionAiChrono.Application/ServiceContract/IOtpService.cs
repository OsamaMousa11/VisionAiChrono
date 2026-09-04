using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.ServiceContract
{
    public interface IOtpService
    {
        Task SendOtpAsync(string email);
        Task<string?> ValidateOtpAsync(string email, string code);
    }
}
