using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BelarusHeritage.Data;
using BelarusHeritage.Localization;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminUsersController : Controller
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "user", "admin"
    };

    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public AdminUsersController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? role = null, string? search = null, int page = 1)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Email!.Contains(search) || u.UserName!.Contains(search));

        var model = new AdminUsersViewModel
        {
            Users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * 25)
                .Take(25)
                .ToListAsync(),
            TotalCount = await query.CountAsync(),
            CurrentPage = page,
            PageSize = 25,
            Role = role,
            Search = search
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var favEntities = await _context.Favorites
            .Where(f => f.UserId == id)
            .Include(f => f.Object).ThenInclude(o => o!.Category)
            .ToListAsync();

        var commentEntities = await _context.Comments
            .Where(c => c.UserId == id)
            .Include(c => c.Object)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var ratingEntities = await _context.Ratings
            .Where(r => r.UserId == id)
            .Include(r => r.Object)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var model = new AdminUserDetailsViewModel
        {
            User = user,
            IsCurrentUser = id == GetCurrentUserId(),
            Favorites = favEntities.Select(f => f.Object!).Where(o => o != null).ToList(),
            Comments = commentEntities
                .Where(c => c.Object != null)
                .Select(c => new CommentWithObject { Comment = c, Object = c.Object! })
                .ToList(),
            Ratings = ratingEntities
                .Where(r => r.Object != null)
                .Select(r => new RatingWithObject { Rating = r, Object = r.Object! })
                .ToList(),
            Routes = await _context.Routes.Where(r => r.UserId == id).Include(r => r.Stops).OrderByDescending(r => r.UpdatedAt).ToListAsync(),
            Ornaments = await _context.Ornaments.Where(o => o.UserId == id).OrderByDescending(o => o.CreatedAt).ToListAsync(),
            QuizResults = await _context.QuizResults.Where(r => r.UserId == id).Include(r => r.Quiz).OrderByDescending(r => r.CompletedAt).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int id, string role)
    {
        if (id == GetCurrentUserId())
        {
            TempData["ErrorMessage"] = T("admin.users.manage.cannotChangeOwnRole");
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ValidRoles.Contains(role))
        {
            TempData["ErrorMessage"] = T("admin.users.manage.invalidRole");
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, roles);
        await _userManager.AddToRoleAsync(user, role);

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = T("admin.users.manage.roleChanged");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(int id, string email)
    {
        email = email.Trim();

        if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
        {
            TempData["ErrorMessage"] = T("admin.users.manage.invalidEmail");
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            TempData["SuccessMessage"] = T("admin.users.manage.emailUnchanged");
            return RedirectToAction(nameof(Details), new { id });
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null && existing.Id != id)
        {
            TempData["ErrorMessage"] = T("admin.users.manage.emailExists");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _userManager.SetEmailAsync(user, email);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = T("admin.users.manage.emailFailed");
            return RedirectToAction(nameof(Details), new { id });
        }

        user.EmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = T("admin.users.manage.emailChanged");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleBlock(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
        }

        return Json(new { success = true, isActive = user?.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            await _context.Comments.Where(c => c.UserId == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.UserId, (int?)null));
            await _context.Ratings.Where(r => r.UserId == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.UserId, (int?)null));
            await _context.Favorites.Where(f => f.UserId == id).ExecuteDeleteAsync();

            await _userManager.DeleteAsync(user);
        }

        return Json(new { success = true });
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string T(string key) => UiText.T(HttpContext, key);
}
