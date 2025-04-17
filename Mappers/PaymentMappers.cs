using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Mappers;

public static class PaymentMappers
{
    public static AllPaymentResponseDto ToAllPaymentResponseDto(this Payment payment)
    {
        return new AllPaymentResponseDto
        {
            PaymentId = payment.Id,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Athlete?.User?.FirstName + " " + payment.Athlete?.User?.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString(CultureInfo.CurrentCulture),
            CoachServiceTitle = payment.CoachService!.Title,
            WorkoutProgramStatus = payment.WorkoutProgram!.Status.ToString()
        };
    }


    public static PaymentResponseDto ToPaymentResponseDto(this Payment payment)
    {
        return new PaymentResponseDto
        {
            TransactionId = payment.TransactionId,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Athlete?.User?.FirstName + " " + payment.Athlete?.User?.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString(CultureInfo.CurrentCulture),
            AthleteQuestion = payment.AthleteQuestion!.ToAthleteQuestionDto(),
            Height = payment.Athlete!.Height,
            WorkoutProgram = payment.WorkoutProgram?.ToProgramResponseDto()??new WorkoutProgramResponseDto()
        };
        
    }
    
}