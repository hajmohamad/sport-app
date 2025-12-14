using sport_app_backend.Interface;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Services;

public class Calculator : ICalculator
{
    public double BmrCalculator(BmrRequestDto request)
    {
        double bmr;
        if (request.ActivityLevel == ActivityLevel.None)
        {
            return 0.0;
        }
        


        if (request.Gender == Gender.MALE)
        {
            bmr = 88.362
                  - (13.397 * request.WeightKg)
                  + (4.799 * request.HeightCm)
                  + (5.677 * request.Age);
        }
        else
        {
            bmr = 447.593
                  - (9.247 * request.WeightKg)
                  + (3.098 * request.HeightCm)
                  + (4.330 * request.Age);
        }


        double pal = ((int)request.ActivityLevel) / 10.0;
        double dailyCalories = bmr * pal;


        return dailyCalories;
    }


}