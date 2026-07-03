namespace sport_app_backend.Dtos.Coach
{
    public class CoachProfileResponse
    {
        public int Id { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? BirthDate { get; set; }
        public string? UserName { get; set; }

        public required string PhoneNumber { get; set; }

        public string? Gender { get; set; }
        public string ImageProfile { get; set; }="";
      
        
        public required List<CoachingServiceResponse> CoachingServices{ get; set; }
        public required int NumberOfAthlete { get; set; }
        public required int NumberOfProgram { get; set; }
        public string? WebsiteUrl { get; set; } = "";
        public string? Slogan { get; set; } = "";
        public string? SiteDescription { get; set; } = "";
        public bool HasPersonalDetails { get; set; }
        public bool HasCommunicationChannels { get; set; }
        public bool HasWebsiteAddress { get; set; }
        public bool HasUserReviews { get; set; }
        public bool HasAthleteChange { get; set; }

        public int CompletionPercentage { get; set; }
        public CoachProfileStatus Status { get; set; }

    }
    public enum CoachProfileStatus
    {
        NeedsCompletion,
        PendingApproval,
        Verified
    }
}