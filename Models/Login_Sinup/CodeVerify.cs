using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Login_Sinup;

public class CodeVerify     
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set;}
    [Required]
    [StringLength(11)]
    [RegularExpression(@"^09\d{11}$")]
    public required string PhoneNumber {get; set;}
    public required string Code {get; set;}     
    public DateTime TimeCodeSend{get; set;}
}
