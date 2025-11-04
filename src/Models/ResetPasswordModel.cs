using System.ComponentModel.DataAnnotations;

namespace StyleMatch.Models;

public class ResetPasswordModel
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; }

    [Required, StringLength(100)]
    public string NewPassword { get; set; }
}