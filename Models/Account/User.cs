using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace sport_app_backend.Models.Account;

public class User
{   
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public  int Id { get; set; }
    
    [StringLength(15)]
    public string? FirstName { get; set; }
    [StringLength(50)]
    public  string? LastName { get; set; }
    [MaxLength(15)]
    [MinLength(6)]
    public string? UserName { get; set; } 
    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime BirthDate { get; set; }
    [Required]
    [StringLength(11)]
    [RegularExpression(@"^09\d{11}$")]

    public required string PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime CreateDate { get; set; }=DateTime.Now.Date;

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
    public DateTime LastLogin { get; set; } 
    public Gender Gender { get; set; }
    public byte[] ImageProfile { get; set; } = Array.Empty<byte>();
    [MaxLength(3)]
    public string Bio { get; set; } = "";
    public Athlete? Athlete { get; set; }
    public Coach? Coach { get; set; }
    public TypeOfUser TypeOfUser { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokeNExpire { get; set; }

}
