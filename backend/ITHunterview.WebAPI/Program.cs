using System.Text;
using ITHunterview.Service.Config;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ─── Core Services ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

// ─── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ITHunterviewContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Application Services ─────────────────────────────────────────────────────
builder.Services.Configure<ITHunterview.Service.Config.CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<ITHunterview.WebAPI.BackgroundServices.LogCleanupBackgroundService>();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
    };
});

// ─── RBAC Authorization Policies ─────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",        p => p.RequireRole("admin"));
    options.AddPolicy("StaffOnly",        p => p.RequireRole("staff"));
    options.AddPolicy("RecruiterOnly",    p => p.RequireRole("recruiter"));
    options.AddPolicy("CandidateOnly",    p => p.RequireRole("candidate"));
    options.AddPolicy("StaffOrAdmin",     p => p.RequireRole("admin", "staff"));
    options.AddPolicy("RecruiterOrAdmin", p => p.RequireRole("admin", "staff", "recruiter"));
    options.AddPolicy("AllRoles",         p => p.RequireRole("admin", "staff", "recruiter", "candidate"));
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ─── DB Migration & Seeding ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ITHunterviewContext>();
        await context.Database.MigrateAsync();
        await DataSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ITHunterview.WebAPI.Middlewares.UserStatusCheckMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
