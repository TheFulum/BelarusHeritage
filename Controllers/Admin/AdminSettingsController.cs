using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Localization;
using BelarusHeritage.Services;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminSettingsController : Controller
{
    private readonly SiteSettingsService _settings;

    public AdminSettingsController(SiteSettingsService settings)
    {
        _settings = settings;
    }

    public async Task<IActionResult> SocialLinks()
    {
        var links = await _settings.GetSocialLinksAsync();
        return View(links);
    }

    [HttpPost]
    public async Task<IActionResult> SocialLinks(string? telegram, string? instagram, string? vk, string? youtube)
    {
        await _settings.SetAsync("social.telegram",  telegram?.Trim());
        await _settings.SetAsync("social.instagram", instagram?.Trim());
        await _settings.SetAsync("social.vk",        vk?.Trim());
        await _settings.SetAsync("social.youtube",   youtube?.Trim());

        TempData["Success"] = UiText.T(HttpContext, "admin.social.saved");
        return RedirectToAction(nameof(SocialLinks));
    }
}
