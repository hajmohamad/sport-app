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
        public double FinalPrice { get; set; }
        public bool IsActive { get; set; }
        public bool HasPublicDiscount { get; set; }
        public string? PublicDiscountType { get; set; }
        public double? PublicDiscountPercent { get; set; }
        public DateTime? PublicDiscountStartsAt { get; set; }
        public DateTime? PublicDiscountExpiresAt { get; set; }
        // public required string CommunicateType { get; set; }
        public required int NumberOfSell{ get; set; }

    }
}
