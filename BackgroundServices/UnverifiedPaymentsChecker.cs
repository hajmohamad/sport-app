// BackgroundServices/UnverifiedPaymentsChecker.cs

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.BackgroundServices;

public class UnverifiedPaymentsChecker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnverifiedPaymentsChecker> _logger;

    public UnverifiedPaymentsChecker(IServiceProvider serviceProvider, ILogger<UnverifiedPaymentsChecker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Unverified Payments Checker Service is starting.");

        // این حلقه تا زمانی که برنامه در حال اجراست، ادامه پیدا می‌کند
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Checking for unverified payments...");
                await CheckPaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking unverified payments.");
            }

            // هر ۵ دقیقه یک بار این فرآیند را تکرار کن
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Unverified Payments Checker Service is stopping.");
    }

private async Task CheckPaymentsAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("Checking for unverified payments...");

    using (var scope = _serviceProvider.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var zarinPal = scope.ServiceProvider.GetRequiredService<IZarinPal>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

        var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
        
        var pendingPayments = await context.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.INPROGRESS && p.PaymentDate < fifteenMinutesAgo)
            .ToListAsync(cancellationToken);

        if (!pendingPayments.Any())
        {
            _logger.LogInformation("No pending payments to check.");
            return;
        }

        _logger.LogInformation("Found {Count} pending payments to verify.", pendingPayments.Count);

        foreach (var payment in pendingPayments)
        {
            try
            {
                var request = new ZarinPalVerifyRequestDto
                {
                    Authority = payment.Authority,
                    Amount = (int)payment.Amount 
                    
                };
                var result = await zarinPal.VerifyPaymentAsync(request);

             
                if (result?.Data is { Code: 100 } or { Code: 101 })
                {
                    payment.PaymentStatus = PaymentStatus.SUCCESS;
                    payment.RefId = result.Data.Ref_id;
                    _logger.LogInformation("Payment {PaymentId} was successful/verified. RefId: {RefId}", payment.Id, result.Data.Ref_id);

                    var notificationData = await context.Payments
                        .Where(p => p.Id == payment.Id)
                        .Select(p => new 
                        {
                            AthleteName = p.Athlete.User.FirstName,
                            AthletePhone = p.Athlete.PhoneNumber,
                            CoachName = p.Coach.User.FirstName,
                            CoachPhone = p.Coach.PhoneNumber,
                            ServiceName = p.CoachService.Title
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (notificationData != null)
                    {
                        await smsService.AthleteSuccessfullySmsNotification(
                            notificationData.AthletePhone,
                            notificationData.ServiceName,
                            notificationData.AthletePhone);

                   
                        await smsService.CoachServiceBuySmsNotification(notificationData.CoachPhone,
                         notificationData.CoachName,notificationData.ServiceName,
                            payment.Amount.ToString(CultureInfo.CurrentCulture));
                    }
                }
                else
                {
                    payment.PaymentStatus = PaymentStatus.FAILED;
                    var errors = result?.GetErrors();
                    var errorMessage = errors != null && errors.Any() ? errors.First().Message : "Unknown error from ZarinPal";
                    _logger.LogWarning("Payment {PaymentId} failed verification. Reason: {Error}", payment.Id, errorMessage);
                    break;
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment with ID {PaymentId}.", payment.Id);
                payment.PaymentStatus = PaymentStatus.FAILED;
            }
        }
        
        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Finished checking unverified payments.");
    }
}}