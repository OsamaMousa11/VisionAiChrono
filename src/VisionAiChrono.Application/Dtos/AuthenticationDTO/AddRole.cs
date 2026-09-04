using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karaakeb.Core.DTO.AuthenticationDTO
{
    public class AddRole
    {
        [Required]
        public Guid UserID { get; set; }
        [Required]

        public string RoleName { get; set; } = string.Empty;
    }
}
