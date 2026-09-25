using Microsoft.AspNetCore.Identity;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Application.ServiceContract;
using CleanArchitectureTemplate_Domain.Model.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailQueueService _emailQueueService;

        public OtpService(UserManager<ApplicationUser> userManager, IEmailQueueService emailQueueService)
        {
            _userManager = userManager;
            _emailQueueService = emailQueueService;
        }

        public async Task SendOtpAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email)
                ?? throw new NotFoundException("User not found.");

            if (user.LastOtpSentAt != null && DateTime.UtcNow - user.LastOtpSentAt < TimeSpan.FromMinutes(5))
            {
                throw new ConflictException("A verification code has already been sent. Please wait 5 minutes before requesting a new one.");
            }

            var otp = GenerateOtp();

            user.OtpCode = otp;
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(5);
            user.LastOtpSentAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var body = $@"<div style='font-family:Arial,sans-serif'>
    <h2>Your verification code</h2>
    <p>Your verification code is:</p>
    <h1 style='letter-spacing:3px'>{otp}</h1>
    <p>This code is valid for 5 minutes. If you did not request this code, please ignore this email.</p>
</div>";

            _emailQueueService.QueueEmail(
                email,
                "Verification Code",
                body
            );
        }

        public async Task<string?> ValidateOtpAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email)
                ?? throw new NotFoundException("User not found.");

            if (user.OtpCode == null || user.OtpCode != code.Trim())
                return "Invalid verification code.";

            if (user.OtpExpiration < DateTime.UtcNow)
                throw new BadRequestException("The verification code has expired. Please request a new one.");

            user.OtpCode = null;
            user.OtpExpiration = null;
            await _userManager.UpdateAsync(user);

            return null;
        }

        private string GenerateOtp()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }
    }
}

