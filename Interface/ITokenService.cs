using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Interface;

public interface ITokenService
{
        string CreateTokenForSite(User user);
        string CreateTokenForApp(TokenUserDto user);
        Task<string> CreateRefreshToken(User user);
        Task<string> CreateSiteRefreshToken(User user);

        string HashEncode(int id);
        int DecodeHash(string hash);
        string GenerateSecureToken();
        string Sha256Hex(string input);



}
