using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Dtos;

public class DiscountCodeCreateDto
{
    [MaxLength(32)]
    public required string Code { get; set; }
    public required int DiscountPercent { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<int>? CoachServiceId { get; set; }
}

public class DiscountCodeUpdateDto
{
    [MaxLength(32)]
    public required string Code { get; set; }
    public required int DiscountPercent { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Status { get; set; }
    public List<int>? CoachServiceId { get; set; }

}

public class DiscountCodeListItemDto
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required int DiscountPercent { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public required string Status { get; set; }
    public List<ServiceForDiscountDto>? ServiceForDiscountDtos { get; set; }


}



public class CheckoutDiscountRequestDto
{
    public string? DiscountCode { get; set; }
}

public class DiscountPreviewDto
{
    public double OriginalPrice { get; set; }
    public double PublicDiscountAmount { get; set; }
    public double CodeDiscountAmount { get; set; }
    public double CodeDiscountPercent { get; set; }
    public double PublicDiscountPercent { get; set; }
    public double FinalPrice { get; set; }

    
    
   
}
