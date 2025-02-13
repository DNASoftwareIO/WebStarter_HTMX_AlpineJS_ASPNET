using Htmx;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace DNA.Pages;

[Authorize]
public class SecurityModel(UserManager<User> userManager, IConfiguration configuration) : PageModel
{
  private readonly UserManager<User> _userManager = userManager;
  private readonly IConfiguration _configuration = configuration;
  public bool ChangePasswordSuccess { get; set; }
  public string? PasswordErrorMessage { get; set; }
  public bool TfaEnabled { get; set; }
  public bool ToggleTfaSuccess { get; set; }
  public string? TfaErrorMessage { get; set; }
  public string? TfaKey { get; set; }
  public ChangePasswordCommand ChangePasswordCmd { get; set; }
  public ToggleTfaCommand ToggleTfaCmd { get; set; }
  public string QrCodeData { get; set; }

  // TODO This can probably be written better, it was copied from an old .net project which may have copied from SO. Asp .NET Core may have a better way to handle
  private static string FormatTfaKey(string unformattedKey)
  {
    var result = new StringBuilder();
    var currentPosition = 0;
    while (currentPosition + 4 < unformattedKey.Length)
    {
      result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
      currentPosition += 4;
    }

    if (currentPosition < unformattedKey.Length)
    {
      result.Append(unformattedKey.Substring(currentPosition));
    }

    return result.ToString().ToLowerInvariant();
  }

  private static string GenerateQrCode(string text)
  {
    var qrGenerator = new QRCodeGenerator();
    var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.H);
    var qrCode = new Base64QRCode(qrCodeData);
    string qrCodeImageAsBase64 = qrCode.GetGraphic(20);
    return string.Format("data:image/png;base64,{0}", qrCodeImageAsBase64);
  }

  private async Task SetQrData()
  {
    var userId = User.GetId();
    var user = await _userManager.FindByIdAsync(userId.ToString());

    // don't return key to someone who has tfa enabled as an attacker could use to disable
    if (!user.TwoFactorEnabled)
    {
      var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
      if (string.IsNullOrEmpty(unformattedKey))
      {
        await _userManager.ResetAuthenticatorKeyAsync(user);
        unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
      }

      
      var authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}";

      var r = string.Format(authenticatorUriFormat, _configuration["WebClientUrl"], user.UserName, unformattedKey);
      QrCodeData = GenerateQrCode(r);
      TfaKey = FormatTfaKey(unformattedKey);
    }

    TfaEnabled = user.TwoFactorEnabled;
  }

  public async Task<ActionResult> OnGet()
  {
    await SetQrData();
    return Page();
  }

  public async Task<IActionResult> OnPostToggleTfa(ToggleTfaCommand ToggleTfaCmd)
  {
    if (!ModelState.IsValid)
    {
      await SetQrData();
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ToggleTfaForm.cshtml", this) : Page();
    }

    var userId = User.GetId();

    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user == null)
    {
      return LocalRedirect("/");
    }

    var verifyResult = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, ToggleTfaCmd.TfaCode);
    if (!verifyResult)
    {
      TfaErrorMessage = "Error toggling Tfa. Please try again later.";
      await SetQrData();
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ToggleTfaForm.cshtml", this) : Page();
    }

    var setResult = await _userManager.SetTwoFactorEnabledAsync(user, !user.TwoFactorEnabled);
    if (!setResult.Succeeded)
    {
      TfaErrorMessage = "Error toggling Tfa. Please try again later.";
      await SetQrData();
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ToggleTfaForm.cshtml", this) : Page();
    }

    ModelState.Clear();

    await SetQrData();

    ToggleTfaSuccess = true;


    return Request.IsHtmx() ? Partial("~/Pages/Components/_ToggleTfaForm.cshtml", this) : Page();
  }

  public async Task<IActionResult> OnPostChangePassword(ChangePasswordCommand ChangePasswordCmd)
  {
    if (!ModelState.IsValid)
    {
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ChangePasswordForm.cshtml", this) : Page();
    }

    if (ChangePasswordCmd.NewPassword != ChangePasswordCmd.ConfirmPassword)
    {
      PasswordErrorMessage = "Passwords do not match";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ChangePasswordForm.cshtml", this) : Page();
    }

    var user = await _userManager.FindByIdAsync(User.GetId().ToString());
    if (user == null)
    {
      PasswordErrorMessage = "Error changing password. Please try again later.";
      return LocalRedirect("/");
    }

    if (user.TwoFactorEnabled)
    {
      TfaEnabled = true;

      if (string.IsNullOrEmpty(ChangePasswordCmd.TfaCode))
      {
        PasswordErrorMessage = "tfa required";
        return Request.IsHtmx() ? Partial("~/Pages/Components/_ChangePasswordForm.cshtml", this) : Page();
      }

      // TODO check tfa code has not been used
      var tfaResult = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, ChangePasswordCmd.TfaCode);
      if (!tfaResult)
      {
        PasswordErrorMessage = "tfa code invalid";
        return Request.IsHtmx() ? Partial("~/Pages/Components/_ChangePasswordForm.cshtml", this) : Page();
      }
    }

    var result = await _userManager.ChangePasswordAsync(user, ChangePasswordCmd.OldPassword, ChangePasswordCmd.NewPassword);
    if (!result.Succeeded)
    {
      PasswordErrorMessage = "Error changing password.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ChangePasswordForm.cshtml", this) : Page();
    }

    ModelState.Clear();
    ChangePasswordSuccess = true;

    return Request.IsHtmx() ? Partial("~/Pages/Components/_ChangePasswordForm.cshtml", this) : Page();
  }
}
