using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public IActionResult Create() => View();

    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Answers.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
            return NotFound();

        ViewBag.Statistics = await _quizService.GetQuizStatisticsAsync(id);

        return View(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> SaveQuiz(Quiz model)
    {
        var isNew = model.Id == 0;
        if (isNew)
            _context.Quizzes.Add(model);
        else
            _context.Quizzes.Update(model);

        await _context.SaveChangesAsync();
        return isNew
            ? RedirectToAction(nameof(Edit), new { id = model.Id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SaveQuestion([FromBody] SaveQuestionRequest req)
    {
        var q = await _context.QuizQuestions.FindAsync(req.Id);
        if (q == null) return Json(new { success = false });
        q.BodyRu = req.BodyRu?.Trim(); q.BodyBe = req.BodyBe?.Trim(); q.BodyEn = req.BodyEn?.Trim();
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UploadQuestionImage(IFormFile file, int questionId)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question == null)
            return Json(new { success = false, message = "Question not found" });

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
        if (a == null) return Json(new { success = false });
        a.BodyRu = (req.BodyRu ?? "").Trim(); a.BodyBe = (req.BodyBe ?? "").Trim(); a.BodyEn = (req.BodyEn ?? "").Trim();
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
            SortOrder = (byte)(maxOrder + 1)
        };

        _context.QuizAnswers.Add(answer);
        await _context.SaveChangesAsync();

        return Json(new { success = true, answerId = answer.Id });
    }
}

public record SaveQuestionRequest(int Id, string? BodyRu, string? BodyBe, string? BodyEn);
public record SaveAnswerRequest(int Id, string? BodyRu, string? BodyBe, string? BodyEn, bool IsCorrect);
