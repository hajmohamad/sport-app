namespace sport_app_backend.Dtos;

public class ActivityDto
{
    public int Id { get; set; }
    public string? Date { get; set; }
    public double CaloriesLost { get; set; }
    public double Duration { get; set; }
    public double Distance { get; set; }
    public string? ActivityCategory {get; set;}
    public string? Name {get; set;}


}