using System.Text;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Config;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Build NpgsqlDataSource with all PostgreSQL enum type mappings
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

builder.Services.AddDbContext<ITHunterviewContext>(options =>
    options.UseNpgsql(dataSource)
           .ConfigureWarnings(w => w.Ignore(
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddApplicationServices();

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ITHunterviewContext>();
        // Ensure Database is created/migrated before seeding (optional but good practice)
        await context.Database.MigrateAsync();
        
        await ITHunterview.Service.Infrastructure.Persistence.DataSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
