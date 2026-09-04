using CleanArchitectureTemplate_Domain.IRepositoryContract;
using CleanArchitectureTemplate_Domain.Model.Entity;
using Microsoft.EntityFrameworkCore;
using CleanArchitectureTemplate_infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_infrastructure.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly AppDbContext _context;

        public OtpRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveOtpAsync(string email, string code)
        {
            // ? ???? ?????? ?????
            await DeleteAllOtpsAsync(email);

            _context.EmailOtps.Add(new EmailOtp
            {
                Email = email,
                Code = code,
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            });

            await _context.SaveChangesAsync();
        }

        public async Task<EmailOtp?> GetLatestOtpAsync(string email)
        {
            return await _context.EmailOtps
                .Where(x => x.Email == email && !x.IsUsed)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteOtpAsync(EmailOtp otp)
        {
            _context.EmailOtps.Remove(otp);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllOtpsAsync(string email)
        {
            var oldOtps = await _context.EmailOtps
                .Where(x => x.Email == email)
                .ToListAsync();

            _context.EmailOtps.RemoveRange(oldOtps);
            await _context.SaveChangesAsync();
        }
    }
}
