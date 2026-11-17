using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Infrastructure.Data;
using ServiceDesk.Web.Authentication;

namespace ServiceDesk.Web.Pages.Admin;

[Authorize(Policy = "Administrators")]
public sealed class IndexModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;

    public IndexModel(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<AppUser> Users { get; private set; } = Array.Empty<AppUser>();
    public IReadOnlyList<AppRole> Roles { get; private set; } = Array.Empty<AppRole>();

    [BindProperty]
    public string? NewRoleName { get; set; }

    [BindProperty]
    public Guid UserId { get; set; }

    [BindProperty]
    public string? SelectedRole { get; set; }

    [BindProperty]
    public string? NewUsername { get; set; }

    [BindProperty]
    public string? NewDisplayName { get; set; }

    [BindProperty]
    public string? NewPassword { get; set; }

    [BindProperty]
    public string? NewUserRole { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateRoleAsync(CancellationToken cancellationToken)
    {
        var roleName = NewRoleName?.Trim() ?? string.Empty;
        if (roleName.Length is < 2 or > 50)
        {
            ModelState.AddModelError(nameof(NewRoleName), "Role names must be between 2 and 50 characters.");
        }
        else if (await _dbContext.AppRoles.AnyAsync(role => role.Name == roleName, cancellationToken))
        {
            ModelState.AddModelError(nameof(NewRoleName), "That role already exists.");
        }
        else if (ModelState.IsValid)
        {
            _dbContext.AppRoles.Add(new AppRole
            {
                TenantId = Infrastructure.DevelopmentRequestContext.TenantIdValue,
                Name = roleName
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["AdminStatusMessage"] = "Role created.";
            return RedirectToPage();
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateUserAsync(CancellationToken cancellationToken)
    {
        var username = NewUsername?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = NewDisplayName?.Trim() ?? string.Empty;
        var role = NewUserRole?.Trim();

        if (username.Length is < 3 or > 100)
        {
            ModelState.AddModelError(nameof(NewUsername), "Usernames must be between 3 and 100 characters.");
        }

        if (displayName.Length is < 2 or > 150)
        {
            ModelState.AddModelError(nameof(NewDisplayName), "Display names must be between 2 and 150 characters.");
        }

        if ((NewPassword?.Length ?? 0) < 6)
        {
            ModelState.AddModelError(nameof(NewPassword), "Passwords must be at least 6 characters.");
        }

        if (string.IsNullOrWhiteSpace(role) ||
            !await _dbContext.AppRoles.AnyAsync(item => item.Name == role, cancellationToken))
        {
            ModelState.AddModelError(nameof(NewUserRole), "Select an existing role.");
        }

        if (await _dbContext.AppUsers.AnyAsync(item => item.Username == username, cancellationToken))
        {
            ModelState.AddModelError(nameof(NewUsername), "That username already exists.");
        }

        if (ModelState.IsValid)
        {
            _dbContext.AppUsers.Add(new AppUser
            {
                UserId = Guid.NewGuid(),
                TenantId = Infrastructure.DevelopmentRequestContext.TenantIdValue,
                Username = username,
                DisplayName = displayName,
                PasswordHash = DemoPasswordHasher.Hash(NewPassword!),
                Role = role!
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["AdminStatusMessage"] = "User created.";
            return RedirectToPage();
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateUserRoleAsync(CancellationToken cancellationToken)
    {
        var roleExists = await _dbContext.AppRoles
            .AnyAsync(role => role.Name == SelectedRole, cancellationToken);
        var user = await _dbContext.AppUsers
            .SingleOrDefaultAsync(item => item.UserId == UserId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(SelectedRole) || !roleExists)
        {
            ModelState.AddModelError(string.Empty, "Select an existing user and role.");
        }
        else
        {
            user.Role = SelectedRole;
            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["AdminStatusMessage"] = "User role updated.";
            return RedirectToPage();
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Users = await _dbContext.AppUsers.OrderBy(user => user.Username).ToListAsync(cancellationToken);
        Roles = await _dbContext.AppRoles.OrderBy(role => role.Name).ToListAsync(cancellationToken);
    }
}
