using BelarusHeritage.Localization;
using BelarusHeritage.Services;
using Microsoft.AspNetCore.Mvc;

namespace BelarusHeritage.Controllers;

public class AuthController : Controller
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl)
    {
        var (status, token) = await _authService.LoginAsync(email, password, rememberMe);

        if (status != LoginStatus.Success || string.IsNullOrEmpty(token))
        {
            var messageKey = status switch
            {
                LoginStatus.AccountDisabled => "auth.error.accountDisabled",
                LoginStatus.LockedOut => "auth.error.accountLocked",
                _ => "auth.error.invalidCredentials"
            };
            ModelState.AddModelError("", T(messageKey));
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = rememberMe ? DateTimeOffset.Now.AddDays(14) : DateTimeOffset.Now.AddHours(1)
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string email, string username, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("", T("auth.error.passwordMismatch"));
            return View();
        }

        var (user, errors) = await _authService.RegisterAsync(email, username, password);

        if (user == null)
        {
            foreach (var error in errors)
                ModelState.AddModelError("", MapRegisterError(error));
            return View();
        }

        TempData["SuccessMessage"] = T("auth.success.register");
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> VerifyEmail(string token)
    {
        var result = await _authService.VerifyEmailAsync(token);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] =
            T(result ? "auth.success.emailVerified" : "auth.error.invalidToken");

        return RedirectToAction(nameof(Login));
    }

    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        await _authService.GeneratePasswordResetTokenAsync(email);

        TempData["SuccessMessage"] = T("auth.success.forgotPassword");
        return RedirectToAction(nameof(Login));
    }

    public IActionResult ResetPassword(string token)
    {
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("", T("auth.error.passwordMismatch"));
            ViewBag.Token = token;
            return View();
        }

        var result = await _authService.ResetPasswordAsync(token, password);

        if (result)
        {
            TempData["SuccessMessage"] = T("auth.success.passwordReset");
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError("", T("auth.error.invalidToken"));
        ViewBag.Token = token;
        return View();
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        return RedirectToAction("Index", "Home");
    }

    private string T(string key) => UiText.T(HttpContext, key);

    private string MapRegisterError(string code) => code switch
    {
        "email_exists" => T("auth.error.emailExists"),
        "username_exists" => T("auth.error.usernameExists"),
        _ => T("auth.error.registerFailed")
    };
}
