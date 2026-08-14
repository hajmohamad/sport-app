using System.ComponentModel.DataAnnotations;
using sport_app_backend.Models.Account.Coach;

namespace sport_app_backend.Dtos.Admin
{
    public class ChangeCoachStatusDto
    {
        [Required(ErrorMessage = "آیدی مربی الزامی است")]
        public int CoachId { get; set; } // تغییر از PhoneNumber به CoachId

        [Required(ErrorMessage = "وضعیت جدید مربی الزامی است")]
        public CoachStatus Status { get; set; } 
    }
}