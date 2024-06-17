using System.ComponentModel.DataAnnotations;

public class RegisterUserCommand
{
  [Required(ErrorMessage = "*")]
  public required string UserName { get; set; }
  [Required(ErrorMessage = "*")]
  public required string Password { get; set; }
  [Required(ErrorMessage = "*")]
  public required string Email { get; set; }
  public string? PromoCode { get; set; }
}

public class LoginUserCommand
{
  [Required(ErrorMessage = "*")]
  public required string UserName { get; set; }
  [Required(ErrorMessage = "*")]
  public required string Password { get; set; }
  public string? TfaCode { get; set; }
}

public class ForgotPasswordCommand
{
  [Required(ErrorMessage = "*")]
  public required string Email { get; set; }
}

public class ResetPasswordCommand
{
  [Required]
  public required string Token { get; set; }

  [Required(ErrorMessage = "*")]
  public required string NewPassword { get; set; }

  [Required(ErrorMessage = "*")]
  public required string ConfirmPassword { get; set; }

  [Required]
  public required string Email { get; set; }
}

public class ChangePasswordCommand
{
  [Required(ErrorMessage = "*")]
  public required string OldPassword { get; set; }

  [Required(ErrorMessage = "*")]
  public required string NewPassword { get; set; }

  [Required(ErrorMessage = "*")]
  public required string ConfirmPassword { get; set; }

  public string? TfaCode { get; set; }
}

public class ToggleTfaCommand
{
  [Required(ErrorMessage = "*")]
  public required string TfaCode { get; set; }
}
