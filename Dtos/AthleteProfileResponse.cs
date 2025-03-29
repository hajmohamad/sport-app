using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Dtos
{
    public class AthleteProfileResponse
    {
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BirthDate { get; set; }
    public string? UserName { get; set; }
    public required string PhoneNumber { get; set; }
    public int Id { get; set; }
    public int Height { get; set; }
    public double CurrentWeight { get; set; }
    public double WeightGoal { get; set; }
    public List<string> InjuryArea { get; set; } = [];
    public string? FitnessLevel { get; set; }
    public int CurrentBodyForm { get; set; }
    public int TargetBodyForm { get; set; }
    public string? Gender { get; set; }
    public byte[] ImageProfile { get; set; } = Array.Empty<byte>();

    public string Bio { get; set; } = "";

    }
}