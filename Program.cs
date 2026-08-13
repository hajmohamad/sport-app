using AspNetCoreRateLimit;
using DotNetEd.CoreAdmin;
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
using Serilog;
using Serilog.Events;
using sport_app_backend.BackgroundServices;
using sport_app_backend.Handler;
using sport_app_backend.Hubs;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Repository.AthleteRepo;
using sport_app_backend.Repository.CoachRepo;
using sport_app_backend.Services.Cash;
using sport_app_backend.Infrastructure.Cache;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Is(LogEventLevel.Information) 
        .Enrich.FromLogContext()
        .WriteTo.Console(); 
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:21345",
                "https://app.chaarset.ir",
                "https://chaarset.ir",
                "https://charset-i-os-pwa.vercel.app",
                "https://charset-pwa.pages.dev",
                "https://frontstaging.chaarset.ir",
                "https://app.chaarset.ir"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

    });
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); 
builder.Services.AddProblemDetails();



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

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{

   
        var serverVersion = new MySqlServerVersion(new Version(9, 0, 1));

        var databaseSettings = builder.Configuration.GetSection("DatabaseSettings");


        options.UseMySql(connectionString, serverVersion,
                mySqlOptions =>
                {
                    if (databaseSettings.GetValue<bool>("EnableRetryOnFailure"))
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: databaseSettings.GetValue<int>("MaxRetryCount"),
                            maxRetryDelay: TimeSpan.FromSeconds(databaseSettings.GetValue<int>("MaxRetryDelaySeconds")),
                            errorNumbersToAdd: null
                        );
                    }
                })
            .LogTo(Console.WriteLine, LogLevel.Error)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();
    
});


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultForbidScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultSignOutScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["JWT:SigningKey"]
                    ?? throw new InvalidOperationException(
                        "JWT:SigningKey is not configured.")
                )
            ),

            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
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
builder.Services.AddScoped<IStorage, Storage>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<ISmsService, SmsService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICoachRepository, CoachRepository>();
builder.Services.AddScoped<IWebPushNotificationService, WebPushNotificationService>();
builder.Services.AddScoped<INotification,NotificationRepository>();
// builder.Services.AddHostedService<TrainingReminderService>();
builder.Services.AddHostedService<ProgramRenewalReminderService>();
builder.Services.AddHostedService<PaymentAttemptSmsService>();
builder.Services.AddHostedService<QuestionReminderService >();

builder.Services.AddScoped<IAthleteRepository, AthleteRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IBuyFromSiteRepository, BuyFromSiteRepository>();
builder.Services.AddScoped<ICalculator, Calculator>();
builder.Services.AddScoped<IAchievements, AchievementsRepository>();
builder.Services.AddScoped<IWaterAndWeight, WaterAndWeightRepository>();
builder.Services.AddScoped<IActivity, ActivityRepository>();
builder.Services.AddScoped<IInAppMessageRepository, InAppMessageRepository>();
builder.Services.AddScoped<IWorkoutProgramTemplateRepository, WorkoutProgramTemplateRepository>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IChangePhotosRepository, ChangePhotosRepository>();


//cash service
builder.Services.AddScoped<IExerciseCacheService, ExerciseCacheService>();
builder.Services.AddScoped<AthleteCacheService>();
builder.Services.AddScoped<WorkoutProgramCacheService>();
builder.Services.AddScoped<TrainingSessionCacheService>();
builder.Services.Configure<ExerciseCacheOptions>(
    builder.Configuration.GetSection(ExerciseCacheOptions.SectionName));


builder.Services.AddScoped<IDataValidator, DataValidator>();
builder.Services.AddScoped<IEitaaAuthService, EitaaAuthService>();

builder.Services.AddSignalR();

builder.Services.AddScoped<IChatRepository, ChatRepository>();









builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseIpRateLimiting(); 

app.UseCors("AllowFrontend");
app.UseMiddleware<ResponseLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

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


 