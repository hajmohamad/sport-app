namespace sport_app_backend.Dtos.Chat;

public class UploadAttachmentDto
{
    public string? FileUrl { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }
}