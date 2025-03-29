using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Dtos
{
    public class AddExercisesRequestDto
    {   
    public  string EnglishName { get; set; }=string.Empty;
    public  string PersianName { get; set; }=string.Empty;
    public string MainImage { get; set; } = string.Empty;
    public string AnatomyImage { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; }="";
    public int Calories { get; set; }=0;
    public required List<string> Image { get; set; }=[];
    public string BaseCategory { get; set; }=string.Empty;

    public required List<string> ActionsTage { get; set; }=[];
    }
}