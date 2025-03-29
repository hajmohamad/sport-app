using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Dtos
{
    public class CoachForSearch
    {   public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public required string UserName { get; set; }

        public byte[] ImageProfile { get; set; } = Array.Empty<byte>();
        public string Bio { get; set; } = string.Empty;

    }
}