using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.C_Question;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Mappers
{
    public static class QuestionMapper
    {
        private static InjuryArea ToInjuryArea(this InjuryAreaDto dto)
        {

            return new InjuryArea
            {
                None = dto.None,
                Skeletal = dto.Skeletal?.Select(Enum.Parse<SkeletalDiseases>).ToList()??[],
                SoftTissueAndLigament = dto.SoftTissueAndLigament?.Select(Enum.Parse<SoftTissueAndLigamentInjuries>).ToList() ??[],
                InternalAndDigestive = dto.InternalAndDigestive?.Select(Enum.Parse<InternalAndDigestiveDiseases>).ToList()??[],
                HormonalAndGlandular = dto.HormonalAndGlandular?.Select(Enum.Parse<HormonalAndGlandularDiseases>).ToList()??[],
                Specific = dto.Specific?.Select(Enum.Parse<SpecificDiseases>).ToList()??[],
                Others = dto.Others??""
            };
        }

        private static InjuryAreaDto ToInjuryAreaDto(this InjuryArea model)
        {
            return new InjuryAreaDto
            {
                None = model.None,
                Skeletal = model.Skeletal?.Select(e => e.ToString()).ToList()??[],
                SoftTissueAndLigament = model.SoftTissueAndLigament?.Select(e => e.ToString()).ToList()??[],
                InternalAndDigestive = model.InternalAndDigestive?.Select(e => e.ToString()).ToList()??[],
                HormonalAndGlandular = model.HormonalAndGlandular?.Select(e => e.ToString()).ToList()??[],
                Specific = model.Specific?.Select(e => e.ToString()).ToList()??[],
                Others = model.Others=""
            };
        }
        public static AthleteQuestion ToAthleteQuestion(this AthleteQuestionDto dto, Athlete athlete)
        {
            return new AthleteQuestion
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                CurrentBodyForm = dto.CurrentBodyForm,
                DaysPerWeekToExercise = dto.DaysPerWeekToExercise,
                FitnessLevel = Enum.Parse<FitnessLevel>(dto.FitnessLevel ?? string.Empty),
                InjuryArea = dto.InjuryArea?.ToInjuryArea(),
                ExerciseGoal = Enum.Parse<ExerciseGoal>(dto.ExerciseGoal ?? string.Empty),
                Weight = dto.CurrentWeight
            };
        }
        public static AthleteQuestionDto ToAthleteQuestionDto(this AthleteQuestion question)
        {
            return new AthleteQuestionDto
            {
                CurrentBodyForm = question.CurrentBodyForm,
                DaysPerWeekToExercise = question.DaysPerWeekToExercise,
                FitnessLevel = question.FitnessLevel.ToString() ?? "",
                InjuryArea = question.InjuryArea?.ToInjuryAreaDto(),
                ExerciseGoal = question.ExerciseGoal.ToString() ?? "",
                CurrentWeight = question.Weight,
                BirthDay = question.Athlete?.User?.BirthDate.ToString("yyyy-MM-dd")
            };
        }

        public static CoachQuestion ToCoachQuestion(this CoachQuestionDto coachQuestionDto,User user)
        {
            return new CoachQuestion()
            {
                UserId = user.Id,
                User = user,
                Disciplines = coachQuestionDto.Disciplines
                    .Select(x => (CoachDispline)Enum.Parse(typeof(CoachDispline), x.ToUpper())).ToList(),
                Motivations = coachQuestionDto.Motivations
                    .Select(x => (CoachingMotivation)Enum.Parse(typeof(CoachingMotivation), x.ToUpper())).ToList(),
                WorkOnlineWithAthletes = coachQuestionDto.WorkOnlineWithAthletes,
                PresentsPracticeProgram = coachQuestionDto.PresentsPracticeProgram.Select(x =>
                    (PresentPracticeProgram)Enum.Parse(typeof(PresentPracticeProgram), x.ToUpper())).ToList(),
                TrackAthlete = Enum.Parse<TrackAthlete>(coachQuestionDto.TrackAthlete),
                ManagingRevenue = coachQuestionDto.ManagingRevenue,
                DifficultTrackAthletes = coachQuestionDto.DifficultTrackAthletes,
                HardCommunicationWithAthletes = coachQuestionDto.HardCommunicationWithAthletes
            };
        }
    }
}
