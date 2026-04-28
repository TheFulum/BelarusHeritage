using BelarusHeritage.Data;
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
            query = query.Where(u => _userManager.GetRolesAsync(u).Result.Contains(role));

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

        var model = new ProfileViewModel
        {
            User = user,
            Favorites = favEntities.Select(f => f.Object!).Where(o => o != null).ToList(),
            Comments = await _context.Comments.Where(c => c.UserId == id).Select(c => new CommentWithObject { Comment = c, Object = c.Object! }).ToListAsync(),
            Ratings = await _context.Ratings.Where(r => r.UserId == id).Select(r => new RatingWithObject { Rating = r, Object = r.Object! }).ToListAsync(),
            Routes = await _context.Routes.Where(r => r.UserId == id).ToListAsync(),
            Ornaments = await _context.Ornaments.Where(o => o.UserId == id).ToListAsync(),
            QuizResults = await _context.QuizResults.Where(r => r.UserId == id).Include(r => r.Quiz).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeRole(int id, string role)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, roles);
            await _userManager.AddToRoleAsync(user, role);
        }

        return Json(new { success = true });
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
            // Null all references to preserve data integrity
            await _context.Comments.Where(c => c.UserId == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.UserId, (int?)null));
            await _context.Ratings.Where(r => r.UserId == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.UserId, (int?)null));
            await _context.Favorites.Where(f => f.UserId == id).ExecuteDeleteAsync();

            await _userManager.DeleteAsync(user);
        }

        return Json(new { success = true });
    }
}
