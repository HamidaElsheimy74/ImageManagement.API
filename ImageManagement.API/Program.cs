using ImageManagement.API.Helpers;
using ImageManagement.BLL.Helpers.Interfaces;
using ImageManagement.BLL.Helpers.Services;
using ImageManagement.BLL.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.BLL.Services;
using ImageManagement.Infrastructure.Implementations;
using ImageManagement.Infrastructure.Interfaces;
using ImageManagement.Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;

services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

var jwtConfig = builder.Configuration.GetSection("JWT").Get<JWT>();
services.Configure<LoginData>(builder.Configuration.GetSection("LoginData"));


services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers["X-Client-Id"].FirstOrDefault() ?? httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));


    options.AddPolicy("strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));


    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.",
            cancellationToken: cancellationToken);
        return ValueTask.CompletedTask;
    };
});


services.AddAuthorization();
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
        .AddJwtBearer(x =>
        {
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig!.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
                RequireExpirationTime = true,
            };
        });
services.AddSwaggerGen(s =>
{
    s.ResolveConflictingActions(apiDesc => apiDesc.First());
    s.SwaggerDoc("v1", new OpenApiInfo { Title = "Image Management API", Version = "v1" });
    var Scheme = new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme,
        },
    };
    s.AddSecurityDefinition(Scheme.Reference.Id, Scheme);
    s.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
            { Scheme, Array.Empty<string>() },
        });
    s.OperationFilter<AddBearerPrefixFilter>();


});


services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("https://localhost:7012");
    });
});

services.AddScoped<IFileStorageService, FileStorageService>();
services.AddScoped<IImageService, ImageService>();
services.AddScoped<ILoginService, LoginService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IImageProcessingHelpers, ImageProcessingHelpers>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, builder.Configuration["UploadFolderName"]!);
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.Run();


