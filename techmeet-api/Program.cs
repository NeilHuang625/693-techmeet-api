using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using techmeet_api.Data;
using techmeet_api.Models;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
            .AllowAnyHeader()
            .AllowAnyOrigin();
    });
});

// Set up the database connection
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Add Identity services
builder.Services.AddDefaultIdentity<User>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    await EnsureRolesAsync(roleManager);
    await EnsureDefaultAdminAsync(userManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make sure the roles exist and if not, create them
async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = ["user", "vip", "admin"];
    foreach (var roleName in roleNames)
    {
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            var addToRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!addToRoleResult.Succeeded)
            {
                throw new Exception($"Could not create role {roleName}");
            }
        }
    }
}

// Make sure the default admin user exitst and if not, create it
async Task EnsureDefaultAdminAsync(UserManager<User> userManager)
{
    var adminEmail = Environment.GetEnvironmentVariable("AdminEmail");
    var adminPassword = Environment.GetEnvironmentVariable("AdminPassword");
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var newAdminUser = new User { Nickname = "Admin", Email = adminEmail, UserName = adminEmail };
        var result = await userManager.CreateAsync(newAdminUser, adminPassword);
        if (result.Succeeded)
        {
            var addToUserResult = await userManager.AddToRoleAsync(newAdminUser, "admin");
            if (!addToUserResult.Succeeded)
            {
                throw new Exception($"Could not add user {newAdminUser.Email} to role admin");
            }

        }
    }
}