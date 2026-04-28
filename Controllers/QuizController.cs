using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Services;
using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Controllers;

public class QuizController : Controller
{
    private readonly QuizService _quizService;

    public QuizController(QuizService quizService)
    {
        _quizService = quizService;
    }

    public async Task<IActionResult> Index()
    {
        var quizzes = await _quizService.GetActiveQuizzesAsync();
        return View(quizzes);
    }

    // Note: default route maps "/Quiz/Play/{id?}" => parameter name "id".
    // We accept both names to avoid 404 from route binding.
    public async Task<IActionResult> Play(string? id = null, string? slug = null)
    {
        var effectiveSlug = slug ?? id ?? string.Empty;
        var quiz = await _quizService.GetQuizBySlugAsync(effectiveSlug);
        if (quiz == null)
            return NotFound();

        return View(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitResult(int quizId, byte score, byte correctCount, byte totalCount, int? timeSpentSec = null)
    {
        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
            userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var result = await _quizService.SaveResultAsync(userId, quizId, score, correctCount, totalCount, timeSpentSec);

        return Json(new { success = true, resultId = result.Id });
    }
}
