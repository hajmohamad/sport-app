using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using sport_app_backend.Data;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Login_Sinup;
using sport_app_backend.Repository;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.TrainingPlan;
using Xunit.Abstractions;

namespace sport_app_backend.Tests.Repository
{
    public class BuyFromSiteRepositoryTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ISmsService> _mockSmsService;
        private readonly Mock<ILiaraStorage> _mockLiaraStorage;
        private readonly Mock<IZarinPal> _mockZarinPal;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly BuyFromSiteRepository _repository;

        public BuyFromSiteRepositoryTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new ApplicationDbContext(options);

            _mockTokenService = new Mock<ITokenService>();
            _mockSmsService = new Mock<ISmsService>();
            _mockLiaraStorage = new Mock<ILiaraStorage>();
            _mockZarinPal = new Mock<IZarinPal>();
            _mockConfig = new Mock<IConfiguration>();

            _repository = new BuyFromSiteRepository(
                _dbContext,
                _mockTokenService.Object,
                _mockSmsService.Object,
                _mockLiaraStorage.Object,
                _mockZarinPal.Object,
                _mockConfig.Object
            );
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        #region GenerateAccessToken Tests

        [Fact]
        public async Task GenerateAccessToken_ShouldReturnInvalid_WhenTokenNotFound()
        {
            // Act
            var result = await _repository.GenerateAccessToken("invalid_token");

            // Assert
            Assert.False(result.Action);
            Assert.Equal("Invalid refresh token", result.Message);
        }

