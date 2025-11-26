namespace sport_app_backend.Dtos.ProgramDto;

public class ExerciseForTrainingSessionDto(string ss, int aa)
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
   public string PersianName { get; set; }= string.Empty;
    public string ImageLink { get; set; }= string.Empty;
    public string VideoLink { get; set; }= string.Empty;


}