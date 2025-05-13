namespace sport_app_backend.Controller
{
    public class AddActivityDto
    {

        public string? ActivityCategory { get; set; }
        public double Duration { get; set; }
        public double CaloriesLost{get;set;}
        public double Distance{get;set;}
        public required string DateTime { get; set; }
      
    }
}