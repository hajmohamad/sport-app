using sport_app_backend.Models.Program;

namespace sport_app_backend.Models.TrainingPlan;

public class TrainingGoalParameter
{
    public double RestBetweenSetsSec { get; set; }
    public double RestBetweenMovesSec { get; set; }
    public double TimePerRepSec { get; set; }
    public double EpocPercentage { get; set; }
}

public static class TrainingGoalParameters
{
    public static readonly Dictionary<ProgramPriority, TrainingGoalParameter> Parameters = new()
    {
        {
            ProgramPriority.INCREASE_VOLUME,
            new TrainingGoalParameter
                { RestBetweenSetsSec = 60, RestBetweenMovesSec = 75, TimePerRepSec = 4, EpocPercentage = 0.07 }
        },
        {
            ProgramPriority.INCREASED_ENDURANCE,
            new TrainingGoalParameter
                { RestBetweenSetsSec = 35, RestBetweenMovesSec = 45, TimePerRepSec = 2, EpocPercentage = 0.04 }
        },
        {
            ProgramPriority.RECOVERY,
            new TrainingGoalParameter
                { RestBetweenSetsSec = 110, RestBetweenMovesSec = 135, TimePerRepSec = 3, EpocPercentage = 0.02 }
        },
        {
            ProgramPriority.LOSS_WEIGHT,
            new TrainingGoalParameter
                { RestBetweenSetsSec = 35, RestBetweenMovesSec = 45, TimePerRepSec = 2.5, EpocPercentage = 0.08 }
        },
        {
            ProgramPriority.INCREASED_AGILITY,
            new TrainingGoalParameter
                { RestBetweenSetsSec = 120, RestBetweenMovesSec = 150, TimePerRepSec = 1.5, EpocPercentage = 0.05 }
        },
        {
            ProgramPriority.INCREASE_STRENGTH,
            new TrainingGoalParameter
                { RestBetweenSetsSec = 145, RestBetweenMovesSec = 180, TimePerRepSec = 3.5, EpocPercentage = 0.06 }
        }
    };

}