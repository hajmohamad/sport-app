using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Eitaa;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.UserExternalAccount;

namespace sport_app_backend.Services;

public class EitaaAuthService(
    ApplicationDbContext dbContext,
    IDataValidator eitaaDataValidator,
    ITokenService tokenService,
    IConfiguration configuration)
    : IEitaaAuthService
{

    public async Task<ApiResponse> LoginAsync(
        EitaaLoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validation = eitaaDataValidator.Validate(request.InitData);

        if (!validation.IsValid)
        {
            return Failed(validation.Error ?? "Invalid Eitaa initData.");
        }

        if (!ValidateAuthDate(validation.Data, out var authDateError))
        {
            return Failed(authDateError);
        }

        EitaaUserDto? eitaaUser;
        try
        {
            eitaaUser = ExtractEitaaUser(validation.Data);
        }
        catch (JsonException)
        {
            return Failed("Eitaa user data has invalid JSON.");
        }

        if (eitaaUser is null || eitaaUser.Id <= 0)
        {
            return Failed("Eitaa user data is missing or invalid.");
        }

        var eitaaUserId = eitaaUser.Id.ToString();

        var externalAccount = await dbContext.UserExternalAccounts
            .Include(x => x.User)
            .ThenInclude(x => x.Athlete)
            .Include(x => x.User)
            .ThenInclude(x => x.Coach)
            .SingleOrDefaultAsync(
                x => x.Provider ==Provider.Eita &&
                     x.ProviderUserId == eitaaUserId,
                cancellationToken);

        if (externalAccount is not null)
        {
            if (externalAccount.User.UserIsBan)
            {
                return Failed("User is banned.");
            }

            var now = DateTime.Now;

            externalAccount.LastLoginAt = DateTime.UtcNow;
            externalAccount.User.LastLogin = now;

            await dbContext.SaveChangesAsync(cancellationToken);

            return await CreateAuthenticatedResponseAsync(externalAccount.User);
        }

        var activeSessions = await dbContext.EitaaLoginSessions
            .Where(x =>
                x.EitaaUserId == eitaaUserId &&
                x.ConsumedAt == null &&
                x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var activeSession in activeSessions)
        {
            activeSession.ExpiresAt = DateTime.UtcNow;
        }

        var linkingToken = tokenService.GenerateSecureToken();

        var loginSession = new EitaaLoginSession
        {
            TokenHash = tokenService.Sha256Hex(linkingToken),
            EitaaUserId = eitaaUserId,
            EitaaUsername = eitaaUser.Username,
            FirstName = eitaaUser.FirstName,
            LastName = eitaaUser.LastName,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetLinkingTokenExpireMinutes())
        };

        await dbContext.EitaaLoginSessions.AddAsync(
            loginSession,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ApiResponse
        {
            Action = true,
            Message = "Contact is required.",
            Result = new
            {
                authenticated = false,
                requiresContact = true,
                linkingToken
            }
        };


    }

public async Task<ApiResponse> CompleteLoginAsync(
    EitaaCompleteLoginRequestDto request,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(request.LinkingToken))
        return Failed("Linking token is required.");

    var tokenHash = tokenService.Sha256Hex(request.LinkingToken);

    // اینها خارج از تراکنش مشکلی ندارند
    var session = await dbContext.EitaaLoginSessions
        .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    if (session is null) return Failed("Invalid linking token.");
    if (session.ConsumedAt is not null) return Failed("Linking token has already been used.");
    if (session.ExpiresAt <= DateTime.UtcNow) return Failed("Linking token has expired.");

    var validation = eitaaDataValidator.Validate(request.ContactData);
    if (!validation.IsValid) return Failed(validation.Error ?? "Invalid Eitaa contactData.");

    if (!ValidateAuthDate(validation.Data, out var authDateError))
        return Failed(authDateError);

    EitaaContactDto? contact;
    try
    {
        contact = ExtractContact(validation.Data);
    }
    catch (JsonException)
    {
        return Failed("Eitaa contact data has invalid JSON.");
    }

    if (contact is null || string.IsNullOrWhiteSpace(contact.PhoneNumber))
        return Failed("Phone number is missing in contact data.");

    if (contact.UserId.HasValue && contact.UserId.Value.ToString() != session.EitaaUserId)
        return Failed("The shared contact does not belong to the Eitaa user.");

    string normalizedPhoneNumber;
    try
    {
        normalizedPhoneNumber = PhoneNumberHelper.NormalizeIranPhoneNumber(contact.PhoneNumber);
    }
    catch (ArgumentException)
    {
        return Failed("Invalid Iranian phone number.");
    }

    // نکته اصلی: تراکنش را داخل execution strategy اجرا کن
    var strategy = dbContext.Database.CreateExecutionStrategy();

    try
    {
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingExternalAccount =
                    await dbContext.UserExternalAccounts
                        .Include(x => x.User).ThenInclude(x => x.Athlete)
                        .Include(x => x.User).ThenInclude(x => x.Coach)
                        .SingleOrDefaultAsync(
                            x => x.Provider == Provider.Eita &&
                                 x.ProviderUserId == session.EitaaUserId,
                            cancellationToken);

                if (existingExternalAccount is not null)
                {
                    if (existingExternalAccount.User.UserIsBan)
                        return Failed("User is banned.");

                    if (existingExternalAccount.User.PhoneNumber != normalizedPhoneNumber)
                        return Failed("This Eitaa account is already linked.");

                    session.ConsumedAt = DateTime.UtcNow;
                    existingExternalAccount.LastLoginAt = DateTime.UtcNow;
                    existingExternalAccount.User.LastLogin = DateTime.Now;

                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return await CreateAuthenticatedResponseAsync(existingExternalAccount.User);
                }

                var user = await dbContext.Users
                    .Include(x => x.Athlete)
                    .Include(x => x.Coach)
                    .SingleOrDefaultAsync(
                        x => x.PhoneNumber == normalizedPhoneNumber,
                        cancellationToken);

                if (user is null)
                {
                    user = await CreateNewAthleteUser(
                        normalizedPhoneNumber, session, cancellationToken);
                }
                else
                {
                    if (user.UserIsBan)
                        return Failed("User is banned.");

                    user.LastLogin = DateTime.Now;

                    if (string.IsNullOrWhiteSpace(user.FirstName))
                        user.FirstName = session.FirstName ?? "";

                    if (string.IsNullOrWhiteSpace(user.LastName))
                        user.LastName = session.LastName ?? "";
                }

                var externalAccount = new UserExternalAccount
                {
                    UserId = user.Id,
                    User = user,
                    Provider = Provider.Eita,
                    ProviderUserId = session.EitaaUserId,
                    ProviderUsername = session.EitaaUsername,
                    FirstName = session.FirstName,
                    LastName = session.LastName,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                await dbContext.UserExternalAccounts.AddAsync(externalAccount, cancellationToken);

                session.ConsumedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await CreateAuthenticatedResponseAsync(user);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed("The phone number or Eitaa account has already been linked.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("does not support user-initiated transactions",
            StringComparison.OrdinalIgnoreCase))
    {
        throw;
    }
}

    private async Task<User> CreateNewAthleteUser(
        string phoneNumber,
        EitaaLoginSession session,
        CancellationToken cancellationToken)
    {
        var newUser = new User
        {
            UserName = await GenerateUniqueUsername(cancellationToken),
            PhoneNumber = phoneNumber,
            FirstName = session.FirstName ?? "",
            LastName = session.LastName ?? "",
            TypeOfUser = TypeOfUser.ATHLETE,
            LastLogin = DateTime.Now,
            CreateDate = DateTime.Now.Date
        };

        await dbContext.Users.AddAsync(newUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var athlete = new Athlete
        {
            UserId = newUser.Id,
            User = newUser,
            PhoneNumber = phoneNumber
        };

        await dbContext.Athletes.AddAsync(athlete, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        newUser.Athlete = athlete;
        newUser.AthleteId = athlete.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return newUser;
    }

    private async Task<string> GenerateUniqueUsername(
        CancellationToken cancellationToken)
    {
        string username;
        do
        {
            username = Guid.NewGuid().ToString("N")[..8];
        } while (await dbContext.Users.AnyAsync(
                     x => x.UserName == username,
                     cancellationToken));

        return username;
    }


    private async Task<ApiResponse> CreateAuthenticatedResponseAsync(User user)
    {
        var refreshToken = user.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            refreshToken = await tokenService.CreateRefreshToken(user);
            user.RefreshToken = refreshToken;
            await dbContext.SaveChangesAsync();
        }

        var accessToken = tokenService.CreateTokenForApp(new TokenUserDto
        {
            Id = user.Id,
            TypeOfUser = user.TypeOfUser,
            AthleteId = user.Athlete?.Id ?? user.AthleteId,
            CoachId = user.Coach?.Id ?? user.CoachId,
            PhoneNumber = user.PhoneNumber,
            LastLogin = user.LastLogin
        });

        return new ApiResponse
        {
            Action = true,
            Message = "Login successful.",
            Result = new
            {
                authenticated = true,
                requiresContact = false,
                linkingToken = (string?)null,
                accessToken,
                refreshToken,
                typeOfUser = user.TypeOfUser.ToString(),
                gender = user.Gender.ToString(),
                questions = !string.IsNullOrWhiteSpace(user.FirstName),
                user = new
                {
                    id = user.Id,
                    phoneNumber = user.PhoneNumber,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    userName = user.UserName
                }
            }
        };
    }

    private static EitaaUserDto? ExtractEitaaUser(
        Dictionary<string, StringValues> data)
    {
        if (!data.TryGetValue("user", out var userValue))
        {
            return null;
        }

        var json = userValue.ToString();

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<EitaaUserDto>(json);
    }
    

    private static EitaaContactDto? ExtractContact(
        Dictionary<string, StringValues> data)
    {
        if (data.TryGetValue("contact", out var contactValue))
        {
            var json = contactValue.ToString();

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    return JsonSerializer.Deserialize<EitaaContactDto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch (JsonException)
                {
                    // JSON نامعتبر است؛ ادامه با فرمت فیلدهای جداگانه
                }
            }
        }

        var phoneNumber = GetValue(data, "phone_number")
                          ?? GetValue(data, "phone");

        var rawUserId = GetValue(data, "user_id");

        long? userId = long.TryParse(rawUserId, out var parsedUserId)
            ? parsedUserId
            : null;

        var firstName = GetValue(data, "first_name");
        var lastName = GetValue(data, "last_name");

        if (string.IsNullOrWhiteSpace(phoneNumber) &&
            userId is null &&
            string.IsNullOrWhiteSpace(firstName) &&
            string.IsNullOrWhiteSpace(lastName))
        {
            return null;
        }

        return new EitaaContactDto
        {
            PhoneNumber = phoneNumber,
            UserId = userId,
            FirstName = firstName,
            LastName = lastName
        };
    }

    private bool ValidateAuthDate(
        Dictionary<string, StringValues> data,
        out string error)
    {
        error = "";

        if (!data.TryGetValue("auth_date", out var authDateValue))
        {
            error = "auth_date is missing.";
            return false;
        }

        if (!long.TryParse(authDateValue.ToString(), out var unixTime))
        {
            error = "auth_date is invalid.";
            return false;
        }

        DateTime authDate;

        try
        {
            authDate = DateTimeOffset
                .FromUnixTimeSeconds(unixTime)
                .UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "auth_date is invalid.";
            return false;
        }

        var now = DateTime.UtcNow;

        if (authDate > now.AddMinutes(5))
        {
            error = "auth_date is in the future.";
            return false;
        }

        if (authDate.AddMinutes(GetInitDataMaxAgeMinutes()) < now)
        {
            error = "Eitaa data has expired.";
            return false;
        }

        return true;
    }

 
    private int GetInitDataMaxAgeMinutes()
    {
        return int.TryParse(
            configuration["Eitaa:InitDataMaxAgeMinutes"],
            out var value)
            ? value
            : 1440;
    }

    private int GetLinkingTokenExpireMinutes()
    {
        return int.TryParse(
            configuration["Eitaa:LinkingTokenExpireMinutes"],
            out var value)
            ? value
            : 5;
    }

    private static string? GetValue(
        Dictionary<string, StringValues> data,
        string key)
    {
        return data.TryGetValue(key, out var value)
            ? value.ToString()
            : null;
    }

    private static ApiResponse Failed(string message)
    {
        return new ApiResponse
        {
            Action = false,
            Message = message,
            Result = null
        };
    }

}
