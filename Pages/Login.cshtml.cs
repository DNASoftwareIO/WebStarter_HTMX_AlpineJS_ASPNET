using Htmx;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DNA.Pages;

public class LoginModel(ILogger<LoginModel> logger, UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext context, IConfiguration configuration, SessionService sessionService) : PageModel
{
  private readonly UserManager<User> _userManager = userManager;
  private readonly SignInManager<User> _signInManager = signInManager;
  private readonly SessionService _sessionService = sessionService;
  private readonly ApplicationDbContext _context = context;
  public string ErrorMessage { get; set; }

  [BindProperty]
  public LoginUserCommand Cmd { get; set; }

  [BindProperty]
  public bool TfaRequired { get; set; }

  public ActionResult OnGet()
  {
    return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
  }

  public async Task<IActionResult> OnPost()
  {
    if (!ModelState.IsValid)
    {
      return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
    }

    var user = await _userManager.FindByNameAsync(Cmd.UserName.Trim());
    if (user == null)
    {
      // Don't return username/email already used messages to prevent account enumeration attacks
      ErrorMessage = "Error logging in. Please try again later.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
    }

    var result = await _userManager.CheckPasswordAsync(user, Cmd.Password);
    if (!result)
    {
      ErrorMessage = "Error logging in. Please try again later.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
    }

    if (user.Deleted)
    {
      ErrorMessage = "Error logging in. Please try again later.";
      return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
    }

    if (user.TwoFactorEnabled)
    {
      TfaRequired = true;

      if (string.IsNullOrEmpty(Cmd.TfaCode))
      {
        ErrorMessage = "Two factor code required.";
        return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
      }

      // TODO check if tfa code has already been used!
      var tfaResult = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, Cmd.TfaCode);
      if (!tfaResult)
      {
        ErrorMessage = "Invalid tfa code.";
        return Request.IsHtmx() ? Partial("~/Pages/Components/_LoginForm.cshtml", this) : Page();
      }
    }

    await _signInManager.SignInAsync(user, true);

    var session = await _sessionService.CreateSession(user.Id);

    await _context.SaveChangesAsync();

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
    Response.Headers.Append("HX-Trigger", "close-login-modal");

    return Request.IsHtmx() ? Partial("~/Pages/Shared/_Nav.cshtml") : Page();
  }
}
