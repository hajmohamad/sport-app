using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using sport_app_backend.Models.Account;
using sport_app_backend.Models;
using sport_app_backend.Interface;
using sport_app_backend.Services;
using sport_app_backend.Repository;
using Microsoft.OpenApi.Models;
// using Serilog;
// using Serilog.Events;
// using sport_app_backend.Handler;

// Log.Logger = new LoggerConfiguration()
//     .WriteTo.Console()
//     .CreateBootstrapLogger();


var builder = WebApplication.CreateBuilder(args);
// builder.Host.UseSerilog((context, configuration) =>
// {
//     configuration
//         .MinimumLevel.Is(LogEventLevel.Error) 
//         .Enrich.FromLogContext()
//         .WriteTo.Console(); 
// });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:21345", "https://charset-i-os-pwa.vercel.app","https://charset-pwa.pages.dev") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
// builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); 



builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            []
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(9, 0, 1));

var databaseSettings = builder.Configuration.GetSection("DatabaseSettings");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion,
        mySqlOptions => {
            if (databaseSettings.GetValue<bool>("EnableRetryOnFailure"))
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: databaseSettings.GetValue<int>("MaxRetryCount"),
                    maxRetryDelay: TimeSpan.FromSeconds(databaseSettings.GetValue<int>("MaxRetryDelaySeconds")),
                    errorNumbersToAdd: null
                );
            }
        })
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
        )
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Athlete", policy => policy.RequireRole("Athlete"));
    options.AddPolicy("Coach", policy => policy.RequireRole("Coach"));
    options.AddPolicy("None", policy => policy.RequireRole("None"));
});
builder.Services.AddScoped<IZarinPal, ZarinPal>();
builder.Services.AddScoped<ILiaraStorage, LiaraStorage>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<ISmsService, SmsService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICoachRepository, CoachRepository>();
builder.Services.AddScoped<IAthleteRepository, AthleteRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddCoreAdmin();

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


var app = builder.Build();
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();
// app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseIpRateLimiting(); 

app.UseCors("AllowFrontendLocalhost"); // فعال کردن CORS در مسیر مناسب

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}

app.Run();
 