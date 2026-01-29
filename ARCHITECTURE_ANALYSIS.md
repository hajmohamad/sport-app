# تحلیل معماری پروژه Sport App
# Architecture Analysis for Sport App Project

## خلاصه (Summary)
این پروژه یک بک‌اند ASP.NET Core است که برای یک اپلیکیشن ورزشی طراحی شده است. پس از بررسی دقیق ساختار پروژه، می‌توان گفت که این پروژه **بخشی از اصول معماری Clean Architecture را رعایت می‌کند، اما به طور کامل Clean نیست**.

This project is an ASP.NET Core backend for a sports application. After thorough examination, the architecture **partially follows Clean Architecture principles, but is not completely clean**.

---

## 1️⃣ نقاط قوت معماری (Architecture Strengths)

### ✅ جدایی لایه‌ها (Layer Separation)
پروژه دارای تفکیک مناسبی از لایه‌ها است:
- **Controllers**: لایه ارائه (Presentation Layer)
- **Services**: سرویس‌های خارجی مانند SMS، Storage، Payment
- **Repository**: لایه دسترسی به داده (Data Access Layer)
- **Interface**: تعریف قراردادها (Contracts)
- **Models**: مدل‌های دامنه (Domain Models)
- **Dtos**: اشیاء انتقال داده (Data Transfer Objects)

### ✅ استفاده از الگوی Repository Pattern
```csharp
IUserRepository → UserRepository
ICoachRepository → CoachRepository
IAthleteRepository → AthleteRepository
```
این الگو به خوبی پیاده‌سازی شده و وابستگی به Entity Framework را انتزاع کرده است.

### ✅ استفاده از Dependency Injection
در `Program.cs` به درستی از DI استفاده شده:
```csharp
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<ISmsService, SmsService>();
```

### ✅ تفکیک Interfaces از Implementation
همه سرویس‌ها و Repository ها دارای Interface مجزا هستند که تست‌پذیری را افزایش می‌دهد.

### ✅ استفاده از DTOs
پروژه از DTOs برای انتقال داده استفاده می‌کند و مدل‌های دامنه را مستقیماً expose نمی‌کند.

---

## 2️⃣ نقاط ضعف و انحراف از Clean Architecture

### ❌ عدم وجود لایه Use Case / Application Layer
در Clean Architecture باید یک لایه میانی بین Controllers و Repository وجود داشته باشد:
```
Controller → Use Case/Service Layer → Repository → Database
```
در حال حاضر:
```
Controller → Repository → Database
```
این باعث می‌شود که **Business Logic مستقیماً در Repository** قرار گیرد.

### ❌ Business Logic در Repository
مثال از `UserRepository.cs`:
```csharp
public async Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto)
{
    // Business logic for code verification
    if (user.TimeCodeSend.AddMinutes(15) < DateTime.Now)
    {
        // Business logic here
    }
    // More business logic...
}
```
**Repository باید فقط دسترسی به داده را مدیریت کند، نه منطق کسب‌وکار.**

### ❌ Repository ها به Entity Framework وابسته هستند
Repository ها مستقیماً از `ApplicationDbContext` و `DbSet` استفاده می‌کنند:
```csharp
public class UserRepository(ApplicationDbContext dbContext, ...)
{
    var user = await dbContext.Users.FirstOrDefaultAsync(...);
}
```
در Clean Architecture، Repository باید یک لایه انتزاعی باشد که هیچ وابستگی به فریمورک خاصی نداشته باشد.

### ❌ عدم وجود Domain Services
منطق‌های پیچیده کسب‌وکار باید در Domain Services قرار گیرند، نه در Repository.

### ❌ Controllers به DTO های خروجی وابسته هستند
Controllers مستقیماً با Repository ارتباط دارند و Response ها را خودشان می‌سازند:
```csharp
public async Task<IActionResult> CheckCode([FromBody] CheckCodeRequestDto dto)
{
    var result = await userRepository.CheckCode(dto);
    if (!result.Action) return BadRequest(result);
    return Ok(result);
}
```

### ❌ استفاده از Generic ApiResponse
همه‌جا از یک `ApiResponse` عمومی استفاده شده:
```csharp
public class ApiResponse
{
    public bool Action { get; set; }
    public string Message { get; set; }
    public object? Result { get; set; }
}
```
این باعث از دست رفتن Type Safety می‌شود.

### ❌ عدم جداسازی Infrastructure
سرویس‌هایی مانند `LiaraStorage`، `ZarinPal`، و `SmsService` باید در یک لایه Infrastructure جداگانه باشند.

