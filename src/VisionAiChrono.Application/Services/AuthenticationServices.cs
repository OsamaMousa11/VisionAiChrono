using Karaakeb.Core.DTO.AuthenticationDTO;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Application.ServiceContract;
using CleanArchitectureTemplate_Domain.Model.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using CleanArchitectureTemplate_Domain.Model.Entity;


namespace CleanArchitectureTemplate_Application.Services
{
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IOtpService _otpService;
        private readonly IMailingService _mailService;
        private readonly JwtDTO _jwt;

        public AuthenticationServices(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOtpService otpService,
            IMailingService mailService,
            IOptions<JwtDTO> jwt)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _otpService = otpService;
            _mailService = mailService;
            _jwt = jwt.Value;
        }

        // ========================= Me ========================= //

        public async Task<MeResponseDTO> GetMeAsync(string token)
        {
            // Strip Bearer prefix if present
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Key);

            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch
            {
                throw new UnauthorizedException("The token is invalid or expired.");
            }

            var userId = principal.FindFirst("uid")?.Value
                ?? throw new UnauthorizedException("Invalid token.");

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            return new MeResponseDTO
            {
                UserId = user.Id.ToString(),
                Name = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                IsAuthenticated = true,
                RememberMe = user.RefreshTokens.Any(rt => rt.IsActive),

                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };
        }

        // ========================= Auth ========================= //

        public async Task<AuthenticationResponse> LoginAsync(LoginDTO loginDTO)
        {
            var input = loginDTO.Email.Trim();

            var user = await _userManager.FindByEmailAsync(input)
                       ?? await _userManager.FindByNameAsync(input);

            if (user is null || !await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                return new AuthenticationResponse
                {
                    Message = "Invalid email or password.",
                    ErrorField = "credentials",
                    IsAuthenticated = false
                };

            if (!user.EmailConfirmed)
                return new AuthenticationResponse
                {
                    Message = "Please confirm your email first.",
                    ErrorField = "email",
                    IsAuthenticated = false
                };

            if (user.IsSuspended)
                return new AuthenticationResponse
                {
                    Message = $"Account is suspended. Reason: {user.SuspendReason ?? "Contact technical support."}",
                    ErrorField = "account",
                    IsAuthenticated = false
                };

            var authResponse = await GenerateJwtToken(user);

            if (user.RefreshTokens != null && user.RefreshTokens.Any(x => x.IsActive))
            {
                var activeToken = user.RefreshTokens.First(x => x.IsActive);
                authResponse.RefreshToken = activeToken.Token;
                authResponse.RefreshTokenExpiration = activeToken.ExpiredOn;
            }
            else
            {
                var newRefreshToken = GenerateRefreshToken();
                authResponse.RefreshToken = newRefreshToken.Token;
                authResponse.RefreshTokenExpiration = newRefreshToken.ExpiredOn;
                
                if (user.RefreshTokens == null)
                    user.RefreshTokens = new List<RefreshToken>();

                user.RefreshTokens.Add(newRefreshToken);
                await _userManager.UpdateAsync(user);
            }

            return authResponse;
        }

        public async Task<AuthenticationResponse> ExternalLoginAsync(
            string provider, string providerKey, string email, string? name)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new AuthenticationResponse
                {
                    Message = "Email was not returned by the external provider.",
                    IsAuthenticated = false
                };

            var user = await _userManager.FindByLoginAsync(provider, providerKey);

            if (user is null)
            {
                user = await _userManager.FindByEmailAsync(email);

                if (user is null)
                {
                    var userName = await GenerateUniqueUserNameAsync(name ?? email.Split('@')[0]);

                    user = new ApplicationUser
                    {
                        UserName = userName,
                        Email = email,
                        FullName = name ?? userName,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return new AuthenticationResponse
                        {
                            Message = string.Join(" | ", createResult.Errors.Select(e => e.Description)),
                            IsAuthenticated = false
                        };

                    const string roleName = "USER";
                    if (!await _roleManager.RoleExistsAsync(roleName))
                        await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });

                    await _userManager.AddToRoleAsync(user, roleName);
                }
                else if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                var addLoginResult = await _userManager.AddLoginAsync(
                    user, new UserLoginInfo(provider, providerKey, provider));

                if (!addLoginResult.Succeeded &&
                    addLoginResult.Errors.All(e => e.Code != "LoginAlreadyAssociated"))
                {
                    return new AuthenticationResponse
                    {
                        Message = string.Join(" | ", addLoginResult.Errors.Select(e => e.Description)),
                        IsAuthenticated = false
                    };
                }
            }

            if (user.IsSuspended)
                return new AuthenticationResponse
                {
                    Message = $"Account is suspended. Reason: {user.SuspendReason ?? "Contact technical support."}",
                    ErrorField = "account",
                    IsAuthenticated = false
                };

            var authResponse = await GenerateJwtToken(user);

            if (user.RefreshTokens != null && user.RefreshTokens.Any(x => x.IsActive))
            {
                var activeToken = user.RefreshTokens.First(x => x.IsActive);
                authResponse.RefreshToken = activeToken.Token;
                authResponse.RefreshTokenExpiration = activeToken.ExpiredOn;
            }
            else
            {
                var newRefreshToken = GenerateRefreshToken();
                authResponse.RefreshToken = newRefreshToken.Token;
                authResponse.RefreshTokenExpiration = newRefreshToken.ExpiredOn;

                if (user.RefreshTokens == null)
                    user.RefreshTokens = new List<RefreshToken>();

                user.RefreshTokens.Add(newRefreshToken);
                await _userManager.UpdateAsync(user);
            }

            return authResponse;
        }

        private async Task<string> GenerateUniqueUserNameAsync(string baseName)
        {
            var sanitized = new string(baseName
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
                .ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "user";

            var userName = sanitized;
            var suffix = 1;
            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{sanitized}{suffix}";
                suffix++;
            }

            return userName;
        }

        public async Task<AuthenticationResponse> RefreshTokenAsync(string token)
        {
            var user = _userManager.Users
                .SingleOrDefault(x => x.RefreshTokens.Any(rt => rt.Token == token));

            if (user == null)
                return new AuthenticationResponse { Message = "Invalid token.", IsAuthenticated = false };

            var refreshToken = user.RefreshTokens.Single(rt => rt.Token == token);

            if (!refreshToken.IsActive)
                return new AuthenticationResponse { Message = "The token is invalid or expired.", IsAuthenticated = false };

            refreshToken.RevokedOn = DateTime.UtcNow;
            var newRefreshToken = GenerateRefreshToken();
            
            if (user.RefreshTokens == null)
                user.RefreshTokens = new List<RefreshToken>();

            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            var authResponse = await GenerateJwtToken(user);
            authResponse.RefreshToken = newRefreshToken.Token;
            authResponse.RefreshTokenExpiration = newRefreshToken.ExpiredOn;

            return authResponse;
        }

        public async Task RevokeTokenAsync(string token)
        {
            var user = _userManager.Users
                .SingleOrDefault(x => x.RefreshTokens.Any(rt => rt.Token == token));

            if (user == null)
                throw new BadRequestException("Invalid token.");

            var refreshToken = user.RefreshTokens.Single(rt => rt.Token == token);

            if (!refreshToken.IsActive)
                throw new BadRequestException("The token is invalid or expired.");

            refreshToken.RevokedOn = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var activeTokens = user.RefreshTokens.Where(rt => rt.IsActive).ToList();
            foreach (var t in activeTokens)
                t.RevokedOn = DateTime.UtcNow;

            if (activeTokens.Any())
                await _userManager.UpdateAsync(user);

            return true;
        }

        public async Task<AuthenticationResponse> GenerateJwtToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("uid", user.Id.ToString())
        }
            .Union(userClaims)
            .Union(roleClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwt.DurationInDays),
                signingCredentials: creds);

            return new AuthenticationResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                Email = user.Email,
                Username = user.UserName,
                Roles = roles.ToList(),
                Message = "Operation completed successfully.",
                IsAuthenticated = true
            };
        }

        private static RefreshToken GenerateRefreshToken()
        {
            byte[] bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return new RefreshToken
            {
                CreatedOn = DateTime.UtcNow,
                ExpiredOn = DateTime.UtcNow.AddDays(10),
                Token = Convert.ToBase64String(bytes)
            };
        }

        // ========================= Register ========================= //

        public async Task RegisterAsync(RegisterDTO registerDTO)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDTO.Email);

            if (existingUser != null)
            {
                if (!existingUser.EmailConfirmed)
                {
                    existingUser.UserName = registerDTO.UserName;
                    await _userManager.UpdateAsync(existingUser);
                    return; // سيتم إرسال OTP من الكونترولر
                }

                throw new ConflictException("Email is already registered.");
            }

            var userName = registerDTO.UserName;
            var existingUserByName = await _userManager.FindByNameAsync(userName);

            if (existingUserByName != null)
            {
                // إذا كان الاسم مأخوذاً وغير مفعل لنفس الإيميل، نمسحه (المنطق القديم)
                if (existingUserByName.Email == registerDTO.Email && !existingUserByName.EmailConfirmed)
                {
                    await _userManager.DeleteAsync(existingUserByName);
                }
                else
                {
                    // توليد اسم مستخدم فريد بإضافة رقم
                    int suffix = 1;
                    while (await _userManager.FindByNameAsync($"{userName}{suffix}") != null)
                    {
                        suffix++;
                    }
                    userName = $"{userName}{suffix}";
                }
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = registerDTO.Email,
                FullName = userName, // Set FullName to userName to satisfy required field
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(" | ", result.Errors.Select(e => e.Description)));

            const string roleName = "USER";
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });

            await _userManager.AddToRoleAsync(user, roleName);
        }

        // ========================= Role Management ========================= //

        public async Task AddRoleToUserAsync(AddRoleDTO model)
        {
            var role = await _roleManager.FindByNameAsync(model.RoleName);
            if (role == null)
            {
                var createResult = await _roleManager.CreateAsync(
                    new ApplicationRole { Name = model.RoleName });

                if (!createResult.Succeeded)
                    throw new BadRequestException(
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            var user = await _userManager.FindByIdAsync(model.UserID.ToString())
                ?? throw new NotFoundException("User not found.");

            if (await _userManager.IsInRoleAsync(user, model.RoleName))
                throw new ConflictException("User already has this role.");

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);

            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<IEnumerable<ApplicationRole>> GetAllRolesAsync()
            => _roleManager.Roles.ToList();

        public async Task DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName)
                ?? throw new NotFoundException("Role not found.");

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // ========================= User Management ========================= //

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
            => _userManager.Users.ToList();

        public async Task<ApplicationUser?> GetUserByIdAsync(string id)
            => await _userManager.FindByIdAsync(id);

        public async Task UpdateUserAsync(string id, UpdateUserDTO dto)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new NotFoundException("User not found.");

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                        .IsValid(dto.Email))
                    throw new BadRequestException("Email address format is invalid.");

                var existing = await _userManager.FindByEmailAsync(dto.Email);
                if (existing != null && existing.Id != user.Id)
                    throw new ConflictException("Email is already registered.");
            }

            if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
            {
                var existing = await _userManager.FindByNameAsync(dto.UserName);
                if (existing != null && existing.Id != user.Id)
                    throw new ConflictException("Username is already taken.");
            }

            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!resetResult.Succeeded)
                    throw new BadRequestException(
                        string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }

            user.Email = dto.Email ?? user.Email;
            user.UserName = dto.UserName ?? user.UserName;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new NotFoundException("User not found.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(e => e.Description)));
        }

        // ========================= Encapsulated Identity Operations ========================= //

        public async Task ResendOtpAsync(ResendOtpDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new NotFoundException("User not found.");

            if (user.EmailConfirmed)
                throw new BadRequestException("Email is already confirmed.");

            await _otpService.SendOtpAsync(dto.Email);
        }

        public async Task<AuthenticationResponse> VerifyOtpAsync(VerifyOtpDTO dto)
        {
            var error = await _otpService.ValidateOtpAsync(dto.Email, dto.Code);
            if (error != null)
                throw new BadRequestException(error);

            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new NotFoundException("User not found.");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Send Welcome Email
            try
            {
                var welcomeHtml = $"<h1>Welcome to our platform, {user.UserName ?? user.Email}!</h1><p>Your email has been successfully verified.</p>";
                await _mailService.SendMessageAsync(user.Email!, "Welcome to our platform ♻️", welcomeHtml, null);
            }
            catch { /* Email fail shouldn't block verification */ }

            var authResponse = await GenerateJwtToken(user);

            var newRefreshToken = GenerateRefreshToken();
            if (user.RefreshTokens == null)
                user.RefreshTokens = new List<RefreshToken>();

            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            authResponse.RefreshToken = newRefreshToken.Token;
            authResponse.RefreshTokenExpiration = newRefreshToken.ExpiredOn;

            return authResponse;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email!)
                ?? throw new NotFoundException("Email not found.");

            await _otpService.SendOtpAsync(dto.Email!);
        }

        public async Task<AuthenticationResponse> ResetPasswordAsync(ResetPasswordDTO dto)
        {
            // 1. Verify OTP
            var error = await _otpService.ValidateOtpAsync(dto.Email, dto.Code);
            if (error != null)
                throw new BadRequestException(error);

            // 2. Find User
            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new NotFoundException("User not found.");

            // 3. Generate Reset Token dynamically
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 4. Reset Password
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(e => e.Description)));

            // 5. Generate Tokens
            var authResponse = await GenerateJwtToken(user);

            var newRefreshToken = GenerateRefreshToken();
            if (user.RefreshTokens == null)
                user.RefreshTokens = new List<RefreshToken>();

            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            authResponse.RefreshToken = newRefreshToken.Token;
            authResponse.RefreshTokenExpiration = newRefreshToken.ExpiredOn;

            return authResponse;
        }

        public async Task<IEnumerable<UserResponseDTO>> GetAllUsersWithRoleUserAsync()
        {
            var users = await GetAllUsersAsync();
            var userDTOs = new List<UserResponseDTO>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!roles.Contains("USER")) continue;

                userDTOs.Add(new UserResponseDTO
                {
                    Id = u.Id.ToString(),
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt,
                    EmailConfirmed = u.EmailConfirmed
                });
            }

            return userDTOs;
        }

        public async Task<UserResponseDTO> GetUserByIdWithRoleUserAsync(string id)
        {
            var user = await GetUserByIdAsync(id)
                ?? throw new NotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("USER"))
                throw new NotFoundException("User not found.");

            return new UserResponseDTO
            {
                Id = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                EmailConfirmed = user.EmailConfirmed
            };
        }

        public async Task DeleteAccountByOtpAsync(DeleteAccountDTO dto)
        {
            var error = await _otpService.ValidateOtpAsync(dto.Email, dto.Code);
            if (error != null)
                throw new BadRequestException(error);

            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new NotFoundException("User not found.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(e => e.Description)));
        }

        public async Task DeleteAccountByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new NotFoundException("User not found.");

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(" | ", result.Errors.Select(e => e.Description)));
        }

        public async Task<MeResponseDTO> GetMeByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new NotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            return new MeResponseDTO
            {
                IsAuthenticated = true,
                UserId = user.Id.ToString(),
                Name = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                RememberMe = user.RefreshTokens.Any(rt => rt.IsActive),
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
