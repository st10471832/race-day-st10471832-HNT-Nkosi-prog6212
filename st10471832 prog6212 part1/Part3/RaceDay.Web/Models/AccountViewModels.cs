using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = "Participant";

    [Required(ErrorMessage = "Please enter your full name.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a password.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Display(Name = "Date of birth")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }
}