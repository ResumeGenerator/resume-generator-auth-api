using Microsoft.AspNetCore.Identity;

namespace ResumeGenerator.AuthApi.Models;

public class ApplicationUser : IdentityUser
{
    // Add custom properties if needed
    public string? DisplayName { get; set; }
}