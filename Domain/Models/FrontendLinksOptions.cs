using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class FrontendLinksOptions
{
    public const string SectionName = "FrontendLinks";

    [Required]
    public required string BaseUrl { get; set; }

    public string ResetPasswordPath { get; set; } = "reset-password";
    public string ConfirmEmailPath { get; set; } = "confirm-email";
}
