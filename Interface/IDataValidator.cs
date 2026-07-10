using sport_app_backend.Dtos.Eitaa;

namespace sport_app_backend.Interface;

public interface IDataValidator
{
    EitaaValidationResult Validate(string rawData);

}