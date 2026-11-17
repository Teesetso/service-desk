using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Infrastructure.Data;
using ServiceDesk.Web.Authentication;

namespace ServiceDesk.Web.Pages;

public sealed class LoginModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;

    public LoginModel(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true
            ? Redirect("/")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var username = Username.Trim();
        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(item => item.Username == username && item.Active);

        if (user is null || !DemoPasswordHasher.Verify(Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("username", user.Username)
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return LocalRedirect(returnUrl ?? user.Role switch
        {
            "Administrator" => "/Admin",
            "Agent" => "/Tickets",
            _ => "/Portal"
        });
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage();
    }
}
