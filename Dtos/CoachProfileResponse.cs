using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Dtos
{
    public class CoachProfileResponse
    {
        public int Id { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? BirthDate { get; set; }
        public string? UserName { get; set; }

        public required string PhoneNumber { get; set; }

        public string? Gender { get; set; }
        public byte[] ImageProfile { get; set; } = Array.Empty<byte>();
      
        public List<string> Bio { get; set; } = [];
         public List<string>? Domain { get; set; }

        public int StartCoachingYear { get; set; }
        public required List<CoachingServiceResponse> CoachingServices{ get; set; }
        public required List<AllPaymentResponseDto> Payments { get; set; }

    }
}