        [Fact]
        public async Task GenerateAccessToken_ShouldReturnExpired_WhenTokenIsExpired()
        {
            // Arrange
            var user = new User
            {
                SiteRefreshToken = "valid_token",
                LastLoginSite = DateTime.Now.AddDays(-91),
                PhoneNumber = "09123456789"
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GenerateAccessToken("valid_token");

            // Assert
            Assert.False(result.Action);
            Assert.Equal("Refresh token expired", result.Message);
        }

        #endregion

        #region Login & CheckCode Tests

        [Fact]
        public async Task Login_ShouldSendSms_WhenUserIsNew()
        {
            // Arrange
            var phoneNumber = "09123456789";
            _mockSmsService.Setup(s => s.SiteLogin(phoneNumber)).ReturnsAsync("1234");

            // Act
            var result = await _repository.Login(phoneNumber);

            // Assert
            Assert.True(result.Action);
            Assert.Equal("CodeIsSuccessFullySend", result.Message);
            
            var savedCode = await _dbContext.CodeVerifies.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            Assert.NotNull(savedCode);
            Assert.Equal("1234", savedCode.Code);
        }

        [Fact]
        public async Task CheckCode_ShouldReturnInvalid_WhenCodeNotFound()
        {
            // Arrange
            var request = new CheckCodeRequestFromBuyFromSiteDto { PhoneNumber = "09123456789", Code = "1234" };

            // Act
            var result = await _repository.CheckCode(request);

            // Assert
            Assert.False(result.Action);
            Assert.Equal("CodeIsNotCorrect", result.Message);
        }

        #endregion

        #region Discount Code & PreviewCheckout Tests

        [Fact]
        public async Task PreviewCheckout_ShouldReturnError_WhenDiscountCodeDoesNotExist()
        {
           
            
            // Arrange
            var phoneNumber = "09123456789";
            var coachServiceId = 1;
            var expiredCode = new CheckoutDiscountRequestDto
            {
                DiscountCode = "blabla"
            };

            
            await SetupBaseCheckoutData(phoneNumber, coachServiceId, 100000);

            var result = await _repository.PreviewCheckout(phoneNumber, coachServiceId,expiredCode );

            Assert.False(result.Action);
            Assert.Equal("کد تخفیف وارد شده معتبر نیست.", result.Message); // فرض بر پیام خطای فارسی
        }

        [Fact]
        public async Task PreviewCheckout_ShouldReturnError_WhenDiscountCodeIsExpired()
        {
            // Arrange
            var phoneNumber = "09123456789";
            var coachServiceId = 1;
            var expiredCode = new CheckoutDiscountRequestDto
            {
                DiscountCode = "expiredCode"
            };

            await SetupBaseCheckoutData(phoneNumber, coachServiceId, 100000);
            
            var discount = new DiscountCode 
            { 
                Code = "EXPIREDCODE", 
                CoachId =  coachServiceId,
                ExpiresAt =  DateTime.Now.AddDays(-1),
                Status = DiscountCodeStatus.ACTIVE,
                IsDeleted = false
            };
            await _dbContext.DiscountCodes.AddAsync(discount);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.PreviewCheckout(phoneNumber, coachServiceId, expiredCode);

            // Assert
            Assert.False(result.Action);
            Assert.Contains("این کد تخفیف منقضی شده است.", result.Message); 
        }

        [Fact]
        public async Task PreviewCheckout_ShouldReturnError_WhenDiscountCodeUsageLimitReached()
        {
            // Arrange
            var phoneNumber = "09123456789";
            var coachServiceId = 1;
            var limitedCode = "LIMIT10";
            var limitedCodeObject = new CheckoutDiscountRequestDto
            {
                DiscountCode = limitedCode
            };

            await SetupBaseCheckoutData(phoneNumber, coachServiceId, 100000);
            
            var discount = new DiscountCode 
            { 
                Code = limitedCode, 
                CoachId = 1,
                ExpiresAt = DateTime.Now.AddDays(10),
                UsageLimit = 5,
                UsedCount = 5, // ظرفیت تکمیل شده
                Status = DiscountCodeStatus.ACTIVE
            };
            await _dbContext.DiscountCodes.AddAsync(discount);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.PreviewCheckout(phoneNumber, coachServiceId, limitedCodeObject);

            // Assert
            Assert.False(result.Action);
            Assert.Contains("این کد تخفیف منقضی شده است.", result.Message); 
        }

        [Fact]
        public async Task PreviewCheckout_ShouldCalculateCorrectPrice_WhenDiscountCodeIsValid()
        {
            // Arrange
            var phoneNumber = "09123456789";
            var coachServiceId = 1; // سرویسی که می‌خواهیم تخفیف را روی آن اعمال کنیم
            var validCode = "OFF20";
            var code = new CheckoutDiscountRequestDto
            {
                DiscountCode = validCode
            };
            var originalPrice = 100000;

            await SetupBaseCheckoutData(phoneNumber, coachServiceId, originalPrice);
    
            var discount = new DiscountCode 
            { 
                Code = validCode, 
                CoachId = 1,
                ExpiresAt = DateTime.Now.AddDays(10),
                DiscountPercent = 20, 
                UsageLimit = 100,
                UsedCount = 0,
                Status = DiscountCodeStatus.ACTIVE,
                // --- شروع رفع اشکال: اتصال صحیح کد تخفیف به سرویس ---
                DiscountCodeCoachServices = 
                [
                    new DiscountCodeCoachService
                    {
                        // اینجا مشخص می‌کنیم که این کد تخفیف برای سرویس با شناسه 1 معتبر است
                        CoachServiceId = coachServiceId 
                    }
                ]
                // --- پایان رفع اشکال ---
            };
            await _dbContext.DiscountCodes.AddAsync(discount);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.PreviewCheckout(phoneNumber, coachServiceId, code);
    
            // Assert
            Assert.True(result.Action);

            var jsonString = JsonSerializer.Serialize(result.Result);

            using var document = JsonDocument.Parse(jsonString);

            var innerResultJson = document.RootElement.GetProperty("Result").GetRawText(); 

            var checkoutData = JsonSerializer.Deserialize<DiscountPreviewDto>(innerResultJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true
            });
            
            Assert.Equal(80000, checkoutData.FinalPrice); 
            Assert.Equal(100000, checkoutData.OriginalPrice);
            Assert.Equal(20000, checkoutData.CodeDiscountAmount);
        }


        #endregion

        #region Helper Methods for Tests

        private async Task SetupBaseCheckoutData(string phoneNumber, int coachServiceId, int price)
        {
            var athlete = new Athlete { PhoneNumber = phoneNumber, User = new User { PhoneNumber = phoneNumber } };
            var coach  = new Coach{ PhoneNumber =  "09395327229", User = new User { PhoneNumber = "09395327229" } };
            var coachService = new CoachService
            {
                Id = coachServiceId,
                CoachId = 1,
                Price = price,
                IsActive = true,
                IsDeleted = false,
                Title = "dsa",
                Description = "sad"
            };
            await _dbContext.Coaches.AddAsync(coach);
            await _dbContext.Athletes.AddAsync(athlete);
            await _dbContext.CoachServices.AddAsync(coachService);
            await _dbContext.SaveChangesAsync();
        }

        #endregion
    }
}
