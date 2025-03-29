using sport_app_backend.Dtos;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Mappers
{
    public static class InjuryAreaMapper
    {
        public static InjuryArea ToInjuryArea(this InjuryAreaDto dto)
        {
            if (dto == null) return null;

            return new InjuryArea
            {
                none = dto.None,
                skeletal = dto.Skeletal?.Select(s => Enum.Parse<SkeletalDiseases>(s)).ToList(),
                softTissueAndLigament = dto.SoftTissueAndLigament?.Select(s => Enum.Parse<SoftTissueAndLigamentInjuries>(s)).ToList(),
                internalAndDigestive = dto.InternalAndDigestive?.Select(s => Enum.Parse<InternalAndDigestiveDiseases>(s)).ToList(),
                hormonalAndGlandular = dto.HormonalAndGlandular?.Select(s => Enum.Parse<HormonalAndGlandularDiseases>(s)).ToList(),
                specific = dto.Specific?.Select(s => Enum.Parse<SpecificDiseases>(s)).ToList(),
                others = dto.Others
            };
        }

        public static InjuryAreaDto ToInjuryAreaDto(this InjuryArea model)
        {
            if (model == null) return null;

            return new InjuryAreaDto
            {
                None = model.none,
                Skeletal = model.skeletal?.Select(e => e.ToString()).ToList(),
                SoftTissueAndLigament = model.softTissueAndLigament?.Select(e => e.ToString()).ToList(),
                InternalAndDigestive = model.internalAndDigestive?.Select(e => e.ToString()).ToList(),
                HormonalAndGlandular = model.hormonalAndGlandular?.Select(e => e.ToString()).ToList(),
                Specific = model.specific?.Select(e => e.ToString()).ToList(),
                Others = model.others
            };
        }
    }
}
