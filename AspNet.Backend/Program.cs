using System.Runtime.CompilerServices;
using System.Text;
using AspNet.Backend.Feature.AppUser;
using AspNet.Backend.Feature.Authentication;
using AspNet.Backend.Feature.Background;
using AspNet.Backend.Feature.Email;
using AspNet.Backend.Feature.Frontend;
using AspNet.Backend.Feature.Character;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using HealthCheckService = AspNet.Backend.Feature.HealthCheck.HealthCheckService;

[assembly: InternalsVisibleTo("AspNet.Backend.Tests")]
namespace AspNet.Backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add asp.net & prometheus services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Map appsettings to setting classes
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));
        
        // Custom services
        builder.Services.AddScoped<EmailTemplateRenderer>();
        builder.Services.AddScoped<EmailSender>();
        builder.Services.AddScoped<AuthenticationService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddScoped<CharacterService>();
        builder.Services.AddScoped<ServerNetworkService>();
        builder.Services.AddHostedService<GameLoopService>();
        
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Backend"))
                    .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter((opt, reader) =>
                    {
                        opt.Endpoint = new Uri("http://otel-collector:4317"); // OTLP-Endpoint Otel-Collector
                        opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc; // Http for 4318
                        reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                    });
                //.AddPrometheusExporter();  // Prometheus 
            }).WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri("http://otel-collector:4317"); // OTLP-Endpoint Otel-Collector
                        opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc; // Http for 4318
                    });
            }).WithLogging(logging =>
                {
                    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Backend"))
                        .AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri("http://otel-collector:4317"); // OTLP-Endpoint Otel-Collector
                            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc; // Http for 4318
                        });
                }
            );
        
        // Health check
        builder.Services.AddHealthChecks().AddCheck<HealthCheckService>(nameof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService));

        // Connect to PostgreSQL
        //builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("teste"));
        
        // Add identity to efcore
        builder.Services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        
        // Adding Authentication & JWT-Bearer
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],  // Access config files and insert value
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
                };
            });

        // Conditional logic for development environment (optional)
        if (builder.Environment.IsDevelopment())
        {
            
            // Disable cors by allowing from all origins
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin() 
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            
            builder.Services.AddSwaggerGen(setup =>
            {
                // Include 'SecurityScheme' to use JWT Authentication
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });
            });
        }

        // Configure the HTTP request pipeline
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Create the database if it doesn't exist
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            dbContext.Database.EnsureDeleted();  // Be careful with EnsureDeleted(), it deletes the database
            dbContext.Database.EnsureCreated();  // Ensures the database is created
            AppDbContext.SeedAsync(userManager, roleManager).Wait();
        }
        
        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");
        
        // Serve static files for frontend (svelte)
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html"); // SPA-Support
        
        // Allow all cors
        app.UseCors("AllowAll");  // Middleware aktivieren
        
        app.Run();
    }
}