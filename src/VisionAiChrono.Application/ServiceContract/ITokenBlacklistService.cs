using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.ServiceContract
{
    public interface ITokenBlacklistService
    {
        void BlacklistToken(string token, DateTime expiry);
        bool IsBlacklisted(string token);
    }
}
