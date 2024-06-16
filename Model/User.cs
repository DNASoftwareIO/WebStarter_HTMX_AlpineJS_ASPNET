using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<Guid>
{
  public bool Deleted { get; set; }
  public DateTime DateJoined { get; set; }
  public string? PromoCode { get; set; }
}

public static class ClaimsPrincipalExtensions
{
  public static Guid GetId(this ClaimsPrincipal user)
  {
    var id = user.FindFirst(ClaimTypes.NameIdentifier) ?? throw new ArgumentNullException();
    return new Guid(id.Value);
  }
}
