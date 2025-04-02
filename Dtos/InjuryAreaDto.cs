namespace sport_app_backend.Dtos
{
    public class InjuryAreaDto
    {
        public bool None { get; set; }
        public List<string>? Skeletal { get; set; }
        public List<string>? SoftTissueAndLigament { get; set; }
        public List<string>? InternalAndDigestive { get; set; }
        public List<string>? HormonalAndGlandular { get; set; }
        public List<string>? Specific { get; set; }
        public string? Others { get; set; }
    }
}
