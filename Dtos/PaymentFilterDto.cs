namespace sport_app_backend.Dtos;
public class PaymentFilterDto
{
    public string? SortBy { get; set; } = "date"; // date, service, amount, athlete
    public bool SortDesc { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
