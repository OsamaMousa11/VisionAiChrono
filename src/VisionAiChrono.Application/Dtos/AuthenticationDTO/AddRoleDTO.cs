namespace Karaakeb.Core.DTO.AuthenticationDTO
{
    public class AddRoleDTO
    {
        public Guid UserID { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
