using Htmx;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DNA.Pages;

public class ForgotPasswordModel(UserManager<User> userManager, EmailService emailService) : PageModel
{
  private readonly UserManager<User> _userManager = userManager;
  private readonly EmailService _emailService = emailService;
  public string ErrorMessage { get; set; }
  public bool LinkSent { get; set; }

  [BindProperty]
  public ForgotPasswordCommand Cmd { get; set; }

  public ActionResult OnGet()
  {
    return Request.IsHtmx() ? Partial("~/Pages/Components/_ForgotPasswordForm.cshtml", this) : Page();
  }

  public async Task<ActionResult> OnPost()
  {
    if (!ModelState.IsValid)
    {
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ForgotPasswordForm.cshtml", this) : Page();
    }

    var user = await _userManager.FindByEmailAsync(Cmd.Email);
    if (user == null)
    {
      // return ok so that user doesn't learn that email is used by someone
      LinkSent = true;
      return Request.IsHtmx() ? Partial("~/Pages/Components/_ForgotPasswordForm.cshtml", this) : Page();
    }

    // TODO throttle so that same email is not sent multiple times
    await _emailService.SendPasswordResetToken(user);

    LinkSent = true;
    return Request.IsHtmx() ? Partial("~/Pages/Components/_ForgotPasswordForm.cshtml", this) : Page();
  }
}
