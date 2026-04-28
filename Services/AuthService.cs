using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BelarusHeritage.Services;

public class AuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        AppDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    public async Task<(User? User, string[] Errors)> RegisterAsync(string email, string username, string password)
    {
        if (await _userManager.FindByEmailAsync(email) != null)
            return (null, new[] { "Email already exists" });

        if (await _userManager.FindByNameAsync(username) != null)
            return (null, new[] { "Username already exists" });

        var user = new User
        {
            Email = email,
            UserName = username,
            DisplayName = username,
            PreferredLang = "ru",
            Role = "user"
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (null, result.Errors.Select(e => e.Description).ToArray());

        await _userManager.AddToRoleAsync(user, "user");
        await GenerateEmailVerificationTokenAsync(user);

        return (user, Array.Empty<string>());
    }

    public async Task<string?> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return null;

        if (!result.Succeeded)
            return null;

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return GenerateJwtToken(user);
    }

    public async Task GenerateEmailVerificationTokenAsync(User user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        _context.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Type = TokenType.EmailVerify,
            Token = token,
            ExpiresAt = expiresAt
        });

        await _context.SaveChangesAsync();
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var userToken = await _context.UserTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token &&
                t.Type == TokenType.EmailVerify &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow);

        if (userToken == null)
            return false;

        var result = await _userManager.ConfirmEmailAsync(userToken.User!, token);
        if (!result.Succeeded)
            return false;

        userToken.UsedAt = DateTime.UtcNow;
        userToken.User!.EmailConfirmed = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _context.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Type = TokenType.PasswordReset,
            Token = token,
            ExpiresAt = expiresAt
        });

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var userToken = await _context.UserTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token &&
                t.Type == TokenType.PasswordReset &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow);

        if (userToken == null)
            return false;

        var result = await _userManager.ResetPasswordAsync(userToken.User!, token, newPassword);
        if (!result.Succeeded)
            return false;

        userToken.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<User?> UpdateProfileAsync(int userId, string? displayName, string? preferredLang)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return null;

        if (displayName != null)
            user.DisplayName = displayName;

        if (preferredLang != null)
            user.PreferredLang = preferredLang;

        await _userManager.UpdateAsync(user);
        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword).ContinueWith(r => r.Result.Succeeded);
    }

    private string GenerateJwtToken(User user)
    {
        var secretKey = _configuration["App:JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = _configuration["App:JwtSettings:Issuer"] ?? "BelarusHeritage";
        var audience = _configuration["App:JwtSettings:Audience"] ?? "BelarusHeritageUsers";
        var expiryMinutes = int.Parse(_configuration["App:JwtSettings:ExpiryMinutes"] ?? "10080");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? $"user-{user.Id}"),
            new Claim(ClaimTypes.Role, user.Role ?? "user")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
