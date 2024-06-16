using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DNA.Pages;

[Authorize]
public class SignOutModel(SignInManager<User> signInManager, ApplicationDbContext context) : PageModel
{
  private readonly SignInManager<User> _signInManager = signInManager;
  private readonly ApplicationDbContext _context = context;

  public ActionResult OnGet()
  {
    return Page();
  }

  public async Task<IActionResult> OnPost()
  {
    var userId = User.GetId();

    var refreshToken = Request.Cookies["refreshToken"];
    var session = await _context.Sessions.FirstOrDefaultAsync(x => x.UserId == userId && x.RefreshToken.ToString() == refreshToken && x.ExpiredAt > DateTime.UtcNow);
    if (session != null)
    {
      session.ExpiredAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();
    }

    await _signInManager.SignOutAsync();

    Response.Cookies.Delete("accessToken");
    Response.Cookies.Delete("refreshToken");

    return LocalRedirect("/");
  }

  public async Task<IActionResult> OnPostById([FromQuery] string id)
  {
    var userId = User.GetId();

    var session = await _context.Sessions.FirstOrDefaultAsync(x => x.Id.ToString() == id && x.UserId == userId);
    if (session == null) return LocalRedirect("/sessions");

    var refreshToken = Request.Cookies["refreshToken"];
    if (session.RefreshToken.ToString() == refreshToken) LocalRedirect("/sessions");

    session.ExpiredAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return LocalRedirect("/sessions");
  }

  public async Task<IActionResult> OnPostAll()
  {
    var userId = User.GetId();

    var sessions = await _context.Sessions.Where(x => x.UserId == userId && x.ExpiredAt > DateTime.UtcNow).ToListAsync();

    foreach (var session in sessions)
    {
      session.ExpiredAt = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();

    await _signInManager.SignOutAsync();

    Response.Cookies.Delete("accessToken");
    Response.Cookies.Delete("refreshToken");

    return LocalRedirect("/");
  }
}
