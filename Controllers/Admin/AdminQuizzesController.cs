using BelarusHeritage.Data;
using BelarusHeritage.Localization;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminQuizzesController : Controller
{
    private readonly AppDbContext _context;
    private readonly QuizService _quizService;
    private readonly FileService _fileService;

    public AdminQuizzesController(AppDbContext context, QuizService quizService, FileService fileService)
    {
        _context = context;
        _quizService = quizService;
        _fileService = fileService;
    }

    public async Task<IActionResult> Index()
    {
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        var stats = new Dictionary<int, Models.ViewModels.QuizStatistics>();
        foreach (var quiz in quizzes)
        {
            stats[quiz.Id] = await _quizService.GetQuizStatisticsAsync(quiz.Id);
        }

        var model = new AdminQuizzesViewModel
        {
            Quizzes = quizzes,
            Statistics = stats
        };

        return View(model);
    }

    public IActionResult Create() => View(new Quiz());

    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await LoadQuizForEditAsync(id);
        if (quiz == null)
            return NotFound();

        ViewBag.Statistics = await _quizService.GetQuizStatisticsAsync(id);

        return View(quiz);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuiz(Quiz model)
    {
        var isNew = model.Id == 0;
        NormalizeQuiz(model);
        ClearQuizBindingValidation();
        await ValidateQuizAsync(model);

        if (!ModelState.IsValid)
            return await QuizFormErrorAsync(model, isNew);

        if (isNew)
        {
            model.CreatedAt = DateTime.UtcNow;
            _context.Quizzes.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = T("admin.quizzes.validation.created");
            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }

        var quiz = await _context.Quizzes.FindAsync(model.Id);
        if (quiz == null)
            return NotFound();

        ApplyQuizFields(quiz, model);
        await _context.SaveChangesAsync();
        TempData["Success"] = T("admin.quizzes.validation.saved");
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    public async Task<IActionResult> SaveQuestion([FromBody] SaveQuestionRequest req)
    {
        var q = await _context.QuizQuestions.FindAsync(req.Id);
        if (q == null) return Json(new { success = false, message = T("admin.quizzes.validation.questionNotFound") });

        q.BodyRu = NullIfEmpty(req.BodyRu);
        q.BodyBe = NullIfEmpty(req.BodyBe);
        q.BodyEn = NullIfEmpty(req.BodyEn);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UploadQuestionImage(IFormFile file, int questionId)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question == null)
            return Json(new { success = false, message = T("admin.quizzes.validation.questionNotFound") });

        try
        {
            var result = await _fileService.UploadImageAsync(file, "quizzes", question.QuizId);
            question.ImageUrl = "/" + result.Url;
            await _context.SaveChangesAsync();

            return Json(new { success = true, imageUrl = question.ImageUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerRequest req)
    {
        var a = await _context.QuizAnswers.FindAsync(req.Id);
        if (a == null) return Json(new { success = false, message = T("admin.quizzes.validation.answerNotFound") });

        var bodyRu = (req.BodyRu ?? "").Trim();
        var bodyBe = (req.BodyBe ?? "").Trim();
        var bodyEn = (req.BodyEn ?? "").Trim();

        if (string.IsNullOrEmpty(bodyRu) && string.IsNullOrEmpty(bodyBe) && string.IsNullOrEmpty(bodyEn))
            return Json(new { success = false, message = T("admin.quizzes.validation.answerEmpty") });

        if (string.IsNullOrEmpty(bodyRu))
            bodyRu = bodyBe.Length > 0 ? bodyBe : bodyEn;
        if (string.IsNullOrEmpty(bodyBe))
            bodyBe = bodyRu;
        if (string.IsNullOrEmpty(bodyEn))
            bodyEn = bodyRu;

        a.BodyRu = bodyRu;
        a.BodyBe = bodyBe;
        a.BodyEn = bodyEn;
        a.IsCorrect = req.IsCorrect;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAnswer([FromBody] int id)
    {
        var answer = await _context.QuizAnswers.FindAsync(id);
        if (answer != null) { _context.QuizAnswers.Remove(answer); await _context.SaveChangesAsync(); }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuiz([FromBody] int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz != null) { _context.Quizzes.Remove(quiz); await _context.SaveChangesAsync(); }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestion([FromBody] int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null) return NotFound();

        var maxOrder = await _context.QuizQuestions.Where(q => q.QuizId == quizId).MaxAsync(q => (short?)q.SortOrder) ?? 0;

        var question = new QuizQuestion
        {
            QuizId = quizId,
            SortOrder = (short)(maxOrder + 1)
        };

        _context.QuizQuestions.Add(question);
        await _context.SaveChangesAsync();

        return Json(new { success = true, questionId = question.Id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuestion([FromBody] int id)
    {
        var question = await _context.QuizQuestions.FindAsync(id);
        if (question != null)
        {
            _context.QuizQuestions.Remove(question);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AddAnswer([FromBody] int questionId)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question == null) return NotFound();

        var maxOrder = await _context.QuizAnswers.Where(a => a.QuestionId == questionId).MaxAsync(a => (byte?)a.SortOrder) ?? 0;

        var answer = new QuizAnswer
        {
            QuestionId = questionId,
            BodyRu = T("admin.quizzes.validation.defaultAnswer"),
            BodyBe = T("admin.quizzes.validation.defaultAnswer"),
            BodyEn = T("admin.quizzes.validation.defaultAnswer"),
            SortOrder = (byte)(maxOrder + 1)
        };

        _context.QuizAnswers.Add(answer);
        await _context.SaveChangesAsync();

        return Json(new { success = true, answerId = answer.Id });
    }

    private string T(string key) => UiText.T(HttpContext, key);

    private async Task<Quiz?> LoadQuizForEditAsync(int id) =>
        await _context.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Answers.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == id);

    private void NormalizeQuiz(Quiz model)
    {
        model.Slug = (model.Slug ?? "").Trim().ToLowerInvariant();
        model.TitleRu = (model.TitleRu ?? "").Trim();
        model.TitleBe = (model.TitleBe ?? "").Trim();
        model.TitleEn = (model.TitleEn ?? "").Trim();
        model.DescriptionRu = NullIfEmpty(model.DescriptionRu);
        model.DescriptionEn = NullIfEmpty(model.DescriptionEn);
        model.CoverUrl = NullIfEmpty(model.CoverUrl);

        if (string.IsNullOrEmpty(model.TitleBe))
            model.TitleBe = model.TitleRu;
        if (string.IsNullOrEmpty(model.TitleEn))
            model.TitleEn = model.TitleRu;

        if (string.IsNullOrEmpty(model.Slug) && !string.IsNullOrEmpty(model.TitleRu))
            model.Slug = GenerateSlug(model.TitleRu);

        model.Slug = Regex.Replace(model.Slug, @"[^a-z0-9\-]", "-");
        model.Slug = Regex.Replace(model.Slug, @"-+", "-").Trim('-');
        if (model.Slug.Length > 80)
            model.Slug = model.Slug[..80].TrimEnd('-');
    }

    /// <summary>
    /// Data annotations run before the action; clear errors for fields filled in <see cref="NormalizeQuiz"/>.
    /// </summary>
    private void ClearQuizBindingValidation()
    {
        ModelState.Remove(nameof(Quiz.TitleBe));
        ModelState.Remove(nameof(Quiz.TitleEn));
        ModelState.Remove(nameof(Quiz.Slug));
    }

    private async Task ValidateQuizAsync(Quiz model)
    {
        if (string.IsNullOrWhiteSpace(model.TitleRu))
            ModelState.AddModelError(nameof(Quiz.TitleRu), T("admin.quizzes.validation.titleRuRequired"));

        if (string.IsNullOrWhiteSpace(model.Slug))
            ModelState.AddModelError(nameof(Quiz.Slug), T("admin.quizzes.validation.slugRequired"));

        if (!string.IsNullOrEmpty(model.Slug) &&
            await _context.Quizzes.AnyAsync(q => q.Slug == model.Slug && q.Id != model.Id))
        {
            ModelState.AddModelError(nameof(Quiz.Slug), T("admin.quizzes.validation.slugExists"));
        }
    }

    private static void ApplyQuizFields(Quiz target, Quiz source)
    {
        target.Slug = source.Slug;
        target.Type = source.Type;
        target.TitleRu = source.TitleRu;
        target.TitleBe = source.TitleBe;
        target.TitleEn = source.TitleEn;
        target.DescriptionRu = source.DescriptionRu;
        target.DescriptionEn = source.DescriptionEn;
        target.CoverUrl = source.CoverUrl;
        target.TimeLimit = source.TimeLimit;
        target.IsActive = source.IsActive;
        target.SortOrder = source.SortOrder;
    }

    private async Task<IActionResult> QuizFormErrorAsync(Quiz model, bool isNew)
    {
        if (isNew)
            return View("Create", model);

        var quiz = await LoadQuizForEditAsync(model.Id);
        if (quiz == null)
            return NotFound();

        ApplyQuizFields(quiz, model);
        ViewBag.Statistics = await _quizService.GetQuizStatisticsAsync(model.Id);
        return View("Edit", quiz);
    }

    private static string? NullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("?", "")
            .Replace("!", "");

        slug = slug.Replace("а", "a").Replace("б", "b").Replace("в", "v")
            .Replace("г", "g").Replace("д", "d").Replace("е", "e")
            .Replace("ё", "yo").Replace("ж", "zh").Replace("з", "z")
            .Replace("и", "i").Replace("й", "y").Replace("к", "k")
            .Replace("л", "l").Replace("м", "m").Replace("н", "n")
            .Replace("о", "o").Replace("п", "p").Replace("р", "r")
            .Replace("с", "s").Replace("т", "t").Replace("у", "u")
            .Replace("ф", "f").Replace("х", "kh").Replace("ц", "ts")
            .Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "sch")
            .Replace("ъ", "").Replace("ы", "y").Replace("ь", "")
            .Replace("э", "e").Replace("ю", "yu").Replace("я", "ya");

        return slug;
    }
}

public record SaveQuestionRequest(int Id, string? BodyRu, string? BodyBe, string? BodyEn);
public record SaveAnswerRequest(int Id, string? BodyRu, string? BodyBe, string? BodyEn, bool IsCorrect);
