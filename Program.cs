using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddIdentity<User, Role>(options =>
  {
    // Deliberately not setting complex password rules
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // only alphanumeric for username
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
  }
).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.AddAuthentication(auth =>
{
  auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
  var authKey = builder.Configuration["JWTSettings:Key"];
  if (string.IsNullOrEmpty(authKey))
    throw new ArgumentNullException();

  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidIssuer = builder.Configuration["JWTSettings:Issuer"],
    ValidAudience = builder.Configuration["JWTSettings:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey)),
    ValidateIssuerSigningKey = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
  };
  options.Events = new JwtBearerEvents
  {
    OnAuthenticationFailed = async ctx =>
    {
      if (ctx.Exception.GetType() == typeof(SecurityTokenExpiredException))
      {
        var serviceProvider = ctx.HttpContext.RequestServices;
        
        var cookies = ctx.Request.Cookies;
        if (cookies.TryGetValue("refreshToken", out var refreshTokenValue))
        {
          var sessionService = serviceProvider.GetRequiredService<SessionService>();
          var jwt = await sessionService.RefreshJwt(refreshTokenValue);

          if (!string.IsNullOrEmpty(jwt))
          {
            var cookieOptions = new CookieOptions
            {
              HttpOnly = true,
              Secure = true,
              Expires = DateTime.UtcNow.AddDays(30),
              SameSite = SameSiteMode.Strict
            };
            ctx.Response.Cookies.Append("accessToken", jwt, cookieOptions);
            return;
          }
        }

        var signinManager = serviceProvider.GetRequiredService<SignInManager<User>>();
        await signinManager.SignOutAsync();
        ctx.Response.Cookies.Delete("accessToken");
        ctx.Response.Cookies.Delete("refreshToken");
      }
    },
    OnMessageReceived = ctx =>
    {
      var cookies = ctx.Request.Cookies;
      if (cookies.TryGetValue("accessToken", out var accessTokenValue))
      {
        ctx.Token = accessTokenValue;
      }

      return Task.CompletedTask;
    }
  };
});

builder.Services.AddRazorPages(o =>
{
  // 
  // o.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});

builder.Services.AddLocalization();
builder.Services.AddSingleton<LocalizationMiddleware>();
builder.Services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

builder.Services.AddTransient<SessionService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddTransient<EmailService>();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

  dbContext.Database.Migrate();
}

var options = new RequestLocalizationOptions
{
  DefaultRequestCulture = new RequestCulture(new CultureInfo("en-US"))
};
app.UseRequestLocalization(options);

app.UseStatusCodePages(async context =>
  {
      var response = context.HttpContext.Response;

      if (response.StatusCode == (int)HttpStatusCode.Unauthorized ||
          response.StatusCode == (int)HttpStatusCode.Forbidden || 
          response.StatusCode == (int)HttpStatusCode.NotFound) 
          response.Redirect("/");
  });

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<LocalizationMiddleware>();
app.UseRouting();

app.UseAuthentication();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
