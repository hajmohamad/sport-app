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

        public string ImageProfile { get; set; }="";
        public string Bio { get; set; } = "";
        public string HeadLine { get; set; } = "";

    }
}