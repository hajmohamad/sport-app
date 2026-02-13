using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Dtos
{
    public class RoleGenderDto
    {
        public  string? Role{ get; set; }
        public string? Gender{ get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }        
    }
}