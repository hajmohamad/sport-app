using sport_app_backend.Dtos.Eitaa;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IEitaaAuthService
{
    Task<ApiResponse> LoginAsync(
        EitaaLoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse> CompleteLoginAsync(
        EitaaCompleteLoginRequestDto request,
        CancellationToken cancellationToken = default);
}