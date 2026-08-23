using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;
using PolySport.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // Wiederholversuche, weil die Datenbank in einer Container-Umgebung
    // kurzzeitig wegfallen oder noch hochfahren kann.
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Statt E-Mail-Bestätigung entscheidet die Admin-Freigabe, ob sich ein Konto anmelden darf.
// Muss NACH AddDefaultIdentity registriert werden, damit es die Standard-Implementierung ersetzt.
builder.Services.AddScoped<IUserConfirmation<ApplicationUser>, AdminApprovalUserConfirmation>();

// Standardmässig prüft Identity das Anmelde-Cookie nur alle 30 Minuten gegen die DB.
// Damit ein Freigabe-Entzug sofort greift, wird bei jedem Request geprüft.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Datenbank vorbereiten und Admin anlegen
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Migrationen selbst anwenden: bei einer frischen Installation existiert
    // die Datenbank noch nicht, und der SQL-Server-Container braucht nach dem
    // Start eine Weile. Darum geduldig warten statt sofort abzustürzen.
    const int maxAttempts = 60;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Datenbank ist auf dem aktuellen Stand.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning("Datenbank noch nicht bereit (Versuch {Attempt}/{Max}): {Message}",
                attempt, maxAttempts, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
        await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));

    // Zugangsdaten des ersten Admins kommen aus der Konfiguration
    // (Umgebungsvariablen Seed__AdminEmail / Seed__AdminPassword).
    // Ausserhalb der Entwicklung gibt es bewusst keinen Rückfallwert: ein
    // fest eingebautes Passwort in einem öffentlichen Repository wäre offen
    // für jeden. Fehlt die Angabe, wird kein Konto angelegt.
    var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@admin.com";
    var adminPassword = builder.Configuration["Seed:AdminPassword"]
        ?? (app.Environment.IsDevelopment() ? "Admin123!" : null);

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        if (string.IsNullOrEmpty(adminPassword))
        {
            logger.LogError(
                "Kein Admin-Konto angelegt: Seed__AdminPassword ist nicht gesetzt. " +
                "Bitte die Umgebungsvariable setzen und die Anwendung neu starten.");
        }
        else
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Admin",
                DisplayName = "Administrator",
                CreatedAt = DateTime.UtcNow,
                IsApproved = true,
                ApprovedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                logger.LogInformation("Admin-Konto {Email} wurde angelegt.", adminEmail);
            }
            else
            {
                logger.LogError("Admin-Konto konnte nicht angelegt werden: {Fehler}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    else if (!adminUser.IsApproved)
    {
        // Sonst würde sich der Admin nach Einführung der Freigabe selbst aussperren.
        adminUser.IsApproved = true;
        adminUser.ApprovedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(adminUser);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
