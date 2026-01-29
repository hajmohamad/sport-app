# نمودار معماری / Architecture Diagram

## معماری فعلی (Current Architecture)

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                        │
│                         (Controllers)                            │
│  UserController, CoachController, AthleteController, etc.       │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ Direct calls
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                       REPOSITORY LAYER                           │
│                   (Data Access + Business Logic)                 │
│  ❌ UserRepository, CoachRepository, AthleteRepository           │
│  ❌ Contains business logic (should be separated)                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ Entity Framework Core
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                         DATABASE LAYER                           │
│                      ApplicationDbContext                        │
│                    MySQL / SQLite Database                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      EXTERNAL SERVICES                           │
│  (Scattered across Services folder)                             │
│  - SmsService, ZarinPal, LiaraStorage, TokenService            │
└─────────────────────────────────────────────────────────────────┘
```

---

## معماری پیشنهادی Clean Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                       PRESENTATION LAYER                            │
│                          (Controllers)                              │
│   UserController, CoachController, AthleteController               │
└────────────────┬───────────────────────────────────────────────────┘
                 │
                 │ Calls use cases
                 ▼
┌────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                              │
│                      (Use Cases / Handlers)                         │
│  ✅ LoginUseCase                                                    │
│  ✅ CheckCodeUseCase                                                │
│  ✅ EditProfileUseCase                                              │
│  ✅ CreateWorkoutProgramUseCase                                     │
│                                                                     │
│  Contains:                                                          │
│  - Business Logic                                                   │
│  - Validation                                                       │
│  - Orchestration                                                    │
│  - DTOs & Mapping                                                   │
└────────────────┬───────────────────────────────────────────────────┘
                 │
                 │ Uses interfaces
                 ▼
┌────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                                │
│                      (Core Business Logic)                          │
│  ✅ Entities: User, Coach, Athlete, Exercise, WorkoutProgram        │
│  ✅ Value Objects: PhoneNumber, Email, Weight, Height              │
│  ✅ Domain Services: UserAuthenticationService                      │
│  ✅ Repository Interfaces: IUserRepository, ICoachRepository        │
│  ✅ Business Rules & Invariants                                     │
│                                                                     │
│  ⭐ NO DEPENDENCIES - This is the core!                             │
└────────────────┬───────────────────────────────────────────────────┘
                 │
                 │ Implemented by
                 ▼
┌────────────────────────────────────────────────────────────────────┐
│                     INFRASTRUCTURE LAYER                            │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │           PERSISTENCE (Data Access)                          │ │
│  │  ✅ ApplicationDbContext                                      │ │
│  │  ✅ Repository Implementations (UserRepository, etc.)        │ │
│  │  ✅ Migrations                                                │ │
│  │  ✅ EF Core Configurations                                    │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │           EXTERNAL SERVICES                                   │ │
│  │  ✅ SMS Service (SMS.ir)                                      │ │
│  │  ✅ Storage Service (Liara)                                   │ │
│  │  ✅ Payment Service (ZarinPal)                                │ │
│  │  ✅ Token Service (JWT)                                       │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │           IDENTITY & SECURITY                                 │ │
│  │  ✅ Authentication                                            │ │
│  │  ✅ Authorization                                             │ │
│  └──────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
                 │
                 │ Stores in
                 ▼
┌────────────────────────────────────────────────────────────────────┐
│                         DATABASE                                    │
│                   MySQL / SQLite                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## جهت وابستگی‌ها (Dependency Direction)

### معماری فعلی ❌
```
Presentation → Repository → Database
     ↓              ↓
External Services   Infrastructure
```
**مشکل**: همه چیز به هم وابسته است

### Clean Architecture ✅
```
                    ┌─────────────┐
                    │   Domain    │ ← هیچ وابستگی ندارد
                    │  (Entities) │
                    └──────┬──────┘
                           ↑
                    ┌──────┴──────┐
                    │ Application │ ← فقط به Domain وابسته
                    │ (Use Cases) │
                    └──────┬──────┘
                           ↑
        ┌──────────────────┼──────────────────┐
        ↑                  ↑                  ↑
┌───────┴────────┐  ┌──────┴───────┐  ┌──────┴───────┐
│ Presentation   │  │Infrastructure│  │   External   │
│ (Controllers)  │  │(Repositories)│  │   Services   │
└────────────────┘  └──────────────┘  └──────────────┘
```

**قانون طلایی Clean Architecture**:  
> وابستگی‌ها همیشه به سمت داخل (Domain) هستند، هرگز به سمت خارج نیستند.

---

## مثال جریان درخواست (Request Flow Example)

### معماری فعلی ❌

```
1. HTTP Request
        ↓
