# نقشه راه بهبود معماری
# Architecture Improvement Roadmap

این سند یک راهنمای عملی برای تبدیل تدریجی معماری فعلی به Clean Architecture است.

---

## 📋 فهرست مطالب

1. [مراحل پیشنهادی](#مراحل-پیشنهادی)
2. [مرحله 1: ایجاد لایه Application](#مرحله-1-ایجاد-لایه-application)
3. [مرحله 2: جداسازی Business Logic](#مرحله-2-جداسازی-business-logic)
4. [مرحله 3: بهبود Repository Pattern](#مرحله-3-بهبود-repository-pattern)
5. [مرحله 4: جداسازی Infrastructure](#مرحله-4-جداسازی-infrastructure)
6. [مرحله 5: بهبود Error Handling](#مرحله-5-بهبود-error-handling)
7. [مثال‌های عملی](#مثالهای-عملی)

---

## مراحل پیشنهادی

### مرحله‌بندی براساس اولویت:

```
┌─────────────────────────────────────────────────────────────┐
│ Priority 1: ایجاد لایه Application (Use Cases)              │ ← شروع از اینجا
├─────────────────────────────────────────────────────────────┤
│ Priority 2: انتقال Business Logic از Repository             │
├─────────────────────────────────────────────────────────────┤
│ Priority 3: بهبود Repository Pattern (فقط Data Access)      │
├─────────────────────────────────────────────────────────────┤
│ Priority 4: جداسازی Infrastructure                          │
├─────────────────────────────────────────────────────────────┤
│ Priority 5: بهبود Error Handling و Result Pattern           │
└─────────────────────────────────────────────────────────────┘
```

**⏱️ زمان تخمینی**: 4-6 هفته (بسته به اندازه تیم)

---

## مرحله 1: ایجاد لایه Application

### هدف
ایجاد یک لایه میانی بین Controllers و Repository برای مدیریت Business Logic.

### گام‌های اجرایی

#### 1.1. ایجاد ساختار پوشه‌ها

```bash
mkdir -p Application/UseCases/User/Login
mkdir -p Application/UseCases/User/CheckCode
mkdir -p Application/UseCases/User/EditProfile
mkdir -p Application/UseCases/Coach
mkdir -p Application/UseCases/Athlete
mkdir -p Application/Common
mkdir -p Application/DTOs
```

#### 1.2. ایجاد Base Classes

**Application/Common/IUseCase.cs**
```csharp
namespace sport_app_backend.Application.Common;

public interface IUseCase<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request);
}
```

**Application/Common/Result.cs**
```csharp
namespace sport_app_backend.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Errors = new List<string>();
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(List<string> errors)
    {
        var result = new Result<T>(false, default, null);
        result.Errors.AddRange(errors);
        return result;
    }
}
```

#### 1.3. مثال Use Case: CheckCode

**Application/UseCases/User/CheckCode/CheckCodeRequest.cs**
```csharp
namespace sport_app_backend.Application.UseCases.User.CheckCode;

public record CheckCodeRequest(string PhoneNumber, string Code);
```

**Application/UseCases/User/CheckCode/CheckCodeResponse.cs**
```csharp
namespace sport_app_backend.Application.UseCases.User.CheckCode;

public record CheckCodeResponse(
    string RefreshToken,
    string AccessToken,
    string TypeOfUser,
    string Gender,
    bool Questions
);
```

**Application/UseCases/User/CheckCode/CheckCodeUseCase.cs**
```csharp
using sport_app_backend.Application.Common;
using sport_app_backend.Interface;

namespace sport_app_backend.Application.UseCases.User.CheckCode;

public class CheckCodeUseCase : IUseCase<CheckCodeRequest, Result<CheckCodeResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICodeVerificationRepository _codeVerificationRepository;
    private readonly ITokenService _tokenService;

    public CheckCodeUseCase(
        IUserRepository userRepository,
        ICodeVerificationRepository codeVerificationRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _codeVerificationRepository = codeVerificationRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<CheckCodeResponse>> ExecuteAsync(CheckCodeRequest request)
    {
        // 1. Get verification code
        var codeVerify = await _codeVerificationRepository
            .GetByPhoneNumberAsync(request.PhoneNumber);
            
        if (codeVerify == null)
        {
            return Result<CheckCodeResponse>.Failure("کد تایید یافت نشد");
        }

        // 2. Check expiration
        if (codeVerify.TimeCodeSend.AddMinutes(15) < DateTime.Now)
        {
            await _codeVerificationRepository.DeleteAsync(codeVerify);
            return Result<CheckCodeResponse>.Failure("کد تایید منقضی شده است");
        }

        // 3. Verify code
        if (codeVerify.Code != request.Code)
        {
            return Result<CheckCodeResponse>.Failure("کد تایید صحیح نیست");
        }

        // 4. Remove verification code
        await _codeVerificationRepository.DeleteAsync(codeVerify);

        // 5. Get or create user
        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
        
        bool hasCompletedProfile = false;
        
        if (user == null)
        {
            user = await CreateNewUser(request.PhoneNumber);
        }
        else
        {
            hasCompletedProfile = !string.IsNullOrEmpty(user.FirstName);
            user.LastLogin = DateTime.Now;
            await _userRepository.UpdateAsync(user);
        }

        // 6. Generate tokens
        var refreshToken = await _tokenService.CreateRefreshToken(user);
        var accessToken = _tokenService.CreateToken(user);

        // 7. Return response
        var response = new CheckCodeResponse(
            RefreshToken: refreshToken,
            AccessToken: accessToken,
            TypeOfUser: user.TypeOfUser.ToString(),
            Gender: user.Gender.ToString(),
            Questions: hasCompletedProfile
        );

        return Result<CheckCodeResponse>.Success(response);
    }

    private async Task<Models.Account.User> CreateNewUser(string phoneNumber)
    {
        var username = await GenerateUniqueUsername();
        
        var newUser = new Models.Account.User
        {
            UserName = username,
            PhoneNumber = phoneNumber,
            TypeOfUser = TypeOfUser.NONE,
            LastLogin = DateTime.Now
        };

        await _userRepository.AddAsync(newUser);
        return newUser;
    }

    private async Task<string> GenerateUniqueUsername()
    {
        string username;
        do
        {
            username = Guid.NewGuid().ToString("N").Substring(0, 8);
        } while (await _userRepository.UsernameExistsAsync(username));
        
        return username;
    }
}
```

#### 1.4. بروزرسانی Controller

**Controller/UserController.cs** (بخش CheckCode)
```csharp
[HttpPost("CheckCode")]
public async Task<IActionResult> CheckCode(
    [FromBody] CheckCodeRequestDto dto,
    [FromServices] CheckCodeUseCase useCase)
{
    var request = new CheckCodeRequest(dto.PhoneNumber, dto.Code);
    var result = await useCase.ExecuteAsync(request);

    if (!result.IsSuccess)
    {
        return BadRequest(new { 
            Action = false, 
            Message = result.ErrorMessage 
        });
    }

    return Ok(new { 
        Action = true, 
        Message = "کد تایید صحیح است",
        Result = result.Value 
    });
}
```

#### 1.5. ثبت در DI Container

**Program.cs**
```csharp
// Use Cases
builder.Services.AddScoped<CheckCodeUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<EditProfileUseCase>();
// ... سایر use cases
```

### ✅ مزایای این مرحله
- جداسازی Business Logic از Controller و Repository
- تست‌پذیری بالاتر (می‌توانید Use Case را جداگانه تست کنید)
- کد تمیزتر و قابل فهم‌تر
- امکان استفاده مجدد از Business Logic

---

## مرحله 2: جداسازی Business Logic

### هدف
انتقال تمام Business Logic از Repository به Use Cases یا Domain Services.

### قوانین طلایی:

```
✅ Repository می‌تواند:
   - Query data (SELECT)
   - Insert data (INSERT)
   - Update data (UPDATE)
   - Delete data (DELETE)
   - Check existence
   - Count records

❌ Repository نباید:
   - Validate business rules
   - Calculate derived data
   - Make business decisions
   - Call external services
   - Format responses
   - Handle complex logic
```

### مثال: انتقال Validation Logic

**قبل (در Repository)** ❌
```csharp
public async Task<ApiResponse> AddRoleGender(string phoneNumber, RoleGenderDto dto)
{
    var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
    
    if (user is null) 
        return new ApiResponse() { Message = "User not found", Action = false };
    
    if(dto.Role is null) 
        return new ApiResponse() { Message = "Role is null", Action = false };
    
    if(dto.Gender is null) 
        return new ApiResponse() { Message = "Gender is null", Action = false };
    
    // ... more logic
}
```

**بعد (در Use Case)** ✅
```csharp
public async Task<Result<AddRoleResponse>> ExecuteAsync(AddRoleRequest request)
{
    // 1. Validation
    if (string.IsNullOrEmpty(request.Role))
        return Result<AddRoleResponse>.Failure("نقش نمی‌تواند خالی باشد");
    
    if (string.IsNullOrEmpty(request.Gender))
        return Result<AddRoleResponse>.Failure("جنسیت نمی‌تواند خالی باشد");
    
    // 2. Get user (repository only does data access)
    var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
    
    if (user == null)
        return Result<AddRoleResponse>.Failure("کاربر یافت نشد");
    
    // 3. Business logic
    user.Gender = Enum.Parse<Gender>(request.Gender.ToUpper());
    
    // ... rest of logic
}
```

---

## مرحله 3: بهبود Repository Pattern

### هدف
ساده‌سازی Repository ها و حذف Business Logic از آنها.

### قبل ❌

**Interface/IUserRepository.cs**
```csharp
public interface IUserRepository
{
    // ❌ Too specific, coupled to business logic
    Task<ApiResponse> Login(string UserPhoneNumber);
    Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto);
    Task<ApiResponse> AddRoleGender(string phoneNumber, RoleGenderDto roleGenderDto);
    Task<ApiResponse> EditUserProfile(string phoneNumber, EditUserProfileDto editUserProfileDto);
    // ...
}
```

### بعد ✅

**Domain/Interfaces/IUserRepository.cs**
```csharp
namespace sport_app_backend.Domain.Interfaces;

public interface IUserRepository
{
    // ✅ Simple, focused on data operations
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> GetAllAsync();
    
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    
    Task<bool> ExistsAsync(int id);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);
    Task<bool> UsernameExistsAsync(string username);
}
```

**Infrastructure/Persistence/Repositories/UserRepository.cs**
```csharp
namespace sport_app_backend.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Coach)
            .Include(u => u.Athlete)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users
            .Include(u => u.Coach)
            .Include(u => u.Athlete)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.UserName == username);
    }
}
```

---

## مرحله 4: جداسازی Infrastructure

### هدف
جداسازی سرویس‌های خارجی و persistence به یک لایه Infrastructure مجزا.

### ساختار پوشه‌ها

```
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── CoachRepository.cs
│   │   └── AthleteRepository.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   └── ...
│   └── Migrations/
│
├── ExternalServices/
│   ├── SMS/
│   │   ├── ISmsService.cs
│   │   └── SmsService.cs
│   ├── Storage/
│   │   ├── IStorageService.cs
│   │   └── LiaraStorageService.cs
│   └── Payment/
│       ├── IPaymentService.cs
│       └── ZarinPalService.cs
│
└── Identity/
    ├── TokenService.cs
    └── JwtSettings.cs
```

### مثال: SMS Service

**Infrastructure/ExternalServices/SMS/ISmsService.cs**
```csharp
namespace sport_app_backend.Infrastructure.ExternalServices.SMS;

public interface ISmsService
{
    Task<string> SendVerificationCodeAsync(string phoneNumber);
    Task<bool> SendNotificationAsync(string phoneNumber, string message);
}
```

**Infrastructure/ExternalServices/SMS/SmsService.cs**
```csharp
namespace sport_app_backend.Infrastructure.ExternalServices.SMS;

public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        HttpClient httpClient, 
        IConfiguration config,
        ILogger<SmsService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string> SendVerificationCodeAsync(string phoneNumber)
    {
        try
        {
            var code = GenerateRandomCode();
            var apiKey = _config["SMS:ApiKey"];
            var templateId = _config["SMS:VerificationTemplateId"];
            
            // Send via SMS.ir API
            var response = await _httpClient.PostAsync(/* ... */);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Verification code sent to {PhoneNumber}", 
                    phoneNumber);
                return code;
            }
            
            throw new Exception("Failed to send SMS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    private string GenerateRandomCode()
    {
        var random = new Random();
        return random.Next(10000, 99999).ToString();
    }
}
```

---

## مرحله 5: بهبود Error Handling

### استفاده از Result Pattern

**Application/Common/Result.cs**
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => 
        new Result<T>(true, value, null);

    public static Result<T> Failure(Error error) => 
        new Result<T>(false, default, error);
}

public record Error(string Code, string Message);
```

### استفاده از Custom Exceptions

**Domain/Exceptions/DomainException.cs**
```csharp
namespace sport_app_backend.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string phoneNumber) 
        : base($"کاربر با شماره {phoneNumber} یافت نشد") { }
}

public class InvalidVerificationCodeException : DomainException
{
    public InvalidVerificationCodeException() 
        : base("کد تایید صحیح نیست") { }
}
```

---

## مثال‌های عملی

### مثال کامل: Login Flow

#### 1. Request & Response DTOs
```csharp
public record LoginRequest(string PhoneNumber);
public record LoginResponse(bool CodeSent, string Message);
```

#### 2. Use Case
```csharp
public class LoginUseCase : IUseCase<LoginRequest, Result<LoginResponse>>
{
    private readonly ICodeVerificationRepository _codeRepo;
    private readonly ISmsService _smsService;

    public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request)
    {
        // 1. Check if code was sent recently
        var existingCode = await _codeRepo.GetByPhoneNumberAsync(request.PhoneNumber);
        
        if (existingCode != null && 
            existingCode.TimeCodeSend.AddMinutes(2) >= DateTime.Now)
        {
            return Result<LoginResponse>.Failure(
                new Error("rate_limit", "لطفا 2 دقیقه صبر کنید"));
        }

        // 2. Delete old code if exists
        if (existingCode != null)
        {
            await _codeRepo.DeleteAsync(existingCode);
        }

        // 3. Generate and send new code
        var code = await _smsService.SendVerificationCodeAsync(request.PhoneNumber);

        // 4. Save verification code
        var codeVerify = new CodeVerify
        {
            PhoneNumber = request.PhoneNumber,
            Code = code,
            TimeCodeSend = DateTime.Now
        };
        
        await _codeRepo.AddAsync(codeVerify);

        // 5. Return success
        return Result<LoginResponse>.Success(
            new LoginResponse(true, "کد تایید ارسال شد"));
    }
}
```

#### 3. Controller
```csharp
[HttpPost("SendCode")]
public async Task<IActionResult> SendCode(
    [FromBody] string phoneNumber,
    [FromServices] LoginUseCase useCase)
{
    var request = new LoginRequest(phoneNumber);
    var result = await useCase.ExecuteAsync(request);

    if (!result.IsSuccess)
    {
        return BadRequest(new { 
            Action = false, 
            Message = result.Error.Message,
            Code = result.Error.Code
        });
    }

    return Ok(new { 
        Action = true, 
        Message = result.Value.Message 
    });
}
```

---

## چک‌لیست اجرا

### Phase 1: Foundation (هفته 1-2)
- [ ] ایجاد پوشه Application/
- [ ] ایجاد پوشه Domain/
- [ ] تعریف IUseCase<TRequest, TResponse>
- [ ] تعریف Result<T> class
- [ ] تعریف Error classes
- [ ] ایجاد اولین Use Case (مثلاً CheckCode)
- [ ] تست Use Case
- [ ] بروزرسانی Controller

### Phase 2: Migration (هفته 3-4)
- [ ] شناسایی تمام Business Logic در Repository ها
- [ ] انتقال Logic به Use Cases
- [ ] ساده‌سازی Repository ها
- [ ] تست هر بخش پس از تغییر
- [ ] حذف کدهای قدیمی

### Phase 3: Infrastructure (هفته 5-6)
- [ ] ایجاد پوشه Infrastructure/
- [ ] جداسازی سرویس‌های خارجی
- [ ] جداسازی Persistence
- [ ] بروزرسانی DI registrations
- [ ] تست Integration

### Phase 4: Finalization
- [ ] بروزرسانی مستندات
- [ ] Code Review
- [ ] Performance Testing
- [ ] Deploy به Production

---

## نکات مهم ⚠️

1. **تدریجی پیش بروید**: همه چیز را یکجا تغییر ندهید
2. **تست بنویسید**: قبل از تغییر، تست بنویسید
3. **مستند کنید**: تغییرات را document کنید
4. **Code Review**: هر مرحله را review کنید
5. **Performance را چک کنید**: مطمئن شوید performance کاهش نیافته

---

## منابع یادگیری

- Clean Architecture by Robert C. Martin
- [Clean Architecture در .NET](https://github.com/jasontaylordev/CleanArchitecture)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Result Pattern in C#](https://enterprisecraftsmanship.com/posts/error-handling-exception-or-result/)

---

**نویسنده**: AI Architecture Analyst  
**تاریخ**: 2026-01-29  
**نسخه**: 1.0
