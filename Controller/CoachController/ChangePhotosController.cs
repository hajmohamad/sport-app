using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Models;

namespace sport_app_backend.Controller.CoachController;
[Authorize(Roles = "Coach")]
[Microsoft.AspNetCore.Components.Route("api/Coach/discount-codes")]
[ApiController]
public class ChangePhotosController(IChangePhotosRepository repository, ApplicationDbContext dbContext): ControllerBase
{
      #region changePhoto
        [HttpGet("ChangePhotos")]
            [Authorize(Roles = "Coach")]
            public async Task<IActionResult> GetAllChangePhotos()
            {
                var coachId = await GetCoachIdAsync();
                if (coachId == 0)
                {
                    return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                }

                var result = await repository.GetAllChangePhotos(coachId);
                if (!result.Action) return NotFound(result);
                return Ok(result);
            }

            [HttpGet("ChangePhotos/{id:int}")]
            [Authorize(Roles = "Coach")]
            public async Task<IActionResult> GetChangePhotoById(int id)
            {
                var coachId = await GetCoachIdAsync();
                if (coachId == 0)
                {
                    return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                }

                var result = await repository.GetChangePhotoById(coachId, id);
                if (!result.Action) return NotFound(result);
                return Ok(result);
            }

        
            [HttpPost("ChangePhotos")]
            [Authorize(Roles = "Coach")]
            [Consumes("multipart/form-data")]
            public async Task<IActionResult> AddChangePhoto([FromForm] AddAthleteChangePhotoDto dto)
            {
                var coachId = await GetCoachIdAsync();
                if (coachId == 0)
                    return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

                var result = await repository.AddChangePhoto(coachId, dto.PhotoUrl, dto);
                if (!result.Action) return BadRequest(result);
                return Ok(result);
            }

            [HttpPut("ChangePhotos/{id:int}")]
            [Authorize(Roles = "Coach")]
            [Consumes("multipart/form-data")]
            public async Task<IActionResult> EditChangePhoto(int id, [FromForm] EditAthleteChangePhotoDto dto)
            {
                var coachId = await GetCoachIdAsync();
                if (coachId == 0)
                    return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

                dto.Id = id;

                var result = await repository.EditChangePhoto(coachId, dto.PhotoUrl, dto);
                if (!result.Action) return BadRequest(result);
                return Ok(result);
            }

            [HttpDelete("ChangePhotos/{id:int}")]
            [Authorize(Roles = "Coach")]
            public async Task<IActionResult> DeleteChangePhoto(int id)
            {
                var coachId = await GetCoachIdAsync();
                if (coachId == 0)
                {
                    return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                }
                var result = await repository.DeleteChangePhoto(coachId, id);
                if (!result.Action) return BadRequest(result);
                return Ok(result);
            }
            #endregion
            private async Task<int> GetCoachIdAsync()
            {
                var coachIdClaim = User.FindFirst("coach_id")?.Value;
                if (int.TryParse(coachIdClaim, out var coachId))
                {
                    return coachId;
                }

                var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return 0;
                }

                var id = await dbContext.Coaches
                    .AsNoTracking()
                    .Where(c => c.PhoneNumber == phoneNumber)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();
                return id;


            }

        }
