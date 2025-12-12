using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LibraryAPI2.Data;
using LibraryAPI2.Services;
using LibraryAPI2.Models;
using Amazon.SecretsManager;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.OpenApi.Models;



var builder = WebApplication.CreateBuilder(args);

// Configure for AWS App Runner
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Add AWS Services
builder.Services.AddAWSService<IAmazonSecretsManager>();

builder.Services.AddControllers();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseNpgsql(connectionString));

// Add Services
// No scoped services to register here; remove invalid AddScoped call.

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("LibrarianOrAdmin", policy => policy.RequireRole("Librarian", "Admin"));
});

// Health Checks
builder.Services.AddHealthChecks().AddNpgSql(connectionString!);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer' followed by your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        context.Database.Migrate();
        await SeedDatabase(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        throw;
    }
}

app.Run();

static async Task SeedDatabase(LibraryContext context, ILogger logger)
{
    if (!await context.Users.AnyAsync())
    {
        logger.LogInformation("Seeding initial data...");
        
        var users = new[]
        {
            new User { Email = "admin@library.com", Password = AuthService.HashPassword("SecureAdmin123!"), Role = "Admin" },
            new User { Email = "librarian@library.com", Password = AuthService.HashPassword("SecureLib123!"), Role = "Librarian" },
            new User { Email = "member@library.com", Password = AuthService.HashPassword("SecureMem123!"), Role = "Member" }
        };

        context.Users.AddRange(users);

        var books = new[]
        {
            new Book { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", IsAvailable = true },
            new Book { Title = "Clean Code", Author = "Robert Martin", IsAvailable = true }
        };

        context.Books.AddRange(books);
        await context.SaveChangesAsync();
    }
}
