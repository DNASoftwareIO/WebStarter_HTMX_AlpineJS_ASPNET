using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public class SessionService
{
  private readonly ApplicationDbContext _context;
  private readonly IConfiguration _configuration;
  private readonly UserManager<User> _userManager;
  private readonly SignInManager<User> _signInManager;

  public SessionService(ApplicationDbContext context, IConfiguration configuration, UserManager<User> userManager, SignInManager<User> signInManager)
  {
    _context = context;
    _configuration = configuration;
    _userManager = userManager;
    _signInManager = signInManager;
  }

  public async Task<Session> CreateSession(Guid userId)
  {
    var session = new Session
    {
      Id = Guid.NewGuid(),
      RefreshToken = Guid.NewGuid(),
      UserId = userId,
      CreatedAt = DateTime.UtcNow,
      ExpiredAt = DateTime.UtcNow.AddDays(28),
      // TODO get from http headers
      // Country = ,
      // IpAddress = ,
      // UserAgent = ,
    };

    await _context.AddAsync(session);
    await _context.SaveChangesAsync();

    return session;
  }

  public string CreateJwt(User user)
  {
    var authClaims = new List<Claim>
    {
      new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new (JwtRegisteredClaimNames.Name, user.UserName ?? "")
    };

    var authKey = _configuration["JWTSettings:Key"];
    if (string.IsNullOrEmpty(authKey))
      throw new ArgumentNullException();

    var symetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey));

    var token = new JwtSecurityToken(
      issuer: _configuration["JWTSettings:Issuer"],
      audience: _configuration["JWTSettings:Audience"],
      claims: authClaims,
      expires: DateTime.UtcNow.AddMinutes(5),
      signingCredentials: new SigningCredentials(symetricKey, SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public async Task<string> RefreshJwt(string refreshToken)
  {
    var session = await _context.Sessions.FirstOrDefaultAsync(x => x.RefreshToken.ToString() == refreshToken && x.ExpiredAt > DateTime.UtcNow);
    if (session == null)
    {
      return "";
    }

    var user = await _userManager.FindByIdAsync(session.UserId.ToString());
    if (user == null)
    {
      return "";
    }

    if (user.Deleted)
    {
      return "";
    }

    await _signInManager.SignInAsync(user, true);

    var jwt = CreateJwt(user);

    return jwt;
  }
}
