using sport_app_backend.Dtos.Eitaa;
using EitaaValidationResult = sport_app_backend.Dtos.Eitaa.EitaaValidationResult;

namespace sport_app_backend.Interface;

public interface IDataValidator
{
    EitaaValidationResult Validate(string rawData);

}