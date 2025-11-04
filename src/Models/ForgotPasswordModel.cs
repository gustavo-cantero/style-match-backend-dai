using System.ComponentModel.DataAnnotations;

namespace StyleMatch.Models;

public class ForgotPasswordModel
{
    [Required, EmailAddress]
    public string Email { get; set; }
}
