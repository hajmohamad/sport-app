namespace sport_app_backend.Dtos.Coach
{
    public class UpdateWebSiteUrlDto
    {
        public string WebSiteUrl { get; set; } = string.Empty;
    }

    public class CoachWebSiteUrlStatusDto
    {
        public string? WebSiteUrl { get; set; }
        public bool CanChange { get; set; }
        public int DaysRemaining { get; set; }
        public string? Message { get; set; }
    }
}