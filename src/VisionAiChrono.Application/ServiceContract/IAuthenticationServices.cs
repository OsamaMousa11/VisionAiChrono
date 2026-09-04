using Karaakeb.Core.DTO.AuthenticationDTO;
using CleanArchitectureTemplate_Domain.Model.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.ServiceContract
{
    public interface IAuthenticationServices
    {
        Task RegisterAsync(RegisterDTO registerDTO); // Task
        Task<AuthenticationResponse> LoginAsync(LoginDTO loginDTO);
        Task<AuthenticationResponse> ExternalLoginAsync(string provider, string providerKey, string email, string? name);
        Task<AuthenticationResponse> RefreshTokenAsync(string token);
        Task<AuthenticationResponse> GenerateJwtToken(ApplicationUser user);
        Task RevokeTokenAsync(string token); // Task
        Task<bool> LogoutAsync(string userId);

        // ====== Roles ======
        Task AddRoleToUserAsync(AddRoleDTO model); // Task
        Task<IEnumerable<ApplicationRole>> GetAllRolesAsync();
        Task DeleteRoleAsync(string roleName); // Task

        // ====== Users ======
        Task<MeResponseDTO> GetMeAsync(string token);
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser?> GetUserByIdAsync(string id);
        Task UpdateUserAsync(string id, UpdateUserDTO dto); // Task
        Task DeleteUserAsync(string id); // Task
        Task ChangePasswordAsync(string userId, ChangePasswordDTO dto);

        // ====== Encapsulated Identity Operations ======
        Task ResendOtpAsync(ResendOtpDTO dto);
        Task<AuthenticationResponse> VerifyOtpAsync(VerifyOtpDTO dto);
        Task ForgotPasswordAsync(ForgotPasswordDTO dto);
        Task<AuthenticationResponse> ResetPasswordAsync(ResetPasswordDTO dto);
        Task<IEnumerable<UserResponseDTO>> GetAllUsersWithRoleUserAsync();
        Task<UserResponseDTO> GetUserByIdWithRoleUserAsync(string id);
        Task DeleteAccountByOtpAsync(DeleteAccountDTO dto);
        Task DeleteAccountByIdAsync(string id);
        Task<MeResponseDTO> GetMeByIdAsync(string id);
    }
}
