using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Dtos
{
    public class WaterInDayDto
    {
    public int NumberOfCupsDrinked {get; set;}
   
    public required string Date {get; set;}
    
    }
}