using System.ComponentModel.DataAnnotations;

// Disabling warning because all properties that have warnings have a Required attribute and won't be null when used in a controller.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class RegisterUserCommand
{
  [Required(ErrorMessage = "*")]
  public string UserName { get; set; }
  [Required(ErrorMessage = "*")]
  public string Password { get; set; }
  [Required(ErrorMessage = "*")]
  public string Email { get; set; }
  public string? PromoCode { get; set; }
}

public class LoginUserCommand
{
  [Required(ErrorMessage = "*")]
  public string UserName { get; set; }
  [Required(ErrorMessage = "*")]
  public string Password { get; set; }
  public string? TfaCode { get; set; }
}

public class ForgotPasswordCommand
{
  [Required(ErrorMessage = "*")]
  public string Email { get; set; }
}

public class ResetPasswordCommand
{
  [Required]
  public string Token { get; set; }

  [Required(ErrorMessage = "*")]
  public string NewPassword { get; set; }

  [Required(ErrorMessage = "*")]
  public string ConfirmPassword { get; set; }

  [Required]
  public string Email { get; set; }
}

public class ChangePasswordCommand
{
  [Required(ErrorMessage = "*")]
  public string OldPassword { get; set; }

  [Required(ErrorMessage = "*")]
  public string NewPassword { get; set; }

  [Required(ErrorMessage = "*")]
  public string ConfirmPassword { get; set; }

  public string? TfaCode { get; set; }
}

public class ToggleTfaCommand
{
  [Required(ErrorMessage = "*")]
  public string TfaCode { get; set; }
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
