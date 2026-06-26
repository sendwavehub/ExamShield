using System.Text;
using ExamShield.Api.Endpoints;
using ExamShield.Application.Behaviors;
using ExamShield.Application.Queries.GetOcrResult;
using ExamShield.Application.Commands.RegisterCapture;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Services;
using ExamShield.Infrastructure;
using ExamShield.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<HashVerificationService>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RegisterCaptureCommand).Assembly);
    cfg.AddOpenBehavior(typeof(AlertBehavior<,>));
});

// ── JWT authentication ────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ExamShield",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ExamShield",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ── RBAC policies (hierarchical: higher roles include lower-level access) ─
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Operator", p =>
        p.RequireRole("Operator", "Supervisor", "Auditor", "Administrator", "SecurityOfficer"));
    options.AddPolicy("Supervisor", p =>
        p.RequireRole("Supervisor", "Auditor", "Administrator", "SecurityOfficer"));
    options.AddPolicy("Auditor", p =>
        p.RequireRole("Auditor", "Administrator", "SecurityOfficer"));
    options.AddPolicy("Administrator", p =>
        p.RequireRole("Administrator", "SecurityOfficer"));
    options.AddPolicy("SecurityOfficer", p =>
        p.RequireRole("SecurityOfficer"));
});

var app = builder.Build();

// ── Database migration (skipped in Testing — repositories are in-memory) ──
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ExamShieldDbContext>().Database.Migrate();
}

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = ex switch
    {
        CaptureNotFoundException     => (404, ex.Message),
        DeviceNotFoundException      => (404, ex.Message),
        DuplicateUploadException     => (409, ex.Message),
        HashMismatchException        => (400, ex.Message),
        InvalidSignatureException    => (400, ex.Message),
        CaptureNotUploadedException  => (400, ex.Message),
        OcrResultNotFoundException   => (404, ex.Message),
        InvalidCredentialsException  => (401, ex.Message),
        UserAlreadyExistsException   => (409, ex.Message),
        ArgumentException e          => (400, e.Message),
        _                            => (500, "An unexpected error occurred.")
    };
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new { title, status });
}));

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapDeviceEndpoints();
app.MapCaptureEndpoints();
app.MapUploadEndpoints();
app.MapAuditEndpoints();
app.MapVerifyEndpoints();
app.MapOcrEndpoints();

app.Run();

public partial class Program;
