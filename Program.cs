using Lib_System.Data;
using Lib_System.Models;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Lib_System.Repositories;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services;
using Lib_System.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IBorrowingRepository, BorrowingRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IFineRepository, FineRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IBorrowingService, BorrowingService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Apply any pending EF Core migrations at startup so the database schema
// matches the model (ensures tables such as Fines exist) and verify the
// Fines table is present after migration.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    db.Database.Migrate();

    var connection = db.Database.GetDbConnection();
    try
    {
        if (connection.State == ConnectionState.Closed)
            connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Fines'";
        var result = cmd.ExecuteScalar();
        var count = Convert.ToInt32(result ?? 0);

        if (count == 0)
        {
            logger.LogError("After migration, the 'Fines' table was not found in the target database.");
        }
        else
        {
            logger.LogInformation("Verified: 'Fines' table exists in the target database.");
        }
    }
    catch (Exception ex)
    {
        var logger2 = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger2.LogError(ex, "Error while verifying presence of 'Fines' table after migration.");
    }
    finally
    {
        if (connection.State == ConnectionState.Open)
            connection.Close();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await SeedDefaultUsersAsync(app.Services);

app.Run();

static async Task SeedDefaultUsersAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = new PasswordHasher<User>();

    var roles = await context.Roles.ToDictionaryAsync(r => r.RoleName);

    async Task EnsureUserAsync(string roleName, string name, string email, string username, string password)
    {
        if (!roles.TryGetValue(roleName, out var role) || await context.Users.AnyAsync(u => u.Username == username))
        {
            return;
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Username = username,
            RoleId = role.RoleId,
            Status = "Active",
            RegistrationDate = DateTime.Today,
            CreatedDate = DateTime.Now
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        context.Users.Add(user);
    }

    await EnsureUserAsync("Admin", "Admin Supervisor", "admin@library.local", "admin", "Admin@123");
    await EnsureUserAsync("Manager", "Library Manager", "manager@library.local", "manager", "Manager@123");
    await EnsureUserAsync("Member", "Library Member", "member@library.local", "member", "Member@123");

    await context.SaveChangesAsync();
}
