using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace sport_app_backend.Dtos.ZarinPal
{
    public class ZarinpalVerifyApiResponseDto
    {
        [JsonProperty("data")]
        public ZarinpalVerifyData Data { get; set; }

        [JsonProperty("errors")]
        public object Errors { get; set; }

    
        public List<ZarinpalError> GetErrors()
        {
            switch (Errors)
            {
                case null:
                    return [];
                case JArray errorsArray:
                    return errorsArray.ToObject<List<ZarinpalError>>();
                case JObject errorObject:
                {
                    var singleError = errorObject.ToObject<ZarinpalError>();
                    return [singleError];
                }
                default:
                    return [];
            }
        }

       
        public bool HasErrors()
        {
            switch (Errors)
            {
                case JArray jArray when !jArray.HasValues:
                case JObject jObject when !jObject.HasValues:
                    return false;
                default:
                    return Errors != null;
            }
        }
    }
}