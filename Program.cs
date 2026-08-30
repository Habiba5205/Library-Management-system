using Lib_System.Data;
using Lib_System.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

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
