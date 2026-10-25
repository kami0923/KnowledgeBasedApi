using KnowledgeBaseAPI.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using KnowledgeBaseAPI.Api;
using KnowledgeBaseAPI.Application.Interfaces;
using KnowledgeBaseAPI.Infrastructure.Data;
using KnowledgeBaseAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
// ...existing usings




var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Knowledge Base API",
        Version = "v1",
        Description = "AI-powered document knowledge base with RAG capabilities"
    });

    options.OperationFilter<SwaggerFileUploadFilter>();   // ← add this line


    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste ONLY the raw JWT token (the value of \"token\" from /api/Auth/login or /api/Auth/register). Swagger UI adds the \"Bearer \" prefix automatically - do NOT type \"Bearer\" yourself."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


//for cors configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization();

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection(GeminiSettings.SectionName));

builder.Services.AddHttpClient<IAiService, GeminiAiService>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<GeminiSettings>>().Value;

    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);

    if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        client.DefaultRequestHeaders.Add("x-goog-api-key", settings.ApiKey);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Knowledge Base API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowViteDev");    

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();