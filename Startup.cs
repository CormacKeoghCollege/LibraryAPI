using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LibraryAPI2.Data;
using LibraryAPI2.Services;
using Microsoft.AspNetCore.Authentication;

namespace LibraryAPI2;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        
           // Add logging first
        services.AddLogging();

        // Check if running on Lambda
        var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
        
    if (isLambda)
    {
        // For Lambda: read from environment variable set by template.yaml
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

 // Create temporary logger for debugging
        using var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<Startup>();
        
        // Log the full connection string (be careful in production!)
        logger.LogInformation("Connection string: '{ConnectionString}'", connectionString);
        logger.LogInformation("Connection string length: {Length}", connectionString?.Length ?? 0);
        
        // Log individual environment variables for debugging
        logger.LogInformation("DATABASE_CONNECTION_STRING env var: '{DbEnvVar}'", Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING"));
        logger.LogInformation("PostgreSQL config value: '{PostgreSqlConfig}'", Configuration.GetConnectionString("PostgreSQL"));
        
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("Connection string is null or empty!");
            throw new InvalidOperationException("Database connection string not found");
        }
        

        
        Console.WriteLine("Lambda - Connection string found: {!string.IsNullOrEmpty(connectionString)}");
        Console.WriteLine("Lambda - Connection string length: {connectionString?.Length}");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("DATABASE_CONNECTION_STRING environment variable not found in Lambda!");
        }
        
        services.AddDbContext<LibraryContext>(options =>
            options.UseNpgsql(connectionString));
    }
    else
    {
        // For local development: read from appsettings.json
        var connectionString = Configuration.GetConnectionString("PostgreSQL");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<LibraryContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else
        {
            // Fallback to in-memory for local development
         //   services.AddDbContext<LibraryContext>(options =>
          //      options.UseInMemoryDatabase("LibraryDB"));
        }
    }


        // Add Authentication Service
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Configure JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtKey = Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured (Jwt:Key).");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        // Add Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("LibrarianOrAdmin", policy => 
                policy.RequireRole("Librarian", "Admin"));
        });

        // Add Swagger (only for local development)
        if (!isLambda)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
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
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

        // Configure pipeline
        if (env.IsDevelopment() && !isLambda)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (!isLambda)
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // Initialize database with sample data (only for non-Lambda)
        if (!isLambda)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
            context.Database.EnsureCreated();
        }
    }
}