using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DNA.Pages;

public class ResetPasswordModel(UserManager<User> userManager, ApplicationDbContext context) : PageModel
{
  private readonly UserManager<User> _userManager = userManager;
  private readonly ApplicationDbContext _context = context;
  public string? ErrorMessage { get; set; }
  public bool ShowSuccess { get; set; }

  [BindProperty]
  public ResetPasswordCommand Cmd { get; set; }

  public ActionResult OnGet([FromQuery] string token, [FromQuery] string email)
  {
    Cmd = new ResetPasswordCommand { Email = email, Token = token };
    return Page();
  }

  public async Task<ActionResult> OnPost()
  {
    if (!ModelState.IsValid)
    {
      return Page();
    }

    if (Cmd.NewPassword != Cmd.ConfirmPassword)
    {
      ErrorMessage = "Passwords do not match";
      return Page();
    }

    var user = await _userManager.FindByEmailAsync(Cmd.Email);
    if (user == null)
    {
      // TODO do we need to do anything extra here to protect against enumeration attacks on this endpoint?
      ErrorMessage = "Error resetting password. Please try again later.";
      return Page();
    }

    Cmd.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Cmd.Token));

    var result = await _userManager.ResetPasswordAsync(user, Cmd.Token, Cmd.NewPassword);
    if (!result.Succeeded)
    {
      ErrorMessage = "Error resetting password. Please try again later.";
      return Page();
    }

    await _context.SaveChangesAsync();

    ShowSuccess = true;

    return Page();
  }
}
