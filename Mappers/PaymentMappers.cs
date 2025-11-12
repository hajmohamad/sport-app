using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Mappers;

public static class PaymentMappers
{
    public static AllPaymentResponseDto ToCoachAllPaymentResponseDto(this Payment payment)
    {
        return new AllPaymentResponseDto
        {
            PaymentId = payment.Id,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Athlete?.User?.FirstName + " " + payment.Athlete?.User?.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString("yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            CoachServiceTitle = payment.CoachService.Title,
            WorkoutProgramStatus = payment.WorkoutProgram!.Status.ToString(),
            ImageProfile = payment.Athlete?.User?.ImageProfile ??""

        };
    }
    
    public static PaymentResponseDto ToCoachPaymentResponseDto(this Payment payment,string wpkey)
    {
        return new PaymentResponseDto
        {   PaymentId = payment.Id,
            TransactionId = payment.Authority,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Athlete.User?.FirstName + " " + payment.Athlete?.User?.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            AthleteQuestion = payment.AthleteQuestion?.AthleteQuestionResponseDto(),
            Height = payment.Athlete!.Height,
            ImageProfile = payment.Athlete.User?.ImageProfile ??"",
            WorkoutProgram = payment.WorkoutProgram?.ToProgramResponseDto()??new WorkoutProgramResponseDto(),
            Gender = payment.Athlete.User?.Gender.ToString(),
            BirthDate = payment.Athlete.User?.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            PdfLink =   $"chaarset.ir/program/{wpkey}",
            WpKey = wpkey
        };
        
    }
    public static PaymentResponseDto ToAthletePaymentResponseDto(this Payment payment)
    {
        return new PaymentResponseDto
        {   PaymentId = payment.Id,
            TransactionId = payment.Authority,
            PaymentStatus = payment.PaymentStatus.ToString(),
            Name = payment.Coach.User.FirstName + " " + payment.Coach.User.LastName,
            Amount = payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = payment.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            AthleteQuestion = payment.AthleteQuestion?.AthleteQuestionResponseDto(),
            Height = payment.Athlete.Height,
            ImageProfile = payment.Athlete.User?.ImageProfile ??"",
            WorkoutProgram = payment.WorkoutProgram?.ToProgramResponseDto()??new WorkoutProgramResponseDto(),
            Gender =   payment.Athlete.User?.Gender.ToString() ??"Female",
            BirthDate = payment.Athlete.User?.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)??"2001-01-01",
            WpKey = "chaarset.ir"


        };
        
    }
    
}