### ❌ Mapper ها به درستی استفاده نشده‌اند
پروژه دارای پوشه `Mappers` است اما در بسیاری از موارد mapping به صورت دستی انجام می‌شود.

---

## 3️⃣ ساختار پیشنهادی Clean Architecture

```
sport-app-backend/
│
├── Domain/                          # قلب برنامه - هیچ وابستگی ندارد
│   ├── Entities/                    # Models (User, Coach, Athlete, ...)
│   ├── ValueObjects/                # اشیاء ارزشی
│   ├── Enums/                       # Enums
│   ├── Interfaces/                  # Interfaces for repositories
│   └── Exceptions/                  # Domain-specific exceptions
│
├── Application/                     # Use Cases / Business Logic
│   ├── Interfaces/                  # Service interfaces
│   ├── UseCases/                    # Use case implementations
│   │   ├── User/
│   │   │   ├── Login/
│   │   │   ├── CheckCode/
│   │   │   └── EditProfile/
│   │   ├── Coach/
│   │   └── Athlete/
│   ├── DTOs/                        # Data Transfer Objects
│   ├── Mappers/                     # Object mapping
│   └── Common/                      # Shared application logic
│
├── Infrastructure/                  # External concerns
│   ├── Persistence/                 # EF Core, DbContext
│   │   ├── ApplicationDbContext.cs
│   │   ├── Repositories/            # Repository implementations
│   │   └── Migrations/
│   ├── ExternalServices/            # Third-party services
│   │   ├── SMS/
│   │   ├── Storage/
│   │   └── Payment/
│   └── Identity/                    # Authentication/Authorization
│
└── Presentation/                    # Web API / Controllers
    ├── Controllers/
    ├── Filters/
    ├── Middleware/
    └── Program.cs
```

---

## 4️⃣ راهکارهای بهبود (Improvement Recommendations)

### پیشنهاد 1: ایجاد لایه Application/Use Cases
```csharp
// Application/UseCases/User/CheckCode/CheckCodeUseCase.cs
public class CheckCodeUseCase : ICheckCodeUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    
    public async Task<CheckCodeResult> ExecuteAsync(CheckCodeRequest request)
    {
        // Business logic here
    }
}
```

### پیشنهاد 2: ساده‌سازی Repository
```csharp
// Domain/Interfaces/IUserRepository.cs
public interface IUserRepository
{
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
```

### پیشنهاد 3: استفاده از Result Pattern به جای ApiResponse
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
}
```

### پیشنهاد 4: ایجاد Domain Services
```csharp
// Domain/Services/UserAuthenticationService.cs
public class UserAuthenticationService
{
    public bool ValidateVerificationCode(CodeVerify codeVerify, string code)
    {
        // Domain logic
    }
}
```

### پیشنهاد 5: جداسازی Infrastructure
```csharp
// Infrastructure/ExternalServices/SMS/SmsService.cs
// Infrastructure/ExternalServices/Storage/LiaraStorageService.cs
// Infrastructure/ExternalServices/Payment/ZarinPalService.cs
```

---

## 5️⃣ نتیجه‌گیری (Conclusion)

### امتیاز کلی: 6/10 ⭐⭐⭐⭐⭐⭐

#### چه چیزی خوب است؟ ✅
- استفاده از Repository Pattern
- Dependency Injection
- تفکیک Interface از Implementation  
- استفاده از DTOs

#### چه چیزی باید بهبود یابد? ⚠️
- عدم وجود لایه Application/Use Case
- Business Logic در Repository
- عدم جداسازی Infrastructure
- وابستگی مستقیم به Entity Framework

### آیا این معماری Clean است؟
**پاسخ**: خیر، این معماری **Clean نیست** اما **بهتر از یک معماری Monolithic معمولی** است.

این پروژه بیشتر شبیه به **Layered Architecture (معماری لایه‌ای)** است تا Clean Architecture.

### توصیه نهایی
اگر پروژه در حال رشد است و قرار است پیچیده‌تر شود، توصیه می‌شود:
1. لایه Application/Use Case را اضافه کنید
2. Business Logic را از Repository ها خارج کنید  
3. Infrastructure را جدا کنید
4. از CQRS Pattern برای queries پیچیده استفاده کنید

اگر پروژه کوچک می‌ماند، معماری فعلی **قابل قبول** است و نیازی به تغییرات بزرگ نیست.

---

## 6️⃣ منابع مفید (Useful Resources)

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Clean Architecture in .NET](https://jasontaylor.dev/clean-architecture-getting-started/)
- [Repository Pattern Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core)

---

**تاریخ تحلیل**: 2026-01-29  
**نسخه**: 1.0
