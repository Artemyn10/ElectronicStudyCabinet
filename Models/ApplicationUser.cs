using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ElectronicStudyCabinet.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}