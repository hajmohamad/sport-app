using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Dtos
{
    public class CoachingServiceResponse
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required double Price { get; set; }
        public bool IsActive { get; set; }
        public bool HaveSupport { get; set; }
        public required string CommunicateType { get; set; }
        public required int NumberOfSell{ get; set; }

    }
}