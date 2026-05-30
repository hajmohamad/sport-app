namespace sport_app_backend.Dtos;

public class AddAthleteChangePhotoDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile File { get; set; } = default!;

}

public class EditAthleteChangePhotoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile File { get; set; } = default!;

}
