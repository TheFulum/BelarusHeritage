using BelarusHeritage.Models.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace BelarusHeritage.Data;

public static class DbSeeder
{
    private static readonly string[] Roles =
    {
        "user",
        "moderator",
        "admin"
    };

    private const string DefaultAdminEmail = "admin.heritage@gmail.com";
    private const string DefaultAdminUserName = "admin.heritage";
    private const string DefaultAdminPassword = "admin.heritage";
    private const string DefaultAdminRole = "admin";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        await EnsureRoutePrivacyColumnsAsync(db);
        await SeedRolesAsync(roleManager);
        await MigrateLegacyRolesAsync(userManager, roleManager, db);
        var admin = await SeedAdminAsync(userManager);
        await SeedHeritageObjectsAsync(db, admin.Id, env.WebRootPath);
        await SeedTimelineEventsAsync(db);
        await SeedQuizzesAsync(db);
        await SeedSiteSettingsAsync(db);
    }

    /// <summary>
    /// Migrates from the older 5-role layout (superadmin / admin / moderator / editor / user)
    /// to the current 3-role layout (admin / moderator / user). Idempotent.
    /// </summary>
    private static async Task MigrateLegacyRolesAsync(
        UserManager<User> userManager,
        RoleManager<UserRole> roleManager,
        AppDbContext db)
    {
        // (oldRole, newRole) pairs
        var roleMigrations = new[]
        {
            ("superadmin", "admin"),
            ("editor",     "user")
        };

        foreach (var (oldRole, newRole) in roleMigrations)
        {
            var legacyRole = await roleManager.FindByNameAsync(oldRole);
            if (legacyRole is null)
                continue;

            // Move every user from oldRole to newRole
            var usersInLegacy = await userManager.GetUsersInRoleAsync(oldRole);
            foreach (var u in usersInLegacy)
            {
                if (!await userManager.IsInRoleAsync(u, newRole))
                    await userManager.AddToRoleAsync(u, newRole);
                await userManager.RemoveFromRoleAsync(u, oldRole);

                // Also fix the legacy User.Role string field, if it points at the old role
                if (string.Equals(u.Role, oldRole, StringComparison.OrdinalIgnoreCase))
                {
                    u.Role = newRole;
                    await userManager.UpdateAsync(u);
                }
            }

            // Delete the legacy role itself
            await roleManager.DeleteAsync(legacyRole);
        }

        // Backstop: any user whose User.Role string still references a removed role
        var stragglers = await db.Users
            .Where(u => u.Role == "superadmin" || u.Role == "editor")
            .ToListAsync();
        foreach (var u in stragglers)
        {
            u.Role = u.Role == "superadmin" ? "admin" : "user";
        }
        if (stragglers.Count > 0)
            await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<UserRole> roleManager)
    {
        foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var role = new UserRole { Name = roleName };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }

    private static async Task EnsureRoutePrivacyColumnsAsync(AppDbContext db)
    {
        async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @tableName
  AND COLUMN_NAME = @columnName";

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@tableName";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);

            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@columnName";
            columnParam.Value = columnName;
            command.Parameters.Add(columnParam);

            var raw = await command.ExecuteScalarAsync();
            var count = raw == null || raw == DBNull.Value ? 0 : Convert.ToInt32(raw);
            return count > 0;
        }

        async Task EnsureColumnAsync(string tableName, string columnName, string alterSql)
        {
            if (!await ColumnExistsAsync(tableName, columnName))
            {
                await db.Database.ExecuteSqlRawAsync(alterSql);
            }
        }

        await EnsureColumnAsync("routes", "start_address", "ALTER TABLE routes ADD COLUMN start_address VARCHAR(255) NULL");
        await EnsureColumnAsync("routes", "start_lat", "ALTER TABLE routes ADD COLUMN start_lat DECIMAL(10,7) NULL");
        await EnsureColumnAsync("routes", "start_lng", "ALTER TABLE routes ADD COLUMN start_lng DECIMAL(10,7) NULL");
        await EnsureColumnAsync("routes", "end_address", "ALTER TABLE routes ADD COLUMN end_address VARCHAR(255) NULL");
        await EnsureColumnAsync("routes", "end_lat", "ALTER TABLE routes ADD COLUMN end_lat DECIMAL(10,7) NULL");
        await EnsureColumnAsync("routes", "end_lng", "ALTER TABLE routes ADD COLUMN end_lng DECIMAL(10,7) NULL");
        await EnsureColumnAsync("routes", "source_route_id", "ALTER TABLE routes ADD COLUMN source_route_id INT NULL");
        await EnsureColumnAsync("routes", "source_route_title", "ALTER TABLE routes ADD COLUMN source_route_title VARCHAR(255) NULL");
        await EnsureColumnAsync("routes", "source_route_share_token", "ALTER TABLE routes ADD COLUMN source_route_share_token VARCHAR(32) NULL");
    }

    private static async Task<User> SeedAdminAsync(UserManager<User> userManager)
    {
        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (admin is not null)
        {
            if (!await userManager.IsInRoleAsync(admin, DefaultAdminRole))
                await userManager.AddToRoleAsync(admin, DefaultAdminRole);
            return admin;
        }

        admin = new User
        {
            Email = DefaultAdminEmail,
            UserName = DefaultAdminUserName,
            DisplayName = "Super Admin",
            PreferredLang = "ru",
            EmailConfirmed = true,
            IsActive = true,
            Role = DefaultAdminRole
        };

        var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create default admin: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(admin, DefaultAdminRole);
        return admin;
    }

    private static async Task SeedHeritageObjectsAsync(AppDbContext db, int adminId, string webRootPath)
    {
        // Reference IDs match the seed order in 001_InitialSchema.sql:
        //   categories: castle=1, church=2, estate=3, katedral=4, monastery=5,
        //               hillfort=6, mosque=7, synagogue=8, manor=9, other=10
        //   regions: brest=1, vitebsk=2, gomel=3, grodno=4, minsk=5, mogilev=6, minsk_city=7
        //   arch_styles: gothic=1, renaissance=2, baroque=3, classicism=4, eclecticism=5
        var seeds = new[]
        {
            new HeritageSeed(
                Slug: "mirski-zamak",
                NameRu: "Мирский замок", NameBe: "Мірскі замак", NameEn: "Mir Castle",
                CategoryId: 1, RegionId: 4, ArchStyleId: 2,
                CenturyStart: 16, CenturyEnd: 20, BuildYear: 1527,
                ShortDescRu: "Средневековый замок XVI века в посёлке Мир, объект Всемирного наследия ЮНЕСКО.",
                ShortDescBe: "Сярэднявечны замак XVI стагоддзя ў пасёлку Мір, аб'ект Сусветнай спадчыны ЮНЕСКА.",
                ShortDescEn: "16th-century medieval castle in the town of Mir, a UNESCO World Heritage site.",
                FunFactRu: "Замок за свою историю принадлежал Ильиничам, Радзивиллам и Святополк-Мирским.",
                FunFactBe: "Замак за сваю гісторыю належаў Ільінічам, Радзівілам і Святаполк-Мірскім.",
                FunFactEn: "Over its history the castle belonged to the Ilinichs, the Radziwiłłs and the Sviatopolk-Mirskis.",
                Architect: "Юрий Ильинич",
                Lat: 53.4511m, Lng: 26.4734m,
                AddressRu: "г.п. Мир, Кореличский район, Гродненская область",
                AddressBe: "г.п. Мір, Карэліцкі раён, Гродзенская вобласць",
                AddressEn: "Mir, Karelichy District, Grodno Region",
                MainImageUrl: "https://upload.wikimedia.org/wikipedia/commons/thumb/0/00/Belarus_-_Mir_Castle_-_01.jpg/1280px-Belarus_-_Mir_Castle_-_01.jpg",
                IsFeatured: true,
                HeritageCategory: 1, HeritageYear: 2000
            ),
            new HeritageSeed(
                Slug: "niasvizh-zamak",
                NameRu: "Несвижский замок", NameBe: "Нясвіжскі замак", NameEn: "Nesvizh Castle",
                CategoryId: 1, RegionId: 5, ArchStyleId: 3,
                CenturyStart: 16, CenturyEnd: 18, BuildYear: 1583,
                ShortDescRu: "Резиденция рода Радзивиллов в Несвиже, памятник архитектуры эпохи барокко, ЮНЕСКО.",
                ShortDescBe: "Рэзідэнцыя роду Радзівілаў у Нясвіжы, помнік архітэктуры эпохі барока, ЮНЕСКА.",
                ShortDescEn: "Residence of the Radziwiłł dynasty in Nesvizh, a Baroque architectural monument, UNESCO.",
                FunFactRu: "Считается одной из первых в Европе крепостей новоитальянского бастионного типа.",
                FunFactBe: "Лічыцца адной з першых у Еўропе крэпасцей новаітальянскага бастыённага тыпу.",
                FunFactEn: "Considered one of the first fortresses in Europe built in the new Italian bastion style.",
                Architect: "Джованни Бернардони",
                Lat: 53.2228m, Lng: 26.6912m,
                AddressRu: "г. Несвиж, Минская область",
                AddressBe: "г. Нясвіж, Мінская вобласць",
                AddressEn: "Nesvizh, Minsk Region",
                MainImageUrl: "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9d/Belarus-Nesvizh-Castle-22.jpg/1280px-Belarus-Nesvizh-Castle-22.jpg",
                IsFeatured: true,
                HeritageCategory: 1, HeritageYear: 2005
            ),
            new HeritageSeed(
                Slug: "lida-zamak",
                NameRu: "Лидский замок", NameBe: "Лідскі замак", NameEn: "Lida Castle",
                CategoryId: 1, RegionId: 4, ArchStyleId: 1,
                CenturyStart: 14, CenturyEnd: 14, BuildYear: 1323,
                ShortDescRu: "Каменный замок XIV века, построенный князем Гедимином на севере Лиды.",
                ShortDescBe: "Каменны замак XIV стагоддзя, пабудаваны князем Гедымінам на поўначы Ліды.",
                ShortDescEn: "14th-century stone castle built by Grand Duke Gediminas in the north of Lida.",
                FunFactRu: "Один из немногих сохранившихся в Беларуси замков бастионно-крепостной архитектуры эпохи ВКЛ.",
                FunFactBe: "Адзін з нямногіх захаваных у Беларусі замкаў бастыённа-крэпаснай архітэктуры эпохі ВКЛ.",
                FunFactEn: "One of the few surviving GDL-era castles in Belarus built in the bastion-fortress style.",
                Architect: null,
                Lat: 53.8943m, Lng: 25.2932m,
                AddressRu: "г. Лида, ул. Замковая, Гродненская область",
                AddressBe: "г. Ліда, вул. Замкавая, Гродзенская вобласць",
                AddressEn: "Lida, Zamkavaya St, Grodno Region",
                MainImageUrl: "https://upload.wikimedia.org/wikipedia/commons/thumb/3/35/Belarus-Lida-Castle-2.jpg/1280px-Belarus-Lida-Castle-2.jpg",
                IsFeatured: true,
                HeritageCategory: 2, HeritageYear: 2004
            ),
            new HeritageSeed(
                Slug: "polacki-safijski-sabor",
                NameRu: "Софийский собор в Полоцке", NameBe: "Сафійскі сабор у Полацку", NameEn: "Saint Sophia Cathedral, Polotsk",
                CategoryId: 2, RegionId: 2, ArchStyleId: 3,
                CenturyStart: 11, CenturyEnd: 18, BuildYear: 1066,
                ShortDescRu: "Один из старейших храмов Беларуси, заложен в XI веке и перестроен в XVIII веке в стиле виленского барокко.",
                ShortDescBe: "Адзін з найстарэйшых храмаў Беларусі, закладзены ў XI стагоддзі і перабудаваны ў XVIII стагоддзі ў стылі віленскага барока.",
                ShortDescEn: "One of the oldest churches in Belarus, founded in the 11th century and rebuilt in the 18th century in the Vilna Baroque style.",
                FunFactRu: "Был четвёртым Софийским собором, построенным после соборов в Константинополе, Киеве и Новгороде.",
                FunFactBe: "Быў чацвёртым Сафійскім саборам, пабудаваным пасля сабораў у Канстанцінопалі, Кіеве і Ноўгарадзе.",
                FunFactEn: "It was the fourth Saint Sophia Cathedral after those in Constantinople, Kyiv and Novgorod.",
                Architect: "Иоганн Кристоф Глаубиц",
                Lat: 55.4859m, Lng: 28.7565m,
                AddressRu: "г. Полоцк, ул. Замковая, Витебская область",
                AddressBe: "г. Полацк, вул. Замкавая, Віцебская вобласць",
                AddressEn: "Polotsk, Zamkavaya St, Vitebsk Region",
                MainImageUrl: null,
                IsFeatured: true,
                HeritageCategory: 1, HeritageYear: 2004
            ),
            new HeritageSeed(
                Slug: "kasciol-sviatych-symona-i-aleny",
                NameRu: "Костёл Святых Симеона и Елены", NameBe: "Касцёл Святых Сымона і Алены", NameEn: "Church of Saints Simon and Helena",
                CategoryId: 4, RegionId: 7, ArchStyleId: 5,
                CenturyStart: 20, CenturyEnd: 20, BuildYear: 1910,
                ShortDescRu: "Минский Красный костёл — памятник неоготической эклектики начала XX века на площади Независимости.",
                ShortDescBe: "Мінскі Чырвоны касцёл — помнік неагатычнай эклектыкі пачатку XX стагоддзя на плошчы Незалежнасці.",
                ShortDescEn: "Minsk's Red Church — an early-20th-century Neo-Gothic eclectic landmark on Independence Square.",
                FunFactRu: "Костёл построен на средства Эдварда Войниловича в память о его рано умерших детях — Симеоне и Елене.",
                FunFactBe: "Касцёл узведзены на сродкі Эдварда Вайніловіча ў памяць пра яго рана памерлых дзяцей — Сымона і Алену.",
                FunFactEn: "The church was funded by Edward Woyniłłowicz in memory of his children Simon and Helena who died young.",
                Architect: "Томаш Пайздерский",
                Lat: 53.8964m, Lng: 27.5468m,
                AddressRu: "г. Минск, пл. Независимости, 15",
                AddressBe: "г. Мінск, пл. Незалежнасці, 15",
                AddressEn: "Minsk, Independence Square 15",
                MainImageUrl: null,
                IsFeatured: false,
                HeritageCategory: 2, HeritageYear: 1988
            ),
            new HeritageSeed(
                Slug: "homelski-palac-rumiancavych-paskievicau",
                NameRu: "Дворец Румянцевых и Паскевичей", NameBe: "Палац Румянцавых і Паскевічаў", NameEn: "Rumyantsev–Paskevich Palace",
                CategoryId: 9, RegionId: 3, ArchStyleId: 4,
                CenturyStart: 18, CenturyEnd: 19, BuildYear: 1794,
                ShortDescRu: "Архитектурный ансамбль в Гомеле, заложенный фельдмаршалом Румянцевым и достроенный родом Паскевичей в стиле классицизма.",
                ShortDescBe: "Архітэктурны ансамбль у Гомелі, закладзены фельдмаршалам Румянцавым і дабудаваны родам Паскевічаў у стылі класіцызму.",
                ShortDescEn: "A palace-park ensemble in Homel founded by Field Marshal Rumyantsev and completed by the Paskevich family in the Classicist style.",
                FunFactRu: "В парке дворца находится фамильная часовня-усыпальница Паскевичей с уникальной майоликовой отделкой.",
                FunFactBe: "У парку палаца знаходзіцца фамільная капліца-пахавальня Паскевічаў з унікальнай маёлікавай аздобай.",
                FunFactEn: "The palace park contains the Paskevich family chapel-tomb decorated with unique majolica tiling.",
                Architect: "Иван Старов",
                Lat: 52.4244m, Lng: 30.9826m,
                AddressRu: "г. Гомель, пл. Ленина, 4",
                AddressBe: "г. Гомель, пл. Леніна, 4",
                AddressEn: "Homel, Lenin Square 4",
                MainImageUrl: null,
                IsFeatured: true,
                HeritageCategory: 1, HeritageYear: 2004
            ),
            new HeritageSeed(
                Slug: "kamianieckaja-vieza",
                NameRu: "Каменецкая башня (Белая вежа)", NameBe: "Камянецкая вежа (Белая вежа)", NameEn: "Kamianets Tower (White Tower)",
                CategoryId: 10, RegionId: 1, ArchStyleId: 1,
                CenturyStart: 13, CenturyEnd: 13, BuildYear: 1276,
                ShortDescRu: "Оборонительная башня-донжон XIII века в Каменце — единственная сохранившаяся «волынская» вежа.",
                ShortDescBe: "Абарончая вежа-данжон XIII стагоддзя ў Камянцы — адзіная захаваная «валынская» вежа.",
                ShortDescEn: "A 13th-century donjon defensive tower in Kamianets — the only surviving Volhynian-type tower.",
                FunFactRu: "Несмотря на название «Белая вежа», подлинный кирпич башни — тёмно-красный; белой её сделали в реставрации XX века, потом вернули исторический цвет.",
                FunFactBe: "Нягледзячы на назву «Белая вежа», аўтэнтычная цэгла — цёмна-чырвоная; белай яе зрабілі рэстаўрацыяй XX стагоддзя, пасля вярнулі гістарычны колер.",
                FunFactEn: "Despite the nickname \"White Tower\", its original brick is dark red — the white plaster was a 20th-century restoration that has since been removed.",
                Architect: "Мастер Алекса",
                Lat: 52.4053m, Lng: 23.8200m,
                AddressRu: "г. Каменец, ул. Ленина, Брестская область",
                AddressBe: "г. Камянец, вул. Леніна, Брэсцкая вобласць",
                AddressEn: "Kamianets, Lenin St, Brest Region",
                MainImageUrl: null,
                IsFeatured: true,
                HeritageCategory: 1, HeritageYear: 2004
            ),
            new HeritageSeed(
                Slug: "mahileuski-mikalajeuski-manastyr",
                NameRu: "Свято-Никольский монастырь в Могилёве", NameBe: "Свята-Мікалаеўскі манастыр у Магілёве", NameEn: "Saint Nicholas Monastery, Mogilev",
                CategoryId: 5, RegionId: 6, ArchStyleId: 3,
                CenturyStart: 17, CenturyEnd: 17, BuildYear: 1672,
                ShortDescRu: "Православный женский монастырь XVII века с Никольским собором — выдающимся образцом могилёвского барокко.",
                ShortDescBe: "Праваслаўны жаночы манастыр XVII стагоддзя з Мікалаеўскім саборам — выдатным узорам магілёўскага барока.",
                ShortDescEn: "A 17th-century Orthodox convent whose Saint Nicholas Cathedral is an outstanding example of Mogilev Baroque.",
                FunFactRu: "Иконостас Никольского собора — пятиярусный, резной, признан памятником искусства мирового значения.",
                FunFactBe: "Іканастас Мікалаеўскага сабора — пяціярусны, разьблёны, прызнаны помнікам мастацтва сусветнага значэння.",
                FunFactEn: "The cathedral's five-tier carved iconostasis is recognised as a work of art of global significance.",
                Architect: null,
                Lat: 53.8825m, Lng: 30.3437m,
                AddressRu: "г. Могилёв, ул. Сурты, 19",
                AddressBe: "г. Магілёў, вул. Сурты, 19",
                AddressEn: "Mogilev, Surty St 19",
                MainImageUrl: null,
                IsFeatured: false,
                HeritageCategory: 1, HeritageYear: 2004
            )
        };

        var existingSlugs = await db.HeritageObjects
            .Where(o => seeds.Select(s => s.Slug).Contains(o.Slug))
            .Select(o => o.Slug)
            .ToListAsync();

        Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "seed"));

        foreach (var s in seeds)
        {
            var localMainImageUrl = await TryCacheSeedImageAsync(webRootPath, s.Slug, s.MainImageUrl);

            if (existingSlugs.Contains(s.Slug))
            {
                // Update only "safe" seed fields, and only replace external images with local cached ones.
                var existing = await db.HeritageObjects
                    .Include(o => o.Location)
                    .FirstAsync(o => o.Slug == s.Slug);

                existing.NameRu = s.NameRu;
                existing.NameBe = s.NameBe;
                existing.NameEn = s.NameEn;
                existing.CategoryId = s.CategoryId;
                existing.RegionId = s.RegionId;
                existing.ArchStyleId = s.ArchStyleId;
                existing.CenturyStart = s.CenturyStart;
                existing.CenturyEnd = s.CenturyEnd;
                existing.BuildYear = s.BuildYear;
                existing.ShortDescRu = s.ShortDescRu;
                existing.ShortDescBe = s.ShortDescBe;
                existing.ShortDescEn = s.ShortDescEn;
                existing.FunFactRu = s.FunFactRu;
                existing.FunFactBe = s.FunFactBe;
                existing.FunFactEn = s.FunFactEn;
                existing.Architect = s.Architect;
                existing.HeritageCategory = s.HeritageCategory;
                existing.HeritageYear = s.HeritageYear;
                existing.IsFeatured = s.IsFeatured;
                existing.UpdatedBy = adminId;

                if (!string.IsNullOrWhiteSpace(localMainImageUrl))
                {
                    // Prefer local cached image if previous was external or empty.
                    if (string.IsNullOrWhiteSpace(existing.MainImageUrl) ||
                        existing.MainImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                        existing.MainImageUrl.Contains("upload.wikimedia.org", StringComparison.OrdinalIgnoreCase))
                    {
                        existing.MainImageUrl = localMainImageUrl;
                    }
                }

                if (existing.Location == null)
                {
                    existing.Location = new ObjectLocation
                    {
                        Lat = s.Lat,
                        Lng = s.Lng,
                        AddressRu = s.AddressRu,
                        AddressBe = s.AddressBe,
                        AddressEn = s.AddressEn,
                        MapZoom = 15
                    };
                }
                else
                {
                    existing.Location.Lat = s.Lat;
                    existing.Location.Lng = s.Lng;
                    existing.Location.AddressRu = s.AddressRu;
                    existing.Location.AddressBe = s.AddressBe;
                    existing.Location.AddressEn = s.AddressEn;
                    existing.Location.MapZoom = 15;
                }

                continue;
            }

            var obj = new HeritageObject
            {
                Slug = s.Slug,
                NameRu = s.NameRu, NameBe = s.NameBe, NameEn = s.NameEn,
                CategoryId = s.CategoryId,
                RegionId = s.RegionId,
                ArchStyleId = s.ArchStyleId,
                CenturyStart = s.CenturyStart,
                CenturyEnd = s.CenturyEnd,
                BuildYear = s.BuildYear,
                ShortDescRu = s.ShortDescRu,
                ShortDescBe = s.ShortDescBe,
                ShortDescEn = s.ShortDescEn,
                FunFactRu = s.FunFactRu,
                FunFactBe = s.FunFactBe,
                FunFactEn = s.FunFactEn,
                Architect = s.Architect,
                HeritageCategory = s.HeritageCategory,
                HeritageYear = s.HeritageYear,
                PreservationStatus = PreservationStatus.Preserved,
                IsVisitable = true,
                MainImageUrl = localMainImageUrl ?? s.MainImageUrl,
                Status = ObjectStatus.Published,
                IsFeatured = s.IsFeatured,
                CreatedBy = adminId,
                UpdatedBy = adminId,
                Location = new ObjectLocation
                {
                    Lat = s.Lat,
                    Lng = s.Lng,
                    AddressRu = s.AddressRu,
                    AddressBe = s.AddressBe,
                    AddressEn = s.AddressEn,
                    MapZoom = 15
                }
            };

            db.HeritageObjects.Add(obj);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<string?> TryCacheSeedImageAsync(string webRootPath, string slug, string? remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
            return null;

        if (!remoteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !remoteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return null;

        var uploadsDir = Path.Combine(webRootPath, "uploads", "seed");
        var fileName = $"{slug}.jpg";
        var filePath = Path.Combine(uploadsDir, fileName);
        var publicUrl = $"/uploads/seed/{fileName}";

        // Already cached
        if (File.Exists(filePath))
            return publicUrl;

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BelarusHeritageSeeder/1.0");
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif"));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));

            using var res = await http.GetAsync(remoteUrl);
            if (!res.IsSuccessStatusCode)
                return null;

            var bytes = await res.Content.ReadAsByteArrayAsync();
            if (bytes.Length < 1024) // sanity
                return null;

            await File.WriteAllBytesAsync(filePath, bytes);
            return publicUrl;
        }
        catch
        {
            return null;
        }
    }

    private static async Task SeedTimelineEventsAsync(AppDbContext db)
    {
        var events = new List<TimelineEvent>
        {
            new()
            {
                Year = 1066,
                TitleRu = "Основание Софийского собора в Полоцке",
                TitleBe = "Заснаванне Сафійскага сабора ў Полацку",
                TitleEn = "Foundation of Saint Sophia Cathedral in Polotsk",
                BodyRu = "Один из древнейших каменных храмов на территории Беларуси.",
                BodyEn = "One of the oldest stone churches in the territory of Belarus.",
                IsPublished = true,
                SortOrder = 0
            },
            new()
            {
                Year = 1323,
                TitleRu = "Строительство Лидского замка",
                TitleBe = "Будаўніцтва Лідскага замка",
                TitleEn = "Construction of Lida Castle",
                BodyRu = "Крепость эпохи Великого княжества Литовского.",
                BodyEn = "A fortress from the era of the Grand Duchy of Lithuania.",
                IsPublished = true,
                SortOrder = 1
            },
            new()
            {
                Year = 1583,
                TitleRu = "Начало строительства Несвижского замка",
                TitleBe = "Пачатак будаўніцтва Нясвіжскага замка",
                TitleEn = "Start of Nesvizh Castle construction",
                BodyRu = "Резиденция рода Радзивиллов, включена в список ЮНЕСКО.",
                BodyEn = "Residence of the Radziwill family, now a UNESCO site.",
                IsPublished = true,
                SortOrder = 2
            }
            ,
            // Extra 5 events (safe to re-run; added only if missing)
            new()
            {
                Year = 1527,
                TitleRu = "Строительство Мирского замка",
                TitleBe = "Будаўніцтва Мірскага замка",
                TitleEn = "Construction of Mir Castle",
                BodyRu = "Один из самых узнаваемых замков Беларуси (XVI век).",
                BodyEn = "One of the most recognizable castles in Belarus (16th century).",
                IsPublished = true,
                SortOrder = 3
            },
            new()
            {
                Year = 1794,
                TitleRu = "Дворец Румянцевых и Паскевичей в Гомеле",
                TitleBe = "Палац Румянцавых і Паскевічаў у Гомелі",
                TitleEn = "Rumyantsev–Paskevich Palace in Homel",
                BodyRu = "Формирование дворцово-паркового ансамбля конца XVIII — XIX веков.",
                BodyEn = "Formation of a palace-and-park ensemble of the late 18th–19th centuries.",
                IsPublished = true,
                SortOrder = 4
            },
            new()
            {
                Year = 1910,
                TitleRu = "Костёл Святых Симеона и Елены в Минске",
                TitleBe = "Касцёл Святых Сымона і Алены ў Мінску",
                TitleEn = "Church of Saints Simon and Helena in Minsk",
                BodyRu = "Известен как «Красный костёл».",
                BodyEn = "Known as the “Red Church”.",
                IsPublished = true,
                SortOrder = 5
            },
            new()
            {
                Year = 2000,
                TitleRu = "Мирский замок в списке ЮНЕСКО",
                TitleBe = "Мірскі замак у спісе ЮНЕСКА",
                TitleEn = "Mir Castle added to the UNESCO list",
                BodyRu = "Объект Всемирного наследия.",
                BodyEn = "A World Heritage Site.",
                IsPublished = true,
                SortOrder = 6
            },
            new()
            {
                Year = 2005,
                TitleRu = "Несвижский замок в списке ЮНЕСКО",
                TitleBe = "Нясвіжскі замак у спісе ЮНЕСКА",
                TitleEn = "Nesvizh Castle added to the UNESCO list",
                BodyRu = "Включён в объект «Резиденции Радзивиллов».",
                BodyEn = "Included as part of the “Radziwill residences” heritage entry.",
                IsPublished = true,
                SortOrder = 7
            }
        };

        foreach (var e in events)
        {
            var exists = await db.TimelineEvents.AnyAsync(x => x.Year == e.Year && x.TitleRu == e.TitleRu);
            if (!exists)
                db.TimelineEvents.Add(e);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedQuizzesAsync(AppDbContext db)
    {
        if (await db.Quizzes.AnyAsync())
            return;

        var quiz = new Quiz
        {
            Slug = "ugadai-region",
            Type = QuizType.RegionGuess,
            TitleRu = "Угадай регион",
            TitleBe = "Адгадай рэгіён",
            TitleEn = "Guess the region",
            DescriptionRu = "Определите регион Беларуси по описанию объекта.",
            DescriptionEn = "Identify the Belarus region by object description.",
            IsActive = true,
            SortOrder = 0
        };

        var q1 = new QuizQuestion
        {
            SortOrder = 1,
            BodyRu = "В каком регионе находится Мирский замок?",
            BodyBe = "У якім рэгіёне знаходзіцца Мірскі замак?",
            BodyEn = "Which region is Mir Castle located in?"
        };
        q1.Answers = new List<QuizAnswer>
        {
            new() { SortOrder = 1, BodyRu = "Гродненская область", BodyBe = "Гродзенская вобласць", BodyEn = "Grodno Region", IsCorrect = true },
            new() { SortOrder = 2, BodyRu = "Минская область", BodyBe = "Мінская вобласць", BodyEn = "Minsk Region", IsCorrect = false },
            new() { SortOrder = 3, BodyRu = "Брестская область", BodyBe = "Брэсцкая вобласць", BodyEn = "Brest Region", IsCorrect = false },
            new() { SortOrder = 4, BodyRu = "Витебская область", BodyBe = "Віцебская вобласць", BodyEn = "Vitebsk Region", IsCorrect = false }
        };

        var q2 = new QuizQuestion
        {
            SortOrder = 2,
            BodyRu = "В каком регионе находится Софийский собор в Полоцке?",
            BodyBe = "У якім рэгіёне знаходзіцца Сафійскі сабор у Полацку?",
            BodyEn = "Which region is Saint Sophia Cathedral in Polotsk located in?"
        };
        q2.Answers = new List<QuizAnswer>
        {
            new() { SortOrder = 1, BodyRu = "Витебская область", BodyBe = "Віцебская вобласць", BodyEn = "Vitebsk Region", IsCorrect = true },
            new() { SortOrder = 2, BodyRu = "Минская область", BodyBe = "Мінская вобласць", BodyEn = "Minsk Region", IsCorrect = false },
            new() { SortOrder = 3, BodyRu = "Гомельская область", BodyBe = "Гомельская вобласць", BodyEn = "Gomel Region", IsCorrect = false },
            new() { SortOrder = 4, BodyRu = "Могилёвская область", BodyBe = "Магілёўская вобласць", BodyEn = "Mogilev Region", IsCorrect = false }
        };

        quiz.Questions.Add(q1);
        quiz.Questions.Add(q2);

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSiteSettingsAsync(AppDbContext db)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["social.telegram"]  = "",
            ["social.instagram"] = "",
            ["social.vk"]        = "",
            ["social.youtube"]   = ""
        };

        foreach (var (key, value) in defaults)
        {
            if (!await db.SiteSettings.AnyAsync(s => s.Key == key))
                db.SiteSettings.Add(new Models.Domain.SiteSettings { Key = key, Value = value });
        }

        await db.SaveChangesAsync();
    }

    private sealed record HeritageSeed(
        string Slug,
        string NameRu, string NameBe, string NameEn,
        int CategoryId, int RegionId, int? ArchStyleId,
        short? CenturyStart, short? CenturyEnd, int? BuildYear,
        string ShortDescRu, string ShortDescBe, string ShortDescEn,
        string FunFactRu, string FunFactBe, string FunFactEn,
        string? Architect,
        decimal Lat, decimal Lng,
        string AddressRu, string AddressBe, string AddressEn,
        string? MainImageUrl,
        bool IsFeatured,
        int? HeritageCategory, int? HeritageYear
    );
}
