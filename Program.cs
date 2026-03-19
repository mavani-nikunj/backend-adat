using AdatHisabdubai.Data;
using JangadHisabApp.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();
// ? ADD THIS LINE HERE (TOP, BEFORE builder.Build())
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<AdatHisabAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db")));

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("db"),
        healthQuery: "SELECT 1",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" }
    );

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .Select(x => x.Value.Errors.First().ErrorMessage)
            .FirstOrDefault();

        return new ObjectResult(new
        {
            Success = false,
            Message = firstError ?? "Invalid request data"
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError // ?? force 500
        };
    };
});

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
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // Skip the default response
            context.HandleResponse();

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var message = new { message = "You are not authorized. Please log in." };
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(message));
        },

        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var message = new { message = "Access denied. You do not have permission to perform this action." };
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(message));
        }
    };

});


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ITokenService, TokenService>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AdatHisabAppContext>();
    await DatabaseSeeder.SeedAsync(context);
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseStatusCodePages(async context =>
{
    if (context.HttpContext.Response.StatusCode == StatusCodes.Status405MethodNotAllowed)
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Method not allowed. \"}");
    }
});
app.UseAuthorization();
app.MapGet("/", async (AdatHisabAppContext db) =>
{
    bool dbConnected;

    try
    {
        dbConnected = await db.Database.CanConnectAsync();
    }
    catch
    {
        dbConnected = false;
    }

    return Results.Ok(new
    {
        status = "API is running",
        database = dbConnected ? "Connected" : "Disconnected",
        environment = app.Environment.EnvironmentName,
        serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")

    });
});
app.MapControllers();

app.Run();
