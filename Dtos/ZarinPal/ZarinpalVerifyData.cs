namespace sport_app_backend.Dtos;

public class ZarinpalVerifyData
{
    public int Code { get; set; }
    public string Message { get; set; } 
    public string Card_hash { get; set; }
    public string Card_pan { get; set; }
    public long Ref_id { get; set; }
    public string Fee_type { get; set; }
    public long Fee { get; set; }
}