using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DNA.Pages;

public class ConfirmEmailModel(UserManager<User> userManager, ApplicationDbContext context) : PageModel
{
  private readonly UserManager<User> _userManager = userManager;
  private readonly ApplicationDbContext _context = context;
  public string ErrorMessage { get; set; }
  public bool ShowSuccess { get; set; }

  public async Task<ActionResult> OnGet([FromQuery] Guid userId, [FromQuery] string token)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user == null)
    {
      ErrorMessage = "User not found.";
      return Page();
    }

    token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

    var result = await _userManager.ConfirmEmailAsync(user, token);
    if (!result.Succeeded)
    {
      ErrorMessage = "Token invalid.";
      return Page();
    }

    await _context.SaveChangesAsync();
    ShowSuccess = true;
    return Page();
  }
}
