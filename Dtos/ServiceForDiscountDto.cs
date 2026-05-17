namespace sport_app_backend.Dtos;

public class ServiceForDiscountDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public double OriginalPrice { get; set; }
    public double DiscountPrice { get; set; }
    public bool IsActive { get; set; }
}