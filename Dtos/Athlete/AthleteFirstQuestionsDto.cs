namespace sport_app_backend.Dtos
{
    public class AthleteFirstQuestionsDto
    {
        public required string FirstName { get; set; }
        public required string LastName{ get; set; }
        public double CurrentWeight{ get; set; }
        public int Height{ get; set; }
    }
}
