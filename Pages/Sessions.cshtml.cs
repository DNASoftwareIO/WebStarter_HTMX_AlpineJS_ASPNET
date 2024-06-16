using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DNA.Pages;

[Authorize]
public class SessionsModel(ApplicationDbContext context) : PageModel
{
  private readonly ApplicationDbContext _context = context;
  public IList<Session> Sessions { get; set; }
  public string RefreshToken { get; set; }

  public async Task<ActionResult> OnGet()
  {
    var userId = User.GetId();

    Sessions = await _context.Sessions.Where(x => x.UserId == userId && x.ExpiredAt > DateTime.UtcNow).OrderByDescending(x => x.CreatedAt).ToListAsync();

    RefreshToken = Request.Cookies["refreshToken"];

    return Page();
  }
}
