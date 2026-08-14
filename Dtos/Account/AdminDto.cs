using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Dtos.Account
{
    public class AdminLoginRequestDto
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        public string Username { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        public string Password { get; set; }
    }
}