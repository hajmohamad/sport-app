using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.Mappers;

public static class PaymentMappers
{
    public static AllPaymentResponseDto ToAllPaymentResponseDto(this Payment payment)
    {
        return new AllPaymentResponseDto()
        {   PaymentId = payment.Id,
            TransactionId = payment.TransitionId,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Athlete?.User?.FirstName + " " + payment.Athlete?.User?.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString(CultureInfo.CurrentCulture)
        };
    }


    public static PaymentResponseDto ToPaymentResponseDto(this Payment payment)
    {
        return new PaymentResponseDto
        {
            TransactionId = payment.TransitionId,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Athlete?.User?.FirstName + " " + payment.Athlete?.User?.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString(CultureInfo.CurrentCulture),
            AthleteQuestion = payment.AthleteQuestion!.ToAthleteQuestionDto(),
            Weight = payment.Athlete!.CurrentWeight,
            Height = payment.Athlete.Height,
            
            
        };
        
    }
    
}