2. UserController.CheckCode()
        ↓
3. UserRepository.CheckCode()  ← ❌ Business Logic HERE!
   - Validate code
   - Check expiration
   - Create user if needed
   - Generate tokens
        ↓
4. ApplicationDbContext (EF Core)
        ↓
5. Database Query
        ↓
6. Return ApiResponse
```

### Clean Architecture ✅

```
1. HTTP Request
        ↓
2. UserController.CheckCode()
        ↓
3. CheckCodeUseCase.Execute()  ← ✅ Business Logic HERE!
   - Validate input
   - Call domain service
        ↓
4. UserAuthenticationService (Domain)
   - Check code validity
   - Verify expiration
        ↓
5. IUserRepository.GetByPhoneNumber()  ← ✅ Only data access
        ↓
6. UserRepository (Infrastructure)
   - Execute query via EF Core
        ↓
7. Database Query
        ↓
8. Return Result<CheckCodeResponse>
```

---

## مقایسه مسئولیت‌ها (Responsibility Comparison)

### Repository در معماری فعلی ❌
```csharp
public class UserRepository
{
    // ❌ Too many responsibilities!
    - Data access
    - Business validation
    - Token generation
    - Code verification logic
    - User creation logic
    - SMS sending coordination
    - Response formatting
}
```

### Repository در Clean Architecture ✅
```csharp
public class UserRepository : IUserRepository
{
    // ✅ Single responsibility: Data access only
    public Task<User?> GetByIdAsync(int id);
    public Task<User?> GetByPhoneNumberAsync(string phone);
    public Task<User> AddAsync(User user);
    public Task UpdateAsync(User user);
    public Task DeleteAsync(User user);
    public Task<bool> ExistsAsync(string phoneNumber);
}
```

### Use Case در Clean Architecture ✅
```csharp
public class CheckCodeUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICodeVerificationService _codeService;
    private readonly ITokenService _tokenService;
    
    public async Task<Result<CheckCodeResponse>> ExecuteAsync(
        CheckCodeRequest request)
    {
        // ✅ Business logic here
        // ✅ Orchestration
        // ✅ Validation
        // ✅ Error handling
    }
}
```

---

## اصول SOLID در Clean Architecture

### Single Responsibility Principle (SRP) ✅
```
Each class has ONE reason to change:
- Repository: Changes when data access logic changes
- Use Case: Changes when business rules change
- Controller: Changes when API contract changes
```

### Open/Closed Principle (OCP) ✅
```
Open for extension, closed for modification:
- Add new use cases without modifying existing ones
- Add new repositories without changing interfaces
```

### Liskov Substitution Principle (LSP) ✅
```
Interfaces can be replaced:
- IUserRepository can be mocked for testing
- ISmsService can be replaced with different provider
```

### Interface Segregation Principle (ISP) ✅
```
Small, focused interfaces:
- IUserRepository (user operations only)
- ISmsService (SMS operations only)
- Not one big IDataService with everything
```

### Dependency Inversion Principle (DIP) ✅
```
Depend on abstractions, not concretions:
- Use Case depends on IRepository (interface)
- Not on concrete EF Core repository
```

---

## خلاصه تصمیم‌گیری (Decision Summary)

| معیار | وضعیت فعلی | Clean Architecture |
|-------|-----------|-------------------|
| تفکیک لایه‌ها | ⚠️ جزئی | ✅ کامل |
| Business Logic | ❌ در Repository | ✅ در Use Cases |
| وابستگی به Framework | ❌ مستقیم | ✅ جدا شده |
| تست‌پذیری | ⚠️ متوسط | ✅ عالی |
| انعطاف‌پذیری | ⚠️ محدود | ✅ بالا |
| پیچیدگی | ✅ کم | ⚠️ بیشتر |
| مناسب پروژه کوچک | ✅ بله | ⚠️ Over-engineering |
| مناسب پروژه بزرگ | ⚠️ مشکل‌ساز | ✅ بله |

---

**توصیه نهایی**: اگر پروژه در حال رشد است، مهاجرت به Clean Architecture را شروع کنید. اگر کوچک می‌ماند، معماری فعلی کافی است.
