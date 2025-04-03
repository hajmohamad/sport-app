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
    public string? Gender { get; set; }
    public byte[] ImageProfile { get; set; } = Array.Empty<byte>();
    public required int TimeBeforeWorkout { get; set; }=10;
    public required int RestTime { get; set; } = 30;

    public string Bio { get; set; } = "";

    }
}