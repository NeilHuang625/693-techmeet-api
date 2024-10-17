using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using techmeet_api.Data;
using techmeet_api.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using techmeet_api.Repositories;
using techmeet_api.Middlewares;
using System.Text.Json;
using techmeet_api.BackgroundTasks;
using techmeet_api.Hubs;
using Microsoft.Data.SqlClient;


static void TestDatabaseConnection(string connectionString)
{
    try
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            Console.WriteLine("Connection successful");
        }
    }
    catch (SqlException ex)
    {
        Console.WriteLine("Connection failed: " + ex.Message);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load();

// Add environment variables as configuration sources
builder.Configuration.AddEnvironmentVariables();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("https://main.d37hwvzkpt7c7a.amplifyapp.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllersWithViews();

// TestDatabaseConnection(connectionString);
string? connectionString = builder.Configuration.GetConnectionString("ConnectionString");
if (connectionString != null)
{
    TestDatabaseConnection(connectionString);
}
else
{
    Console.WriteLine("Connection string is null");
}

// Set up the database connection
var connectionStringLocal = builder.Configuration["ConnectionString"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // options.UseSqlServer(connectionStringLocal); // for local development
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString")); // for deployment
});

// Add Identity services
builder.Services.AddDefaultIdentity<User>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add JWT authentication
var jwtKey = builder.Configuration["Jwt_Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT configuration is missing");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt_Issuer"],
            ValidAudience = builder.Configuration["Jwt_Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Response when JWT is invalid
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();

                var jsonResponse = JsonSerializer.Serialize(new { message = "Unauthorized" });
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync(jsonResponse);

                return Task.CompletedTask;
            }
        };
    });

// Add signalR
builder.Services.AddSignalR();

// Background tasks
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddSingleton<NotificationBackgroundService>();

// Add MessageService
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register JWT blacklist service
builder.Services.AddScoped<IJwtBlacklistService, JwtBlacklistService>();

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

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

// Add the JWT blacklist middleware
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");

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

async Task EnsureDefaultAdminAsync(UserManager<User> userManager)
{
    var adminEmail = builder.Configuration["AdminEmail"];
    var adminPassword = builder.Configuration["AdminPassword"];

    if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
    {
        throw new Exception("Admin email or password is not set in the configuration");
    }

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