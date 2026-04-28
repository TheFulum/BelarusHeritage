using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BelarusHeritage.Models.Domain;
using HeritageRoute = BelarusHeritage.Models.Domain.Route;

namespace BelarusHeritage.Data;

public class AppDbContext : IdentityDbContext<User, UserRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Reference data
    public DbSet<Region> Regions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ArchStyle> ArchStyles { get; set; }

    // Heritage objects
    public DbSet<HeritageObject> HeritageObjects { get; set; }
    public DbSet<ObjectImage> ObjectImages { get; set; }
    public DbSet<ObjectLocation> ObjectLocations { get; set; }
    public DbSet<ObjectTagMap> ObjectTagMaps { get; set; }
    public DbSet<ObjectRelation> ObjectRelations { get; set; }
    public DbSet<ObjectSource> ObjectSources { get; set; }

    // User activity
    public new DbSet<UserToken> UserTokens { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    // Routes
    public DbSet<HeritageRoute> Routes { get; set; }
    public DbSet<RouteStop> RouteStops { get; set; }

    // Quizzes
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuizAnswer> QuizAnswers { get; set; }
    public DbSet<QuizResult> QuizResults { get; set; }

    // Ornaments
    public DbSet<Ornament> Ornaments { get; set; }

    // Timeline
    public DbSet<TimelineEvent> TimelineEvents { get; set; }

    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; }

    // Settings
    public DbSet<SiteSettings> SiteSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        //  ASP.NET Identity mapping -> custom snake_case tables
        // ============================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserName).HasColumnName("username").HasMaxLength(60);
            entity.Property(e => e.NormalizedUserName).HasColumnName("normalized_username").HasMaxLength(60);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(255);
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(255);
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32);
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");

            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
            entity.Property(e => e.Bio).HasColumnName("bio").HasMaxLength(500);
            entity.Property(e => e.PreferredLang).HasColumnName("preferred_lang").HasMaxLength(2);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.GoogleId).HasColumnName("google_id").HasMaxLength(100);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(30);
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name").HasMaxLength(256);
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(255);
        });

        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.ToTable("user_role_map");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.ToTable("user_claims");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type").HasMaxLength(255);
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.ToTable("user_logins");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider").HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasColumnName("provider_key").HasMaxLength(128);
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name").HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.ToTable("asp_user_tokens");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider").HasMaxLength(128);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(128);
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(entity =>
        {
            entity.ToTable("role_claims");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type").HasMaxLength(255);
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        // Regions
        modelBuilder.Entity<Region>(entity =>
        {
            entity.ToTable("regions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
            entity.Property(e => e.NameRu).HasColumnName("name_ru").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameBe).HasColumnName("name_be").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Categories
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(60).IsRequired();
            entity.Property(e => e.NameRu).HasColumnName("name_ru").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameBe).HasColumnName("name_be").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(100).IsRequired();
            entity.Property(e => e.IconClass).HasColumnName("icon_class").HasMaxLength(60);
            entity.Property(e => e.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Tags
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(80).IsRequired();
            entity.Property(e => e.NameRu).HasColumnName("name_ru").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameBe).HasColumnName("name_be").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // ArchStyles
        modelBuilder.Entity<ArchStyle>(entity =>
        {
            entity.ToTable("arch_styles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(80).IsRequired();
            entity.Property(e => e.NameRu).HasColumnName("name_ru").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameBe).HasColumnName("name_be").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // HeritageObjects
        modelBuilder.Entity<HeritageObject>(entity =>
        {
            entity.ToTable("heritage_objects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
            entity.Property(e => e.NameRu).HasColumnName("name_ru").HasMaxLength(255).IsRequired();
            entity.Property(e => e.NameBe).HasColumnName("name_be").HasMaxLength(255).IsRequired();
            entity.Property(e => e.NameEn).HasColumnName("name_en").HasMaxLength(255).IsRequired();
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.ArchStyleId).HasColumnName("arch_style_id");
            entity.Property(e => e.CenturyStart).HasColumnName("century_start");
            entity.Property(e => e.CenturyEnd).HasColumnName("century_end");
            entity.Property(e => e.BuildYear).HasColumnName("build_year");
            entity.Property(e => e.DescriptionRu).HasColumnName("description_ru");
            entity.Property(e => e.DescriptionBe).HasColumnName("description_be");
            entity.Property(e => e.DescriptionEn).HasColumnName("description_en");
            entity.Property(e => e.ShortDescRu).HasColumnName("short_desc_ru").HasMaxLength(300);
            entity.Property(e => e.ShortDescBe).HasColumnName("short_desc_be").HasMaxLength(300);
            entity.Property(e => e.ShortDescEn).HasColumnName("short_desc_en").HasMaxLength(300);
            entity.Property(e => e.FunFactRu).HasColumnName("fun_fact_ru").HasMaxLength(500);
            entity.Property(e => e.FunFactBe).HasColumnName("fun_fact_be").HasMaxLength(500);
            entity.Property(e => e.FunFactEn).HasColumnName("fun_fact_en").HasMaxLength(500);
            entity.Property(e => e.Architect).HasColumnName("architect").HasMaxLength(255);
            entity.Property(e => e.HeritageCategory).HasColumnName("heritage_category");
            entity.Property(e => e.HeritageYear).HasColumnName("heritage_year");
            entity.Property(e => e.PreservationStatus).HasColumnName("preservation_status")
                .HasConversion(
                    v => v == PreservationStatus.Preserved ? "preserved"
                       : v == PreservationStatus.Partial ? "partial"
                       : v == PreservationStatus.Ruins ? "ruins"
                       : "lost",
                    v => v == "preserved" ? PreservationStatus.Preserved
                       : v == "partial" ? PreservationStatus.Partial
                       : v == "ruins" ? PreservationStatus.Ruins
                       : PreservationStatus.Lost);
            entity.Property(e => e.IsVisitable).HasColumnName("is_visitable");
            entity.Property(e => e.VisitingHours).HasColumnName("visiting_hours").HasMaxLength(255);
            entity.Property(e => e.EntryFee).HasColumnName("entry_fee").HasMaxLength(100);
            entity.Property(e => e.MainImageUrl).HasColumnName("main_image_url").HasMaxLength(500);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion(
                v => v == ObjectStatus.Draft ? "draft"
                   : v == ObjectStatus.Published ? "published"
                   : "archived",
                v => v == "draft" ? ObjectStatus.Draft
                   : v == "published" ? ObjectStatus.Published
                   : ObjectStatus.Archived);
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.RatingAvg).HasColumnName("rating_avg").HasPrecision(3, 2);
            entity.Property(e => e.RatingCount).HasColumnName("rating_count");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => new { e.Status, e.IsDeleted });
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.RegionId);
            entity.HasIndex(e => new { e.CenturyStart, e.CenturyEnd });
            entity.HasIndex(e => new { e.IsFeatured, e.Status });
            entity.HasIndex(e => e.RatingAvg);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Objects)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Region)
                .WithMany(r => r.Objects)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ArchStyle)
                .WithMany(a => a.Objects)
                .HasForeignKey(e => e.ArchStyleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Updater)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ObjectImages
        modelBuilder.Entity<ObjectImage>(entity =>
        {
            entity.ToTable("object_images");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.Url).HasColumnName("url").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ThumbUrl).HasColumnName("thumb_url").HasMaxLength(500);
            entity.Property(e => e.CaptionRu).HasColumnName("caption_ru").HasMaxLength(300);
            entity.Property(e => e.CaptionBe).HasColumnName("caption_be").HasMaxLength(300);
            entity.Property(e => e.CaptionEn).HasColumnName("caption_en").HasMaxLength(300);
            entity.Property(e => e.IsMain).HasColumnName("is_main");
            entity.Property(e => e.Is360).HasColumnName("is_360");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Object)
                .WithMany(o => o.Images)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Uploader)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.ObjectId, e.SortOrder });
        });

        // ObjectLocations
        modelBuilder.Entity<ObjectLocation>(entity =>
        {
            entity.ToTable("object_locations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.Lat).HasColumnName("lat").HasPrecision(10, 7);
            entity.Property(e => e.Lng).HasColumnName("lng").HasPrecision(10, 7);
            entity.Property(e => e.AddressRu).HasColumnName("address_ru").HasMaxLength(300);
            entity.Property(e => e.AddressBe).HasColumnName("address_be").HasMaxLength(300);
            entity.Property(e => e.AddressEn).HasColumnName("address_en").HasMaxLength(300);
            entity.Property(e => e.MapZoom).HasColumnName("map_zoom");

            entity.HasOne(e => e.Object)
                .WithOne(o => o.Location)
                .HasForeignKey<ObjectLocation>(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Lat, e.Lng });
        });

        // ObjectTagMaps
        modelBuilder.Entity<ObjectTagMap>(entity =>
        {
            entity.ToTable("object_tag_map");
            entity.HasKey(e => new { e.ObjectId, e.TagId });
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");

            entity.HasOne(e => e.Object)
                .WithMany(o => o.TagMaps)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.ObjectTagMaps)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ObjectRelations
        modelBuilder.Entity<ObjectRelation>(entity =>
        {
            entity.ToTable("object_relations");
            entity.HasKey(e => new { e.ObjectId, e.RelatedId });
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.RelatedId).HasColumnName("related_id");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(200);

            entity.HasOne(e => e.Object)
                .WithMany(o => o.RelatedFrom)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Related)
                .WithMany(o => o.RelatedTo)
                .HasForeignKey(e => e.RelatedId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ObjectSources
        modelBuilder.Entity<ObjectSource>(entity =>
        {
            entity.ToTable("object_sources");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion(
                v => v == SourceType.Book ? "book"
                   : v == SourceType.Article ? "article"
                   : v == SourceType.Website ? "website"
                   : v == SourceType.Archive ? "archive"
                   : v == SourceType.Museum ? "museum"
                   : "other",
                v => v == "book" ? SourceType.Book
                   : v == "article" ? SourceType.Article
                   : v == "website" ? SourceType.Website
                   : v == "archive" ? SourceType.Archive
                   : v == "museum" ? SourceType.Museum
                   : SourceType.Other);
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Author).HasColumnName("author").HasMaxLength(300);
            entity.Property(e => e.Publisher).HasColumnName("publisher").HasMaxLength(300);
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.Url).HasColumnName("url").HasMaxLength(1000);
            entity.Property(e => e.Pages).HasColumnName("pages").HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(e => e.Object)
                .WithMany(o => o.Sources)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserTokens
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion(
                    v => v == TokenType.EmailVerify   ? "email_verify"
                       : v == TokenType.PasswordReset ? "password_reset"
                       : "refresh",
                    v => v == "email_verify"   ? TokenType.EmailVerify
                       : v == "password_reset" ? TokenType.PasswordReset
                       : TokenType.Refresh);
            entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(255).IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Tokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Favorites
        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.ToTable("favorites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.AddedAt).HasColumnName("added_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Object)
                .WithMany(o => o.Favorites)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.ObjectId }).IsUnique();
        });

        // Comments
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.Body).HasColumnName("body").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion(
                v => v == CommentStatus.Pending ? "pending"
                   : v == CommentStatus.Approved ? "approved"
                   : v == CommentStatus.Rejected ? "rejected"
                   : "spam",
                v => v == "pending" ? CommentStatus.Pending
                   : v == "approved" ? CommentStatus.Approved
                   : v == "rejected" ? CommentStatus.Rejected
                   : CommentStatus.Spam);
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Object)
                .WithMany(o => o.Comments)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Ratings
        modelBuilder.Entity<Rating>(entity =>
        {
            entity.ToTable("ratings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Object)
                .WithMany(o => o.Ratings)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.ObjectId }).IsUnique();
        });

        // Routes
        modelBuilder.Entity<HeritageRoute>(entity =>
        {
            entity.ToTable("routes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.ShareToken).HasColumnName("share_token").HasMaxLength(32);
            entity.Property(e => e.TotalKm).HasColumnName("total_km").HasPrecision(8, 1);
            entity.Property(e => e.StartAddress).HasColumnName("start_address").HasMaxLength(255);
            entity.Property(e => e.StartLat).HasColumnName("start_lat").HasPrecision(10, 7);
            entity.Property(e => e.StartLng).HasColumnName("start_lng").HasPrecision(10, 7);
            entity.Property(e => e.EndAddress).HasColumnName("end_address").HasMaxLength(255);
            entity.Property(e => e.EndLat).HasColumnName("end_lat").HasPrecision(10, 7);
            entity.Property(e => e.EndLng).HasColumnName("end_lng").HasPrecision(10, 7);
            entity.Property(e => e.SourceRouteId).HasColumnName("source_route_id");
            entity.Property(e => e.SourceRouteTitle).HasColumnName("source_route_title").HasMaxLength(255);
            entity.Property(e => e.SourceRouteShareToken).HasColumnName("source_route_share_token").HasMaxLength(32);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Routes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RouteStops
        modelBuilder.Entity<RouteStop>(entity =>
        {
            entity.ToTable("route_stops");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RouteId).HasColumnName("route_id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);

            entity.HasOne(e => e.Route)
                .WithMany(r => r.Stops)
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Object)
                .WithMany()
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.RouteId, e.ObjectId }).IsUnique();
        });

        // Quizzes
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.ToTable("quizzes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(80).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasConversion(
                v => v == QuizType.ImageGuess ? "image_guess"
                   : v == QuizType.RegionGuess ? "region_guess"
                   : v == QuizType.DragDrop ? "dragdrop"
                   : v == QuizType.CenturyGuess ? "century_guess"
                   : "odd_one_out",
                v => v == "image_guess" ? QuizType.ImageGuess
                   : v == "region_guess" ? QuizType.RegionGuess
                   : v == "dragdrop" ? QuizType.DragDrop
                   : v == "century_guess" ? QuizType.CenturyGuess
                   : QuizType.OddOneOut);
            entity.Property(e => e.TitleRu).HasColumnName("title_ru").HasMaxLength(200).IsRequired();
            entity.Property(e => e.TitleBe).HasColumnName("title_be").HasMaxLength(200).IsRequired();
            entity.Property(e => e.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            entity.Property(e => e.DescriptionRu).HasColumnName("description_ru");
            entity.Property(e => e.DescriptionEn).HasColumnName("description_en");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url").HasMaxLength(500);
            entity.Property(e => e.TimeLimit).HasColumnName("time_limit");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // QuizQuestions
        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.ToTable("quiz_questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.BodyRu).HasColumnName("body_ru").HasMaxLength(500);
            entity.Property(e => e.BodyBe).HasColumnName("body_be").HasMaxLength(500);
            entity.Property(e => e.BodyEn).HasColumnName("body_en").HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(e => e.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(e => e.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuizAnswers
        modelBuilder.Entity<QuizAnswer>(entity =>
        {
            entity.ToTable("quiz_answers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.BodyRu).HasColumnName("body_ru").HasMaxLength(300).IsRequired();
            entity.Property(e => e.BodyBe).HasColumnName("body_be").HasMaxLength(300).IsRequired();
            entity.Property(e => e.BodyEn).HasColumnName("body_en").HasMaxLength(300).IsRequired();
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuizResults
        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.ToTable("quiz_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.CorrectCount).HasColumnName("correct_count");
            entity.Property(e => e.TotalCount).HasColumnName("total_count");
            entity.Property(e => e.TimeSpentSec).HasColumnName("time_spent_sec");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.QuizResults)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Quiz)
                .WithMany(q => q.Results)
                .HasForeignKey(e => e.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Ornaments
        modelBuilder.Entity<Ornament>(entity =>
        {
            entity.ToTable("ornaments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(500).IsRequired();
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Ornaments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TimelineEvents
        modelBuilder.Entity<TimelineEvent>(entity =>
        {
            entity.ToTable("timeline_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.TitleRu).HasColumnName("title_ru").HasMaxLength(300).IsRequired();
            entity.Property(e => e.TitleBe).HasColumnName("title_be").HasMaxLength(300).IsRequired();
            entity.Property(e => e.TitleEn).HasColumnName("title_en").HasMaxLength(300).IsRequired();
            entity.Property(e => e.BodyRu).HasColumnName("body_ru");
            entity.Property(e => e.BodyEn).HasColumnName("body_en");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(e => e.IsPublished).HasColumnName("is_published");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(e => e.Object)
                .WithMany(o => o.TimelineEvents)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLogs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Entity).HasColumnName("entity").HasMaxLength(60).IsRequired();
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.Payload).HasColumnName("payload");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SiteSettings
        modelBuilder.Entity<SiteSettings>(entity =>
        {
            entity.ToTable("site_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Seed initial data for reference tables
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Regions
        modelBuilder.Entity<Region>().HasData(
            new Region { Id = 1, Code = "brest", NameRu = "Брестская область", NameBe = "Брэсцкая вобласць", NameEn = "Brest Region" },
            new Region { Id = 2, Code = "vitebsk", NameRu = "Витебская область", NameBe = "Віцебская вобласць", NameEn = "Vitebsk Region" },
            new Region { Id = 3, Code = "gomel", NameRu = "Гомельская область", NameBe = "Гомельская вобласць", NameEn = "Gomel Region" },
            new Region { Id = 4, Code = "grodno", NameRu = "Гродненская область", NameBe = "Гродзенская вобласць", NameEn = "Grodno Region" },
            new Region { Id = 5, Code = "minsk", NameRu = "Минская область", NameBe = "Мінская вобласць", NameEn = "Minsk Region" },
            new Region { Id = 6, Code = "mogilev", NameRu = "Могилёвская область", NameBe = "Магілёўская вобласць", NameEn = "Mogilev Region" },
            new Region { Id = 7, Code = "minsk_city", NameRu = "г. Минск", NameBe = "г. Мінск", NameEn = "Minsk City" }
        );

        // Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Slug = "castle", NameRu = "Замок", NameBe = "Замак", NameEn = "Castle", IconClass = "icon-castle", ColorHex = "#8B1A2A" },
            new Category { Id = 2, Slug = "church", NameRu = "Церковь", NameBe = "Царква", NameEn = "Church", IconClass = "icon-church", ColorHex = "#3B5E3F" },
            new Category { Id = 3, Slug = "estate", NameRu = "Усадьба", NameBe = "Сядзіба", NameEn = "Estate", IconClass = "icon-estate", ColorHex = "#7A5C1E" },
            new Category { Id = 4, Slug = "katedral", NameRu = "Костёл", NameBe = "Касцёл", NameEn = "Cathedral", IconClass = "icon-cathedral", ColorHex = "#2A4A7F" },
            new Category { Id = 5, Slug = "monastery", NameRu = "Монастырь", NameBe = "Манастыр", NameEn = "Monastery", IconClass = "icon-monastery", ColorHex = "#5B3A7A" },
            new Category { Id = 6, Slug = "hillfort", NameRu = "Городище", NameBe = "Гарадзішча", NameEn = "Hillfort", IconClass = "icon-hillfort", ColorHex = "#4A6741" },
            new Category { Id = 7, Slug = "mosque", NameRu = "Мечеть", NameBe = "Мячэць", NameEn = "Mosque", IconClass = "icon-mosque", ColorHex = "#1A6B5E" },
            new Category { Id = 8, Slug = "synagogue", NameRu = "Синагога", NameBe = "Сінагога", NameEn = "Synagogue", IconClass = "icon-synagogue", ColorHex = "#8B6B1A" },
            new Category { Id = 9, Slug = "manor", NameRu = "Дворец", NameBe = "Палац", NameEn = "Palace", IconClass = "icon-manor", ColorHex = "#6B1A5B" },
            new Category { Id = 10, Slug = "other", NameRu = "Прочее", NameBe = "Іншае", NameEn = "Other", IconClass = "icon-other", ColorHex = "#555555" }
        );

        // ArchStyles
        modelBuilder.Entity<ArchStyle>().HasData(
            new ArchStyle { Id = 1, Slug = "gothic", NameRu = "Готика", NameBe = "Готыка", NameEn = "Gothic" },
            new ArchStyle { Id = 2, Slug = "renaissance", NameRu = "Ренессанс", NameBe = "Рэнесанс", NameEn = "Renaissance" },
            new ArchStyle { Id = 3, Slug = "baroque", NameRu = "Барокко", NameBe = "Барока", NameEn = "Baroque" },
            new ArchStyle { Id = 4, Slug = "classicism", NameRu = "Классицизм", NameBe = "Класіцызм", NameEn = "Classicism" },
            new ArchStyle { Id = 5, Slug = "eclecticism", NameRu = "Эклектика", NameBe = "Эклектыка", NameEn = "Eclecticism" },
            new ArchStyle { Id = 6, Slug = "modernity", NameRu = "Модерн", NameBe = "Мадэрн", NameEn = "Art Nouveau" },
            new ArchStyle { Id = 7, Slug = "constructivism", NameRu = "Конструктивизм", NameBe = "Канструктывізм", NameEn = "Constructivism" },
            new ArchStyle { Id = 8, Slug = "wooden", NameRu = "Деревянное зодчество", NameBe = "Драўлянае дойлідства", NameEn = "Wooden Architecture" },
            new ArchStyle { Id = 9, Slug = "brick", NameRu = "Кирпичный стиль", NameBe = "Цагляны стыль", NameEn = "Brick Style" }
        );

        // Tags
        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = 1, Slug = "gdk", NameRu = "ВКЛ", NameBe = "ВКЛ", NameEn = "GDL", ColorHex = "#3B5E3F" },
            new Tag { Id = 2, Slug = "ww2", NameRu = "Великая Отечественная", NameBe = "Вялікая Айчынная", NameEn = "WWII", ColorHex = "#8B1A2A" },
            new Tag { Id = 3, Slug = "wooden", NameRu = "Деревянное зодчество", NameBe = "Драўлянае дойлідства", NameEn = "Wooden Heritage", ColorHex = "#7A5C1E" },
            new Tag { Id = 4, Slug = "defensive", NameRu = "Оборонительная", NameBe = "Абарончая", NameEn = "Defensive", ColorHex = "#2A3A5A" },
            new Tag { Id = 5, Slug = "royal", NameRu = "Королевская", NameBe = "Каралеўская", NameEn = "Royal", ColorHex = "#7A1A6B" },
            new Tag { Id = 6, Slug = "radzivill", NameRu = "Радзивиллы", NameBe = "Радзівілы", NameEn = "Radziwill", ColorHex = "#4A2A1A" },
            new Tag { Id = 7, Slug = "orthodox", NameRu = "Православная", NameBe = "Праваслаўная", NameEn = "Orthodox", ColorHex = "#1A4A7A" },
            new Tag { Id = 8, Slug = "catholic", NameRu = "Католическая", NameBe = "Каталіцкая", NameEn = "Catholic", ColorHex = "#1A6B5E" },
            new Tag { Id = 9, Slug = "slucak", NameRu = "Слуцкое наследие", NameBe = "Слуцкая спадчына", NameEn = "Slutsk Heritage", ColorHex = "#C68B2A" },
            new Tag { Id = 10, Slug = "renaissance", NameRu = "Ренессанс", NameBe = "Рэнесанс", NameEn = "Renaissance", ColorHex = "#5B3A7A" }
        );
    }
}
