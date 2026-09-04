using CleanArchitectureTemplate_Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Domain.Model.Entity
{
    public class EmailOtp : BaseEntity
    {


        public string Email { get; set; }

        public string Code { get; set; }

        public DateTime ExpirationTime { get; set; }

        public bool IsUsed { get; set; }
    }
}
