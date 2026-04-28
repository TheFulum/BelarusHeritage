using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Services;

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
        var token = await _authService.LoginAsync(email, password, rememberMe);

        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError("", "Invalid email or password");
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
            ModelState.AddModelError("", "Passwords do not match");
            return View();
        }

        var (user, errors) = await _authService.RegisterAsync(email, username, password);

        if (user == null)
        {
            foreach (var error in errors)
                ModelState.AddModelError("", error);
            return View();
        }

        TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> VerifyEmail(string token)
    {
        var result = await _authService.VerifyEmailAsync(token);

        if (result)
        {
            TempData["SuccessMessage"] = "Email verified successfully! You can now log in.";
        }
        else
        {
            TempData["ErrorMessage"] = "Invalid or expired verification token.";
        }

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

        TempData["SuccessMessage"] = "If an account with that email exists, a password reset link has been sent.";
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
            ModelState.AddModelError("", "Passwords do not match");
            return View();
        }

        var result = await _authService.ResetPasswordAsync(token, password);

        if (result)
        {
            TempData["SuccessMessage"] = "Password reset successful! You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError("", "Invalid or expired reset token.");
        return View();
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        return RedirectToAction("Index", "Home");
    }
}
