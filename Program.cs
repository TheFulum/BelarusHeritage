using BelarusHeritage.Data;
using BelarusHeritage.Middleware;
using BelarusHeritage.Services;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// HtmlEncoder: allow Cyrillic / non-ASCII to be emitted as raw chars (instead of &#x...;)
builder.Services.Configure<WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        }));

// Authentication
builder.Services.AddIdentity<BelarusHeritage.Models.Domain.User, BelarusHeritage.Models.Domain.UserRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var jwtSecret = builder.Configuration["App:JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["App:JwtSettings:Issuer"] ?? "BelarusHeritage";
var jwtAudience = builder.Configuration["App:JwtSettings:Audience"] ?? "BelarusHeritageUsers";
var jwtExpiry = int.Parse(builder.Configuration["App:JwtSettings:ExpiryMinutes"] ?? "10080");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["auth_token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("admin", "moderator"));
    options.AddPolicy("Moderator", policy => policy.RequireRole("moderator"));
});

builder.Services.AddMemoryCache();

// Services
builder.Services.AddScoped<ObjectService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<RouteService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<SiteSettingsService>();

// Session for guest route builder
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// App settings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseMiddleware<AuditLogMiddleware>();

var supportedCultures = new[] { "ru", "be", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("ru")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Make the Program class accessible for testing
public partial class Program { }

// App settings configuration class
public class AppSettings
{
    public string SiteName { get; set; } = "Культурное наследие Беларуси";
    public string SiteNameEn { get; set; } = "Cultural Heritage of Belarus";
    public string AdminEmail { get; set; } = "admin@heritage.by";
    public int MaxUploadSizeMb { get; set; } = 10;
    public string AllowedImageExtensions { get; set; } = ".jpg,.jpeg,.png,.webp";
    public JwtSettings JwtSettings { get; set; } = new();
    public EmailSettings EmailSettings { get; set; } = new();
}

public class JwtSettings
{
    public string SecretKey { get; set; } = "";
    public string Issuer { get; set; } = "BelarusHeritage";
    public string Audience { get; set; } = "BelarusHeritageUsers";
    public int ExpiryMinutes { get; set; } = 10080;
}

public class EmailSettings
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
}
