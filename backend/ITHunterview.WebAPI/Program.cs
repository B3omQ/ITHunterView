using System.Text;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Config;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ─── Core Services ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

// ─── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogInterceptor>();

var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.MapEnum<UserStatus>("user_status");
dataSourceBuilder.MapEnum<CompanyVerificationMethod>("company_verification_method");
dataSourceBuilder.MapEnum<CompanyStatus>("company_status");
dataSourceBuilder.MapEnum<ReviewStatus>("review_status");
dataSourceBuilder.MapEnum<SkillStatus>("skill_status");
dataSourceBuilder.MapEnum<JobType>("job_type");
dataSourceBuilder.MapEnum<JobStatus>("job_status");
dataSourceBuilder.MapEnum<ApplicationStatus>("application_status");
dataSourceBuilder.MapEnum<PromotionStatus>("promotion_status");
dataSourceBuilder.MapEnum<DifficultyLevel>("difficulty_level");
dataSourceBuilder.MapEnum<InterviewSessionStatus>("interview_session_status");
dataSourceBuilder.MapEnum<SubscriptionStatus>("subscription_status");
dataSourceBuilder.MapEnum<UserSubscriptionStatus>("user_subscription_status");
dataSourceBuilder.MapEnum<PaymentGateway>("payment_gateway");
dataSourceBuilder.MapEnum<PaymentTargetType>("payment_target_type");
dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
dataSourceBuilder.MapEnum<CreditTransactionType>("credit_transaction_type");
dataSourceBuilder.MapEnum<EmploymentType>("employment_type");
dataSourceBuilder.MapEnum<NotificationType>("notification_type");
dataSourceBuilder.MapEnum<EmailLogStatus>("email_log_status");
dataSourceBuilder.MapEnum<ActivityLogCategory>("activity_log_category");
dataSourceBuilder.MapEnum<ActivityLogStatus>("activity_log_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ITHunterviewContext>((sp, options) =>
{
    options.UseNpgsql(dataSource)
           .AddInterceptors(sp.GetRequiredService<AuditLogInterceptor>())
           .ConfigureWarnings(w => w.Ignore(
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

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
if (!app.Environment.IsEnvironment("Testing"))
{
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
}

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ITHunterview.WebAPI.Middlewares.ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseMiddleware<ITHunterview.WebAPI.Middlewares.UserStatusCheckMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
