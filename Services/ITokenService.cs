using ResumeGenerator.AuthApi.Models;

namespace ResumeGenerator.AuthApi.Services;

public interface ITokenService
{
    Task<string> CreateTokenAsync(ApplicationUser user, IList<string>? roles = null);
}