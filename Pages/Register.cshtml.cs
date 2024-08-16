using System.ComponentModel.DataAnnotations;
using Htmx;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DNA.Pages;

public class RegisterModel(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext context, SessionService sessionService, EmailService emailService) : PageModel
{
  private readonly UserManager<User> _userManager = userManager;
  private readonly SignInManager<User> _signInManager = signInManager;
  private readonly ApplicationDbContext _context = context;
  private readonly EmailService _emailService = emailService;
  private readonly SessionService _sessionService = sessionService;
  public string ErrorMessage { get; set; }

  [BindProperty]
  public RegisterUserCommand Cmd { get; set; }

  [BindProperty]
  [Required]
  public bool TermsChecked { get; set; }

  public ActionResult OnGet()
  {
    return Request.IsHtmx() ? Partial("~/Pages/Components/_RegisterForm.cshtml", this) : Page();
  }

  public async Task<IActionResult> OnPost()
  {
    if (!TermsChecked)
    {
      ModelState.AddModelError("TermsChecked", "You must agree to the terms.");
    }

    if (!ModelState.IsValid)
    {
      return Request.IsHtmx() ? Partial("~/Pages/Components/_RegisterForm.cshtml", this) : Page();
    }

    var user = await _userManager.FindByNameAsync(Cmd.UserName.Trim());
    if (user != null)
    {
      // Don't return username/email already used messages to prevent account enumeration attacks
      ErrorMessage = "Error registering account. Please try again later.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_RegisterForm.cshtml", this) : Page();
    }

    user = await _userManager.FindByEmailAsync(Cmd.Email.Trim());
    if (user != null)
    {
      ErrorMessage = "Error registering account. Please try again later.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_RegisterForm.cshtml", this) : Page();
    }

    user = new User
    {
      UserName = Cmd.UserName.Trim(),
      Email = Cmd.Email.Trim().ToLower(),
      DateJoined = DateTime.UtcNow,
      PromoCode = Cmd.PromoCode
    };

    var result = await _userManager.CreateAsync(user, Cmd.Password);
    if (!result.Succeeded)
    {
      ErrorMessage = "Error registering account. Please try again later.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_RegisterForm.cshtml", this) : Page();
    }

    var session = await _sessionService.CreateSession(user.Id);

    await _context.SaveChangesAsync();

    await _signInManager.SignInAsync(user, true);

    var jwt = _sessionService.CreateJwt(user);

    var cookieOptions = new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      Expires = DateTime.UtcNow.AddDays(30),
      SameSite = SameSiteMode.Strict
    };
    Response.Cookies.Append("accessToken", jwt, cookieOptions);
    Response.Cookies.Append("refreshToken", session.RefreshToken.ToString(), cookieOptions);
    Response.Headers.Append("HX-Trigger", "close-auth-modal");

    await _emailService.SendEmailConfirmationToken(user);

    return Request.IsHtmx() ? Partial("~/Pages/Shared/_Nav.cshtml") : Page();
  }
}
