using CleanArchitectureTemplate_Domain.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Domain.IRepositoryContract
{
    public interface IOtpRepository
    {
        Task SaveOtpAsync(string email, string code);
        Task<EmailOtp?> GetLatestOtpAsync(string email);
        Task DeleteOtpAsync(EmailOtp otp);
        Task DeleteAllOtpsAsync(string email);
    }
}
