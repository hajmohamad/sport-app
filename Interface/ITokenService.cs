using sport_app_backend.Models.Account;

namespace sport_app_backend.Interface;

public interface ITokenService
{
        string CreateToken(User user);
        Task<string> CreateRefreshToken(User user);

}
