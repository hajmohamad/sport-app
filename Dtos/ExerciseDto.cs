namespace sport_app_backend.Dtos;

public class ExerciseDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExerciseCategories { get; set; }= string.Empty;
    public string Equipment { get; set; }= string.Empty;
    public List<string> Muscles { get; set; } = [];
    public string EnglishName { get; set; }= string.Empty;
    public string PersianName { get; set; }= string.Empty;
    public string ImageLink { get; set; }= string.Empty;
    public string VideoLink { get; set; }= string.Empty;
    public string BaseCategory { get; set; }= string.Empty;
    public string ExerciseLevel { get; set; }= string.Empty;
    public string Mechanics { get; set; }= string.Empty;
   


}