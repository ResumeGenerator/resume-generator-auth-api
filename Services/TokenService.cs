using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using ResumeGenerator.AuthApi.Models;

namespace ResumeGenerator.AuthApi.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public async Task<string> CreateTokenAsync(ApplicationUser user, IList<string>? roles = null)
    {
        var key = GetRequiredJwtSetting("Jwt:Key", "JWT_KEY");
        var issuer = GetRequiredJwtSetting("Jwt:Issuer", "JWT_ISSUER");
        var audience = GetRequiredJwtSetting("Jwt:Audience", "JWT_AUDIENCE");
        var expiryInMinutes = GetJwtExpiryInMinutes();

        var claims = new List<Claim>
        {
            // IdentityUser.Id is used as the primary subject (usually a GUID string)
            new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? ""),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
        };

        // Add explicit GUID claim based on ASP.NET Identity Id
        if (!string.IsNullOrEmpty(user.Id))
        {
            if (Guid.TryParse(user.Id, out var parsedGuid))
                claims.Add(new Claim("user_guid", parsedGuid.ToString()));
            else
                claims.Add(new Claim("user_guid", user.Id));
        }

        roles ??= await _userManager.GetRolesAsync(user);
        foreach (var r in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, r));
        }

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetRequiredJwtSetting(string configKey, string envKey)
    {
        var value = Environment.GetEnvironmentVariable(envKey)
            ?? Environment.GetEnvironmentVariable(configKey.Replace(":", "__"))
            ?? _config[configKey];

        if (string.IsNullOrWhiteSpace(value))
            throw new Exception($"{envKey} env var, {configKey.Replace(":", "__")} env var, or {configKey} config is required");

        return value;
    }

    private int GetJwtExpiryInMinutes()
    {
        var value = Environment.GetEnvironmentVariable("JWT_EXPIRY_IN_MINUTES")
            ?? Environment.GetEnvironmentVariable("Jwt__ExpiryInMinutes")
            ?? _config["Jwt:ExpiryInMinutes"]
            ?? "60";

        if (!int.TryParse(value, out var expiryInMinutes) || expiryInMinutes <= 0)
            throw new Exception("JWT_EXPIRY_IN_MINUTES env var, Jwt__ExpiryInMinutes env var, or Jwt:ExpiryInMinutes config must be a positive whole number");

        return expiryInMinutes;
    }
}
