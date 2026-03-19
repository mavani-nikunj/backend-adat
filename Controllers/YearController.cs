using AdatHisabdubai.Data;
using AdatHisabdubai.Dto;
using AdatHisabdubai.Extensions;
using JangadHisabApp.Dtos;
using JangadHisabApp.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JangadHisabApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class YearMasterController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly AdatHisabAppContext _context;

        public YearMasterController(ITokenService tokenService, AdatHisabAppContext context)
        {
            _tokenService = tokenService;
            _context = context;
        }
        // ✅ SELECT  YEARS FOR CONTINUE
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetYearForContinue([FromBody] Yearselectdto dto)
        {
            var years = await _context.Yearmasters
                .Where(y => y.Id == dto.YearId && y.ClientId == User.GetClientId() && y.Isdelete == false)
                .FirstOrDefaultAsync();
            if (years == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "No financial years found",
                    Data = new List<object>()
                });
            }

            var user = await _context.Clientmasters.Where(z => z.UserName == User.GetUserId()).FirstOrDefaultAsync();
            var role = await _context.Rolemsts.Where(r => r.Id == user.UserRoleId).Select(z => z.Name).FirstOrDefaultAsync();
            tokendto client = new tokendto
            {
                Id = User.GetClientId(),
                Name = user.Name,
                Phoneno = user.Phoneno,
                Email = user.Email,
                YearId = years.Id,

                RoleId = user.UserRoleId,
                UserId = user.UserName,
                Role = role,

            };

            var token = _tokenService.GenerateToken(client);
            return Ok(new
            {
                Success = true,
                Message = "Year list fetched successfully",
                Data = years,
                Token = token
            });
        }
        // ✅ GET ALL YEARS
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var years = await _context.Yearmasters
                .Where(y => y.ClientId == User.GetClientId() && y.Isdelete == false)
                .OrderByDescending(y => y.Id)
                .Select(y => new
                {
                    y.Id,
                    Fromyear = y.Fromyear,
                    Toyear = y.Toyear,
                    Client = new
                    {
                        id = y.ClientId,
                        Name = _context.Clientmasters.Where(c => c.Id == y.ClientId).Select(c => c.Name).FirstOrDefault(),
                        Userid = _context.Clientmasters.Where(c => c.Id == y.ClientId).Select(c => c.UserName).FirstOrDefault(),
                    },
                    y.Cdate,
                    y.Udate
                })
                .ToListAsync();

            if (!years.Any())
            {
                return Ok(new
                {
                    Success = false,
                    Message = "No financial years found",
                    Data = new List<object>()
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Year list fetched successfully",
                Data = years
            });
        }

        // ✅ GET YEAR BY ID
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var year = await _context.Yearmasters
                .Where(y => y.Id == id && y.ClientId == User.GetClientId() && y.Isdelete == false)
                .Select(y => new
                {
                    y.Id,
                    Fromyear = y.Fromyear,
                    Toyear = y.Toyear,
                    Client = new
                    {
                        id = y.ClientId,
                        Name = _context.Clientmasters.Where(c => c.Id == y.ClientId).Select(c => c.Name).FirstOrDefault(),
                        Userid = _context.Clientmasters.Where(c => c.Id == y.ClientId).Select(c => c.UserName).FirstOrDefault(),
                    },
                    y.Cdate,
                    y.Udate
                })
                .FirstOrDefaultAsync();

            if (year == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Year not found",
                    Data = new List<object>()
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Year fetched successfully",
                Data = year
            });
        }

        // ✅ CREATE YEAR
        [HttpPost]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> Add([FromBody] Yearmaster model)
        {
            try
            {
                if (model.Fromyear == null || model.Toyear == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "From year and To year are required"
                    });
                }

                if (model.Fromyear > model.Toyear)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "From year cannot be greater than To year"
                    });
                }

                // 🔁 DUPLICATE YEAR CHECK
                bool exists = await _context.Yearmasters.AnyAsync(y =>
                    y.ClientId == User.GetClientId() &&
                    y.Fromyear == model.Fromyear &&
                    y.Toyear == model.Toyear);

                if (exists)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Financial year already exists"
                    });
                }

                model.ClientId = User.GetClientId();
                model.UserId = User.GetUserId();
                model.Cdate = DateTime.Now;
                model.Isdelete = false;
                _context.Yearmasters.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Year created successfully",
                    Data = model
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ✅ UPDATE YEAR
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] Yearmaster dto)
        {
            try
            {
                var year = await _context.Yearmasters
                    .FirstOrDefaultAsync(y => y.Id == id && y.ClientId == User.GetClientId() && y.Isdelete == false);

                if (year == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Year not found",
                        Data = new List<object>()
                    });
                }

                // 🔹 Determine final values (old or new)
                var finalFromYear = dto.Fromyear ?? year.Fromyear;
                var finalToYear = dto.Toyear ?? year.Toyear;

                // 🔹 Validation
                if (finalFromYear == null || finalToYear == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "From year and To year are required"
                    });
                }

                if (finalFromYear > finalToYear)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "From year cannot be greater than To year"
                    });
                }

                // 🔁 DUPLICATE CHECK (EXCLUDING CURRENT ID)
                bool exists = await _context.Yearmasters.AnyAsync(y =>
                    y.ClientId == User.GetClientId() &&
                    y.Id != id &&
                    y.Fromyear == finalFromYear &&
                    y.Toyear == finalToYear);

                if (exists)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Financial year already exists"
                    });
                }
                // 🔹 APPLY UPDATE
                year.Fromyear = finalFromYear;
                year.Toyear = finalToYear;

                year.Udate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Year updated successfully",
                    Data = year
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ✅ DELETE YEAR (HARD DELETE)
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {



            var year = await _context.Yearmasters
                .FirstOrDefaultAsync(y => y.Id == id && y.ClientId == User.GetClientId() && y.Isdelete == false);

            if (year == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Year not found",
                    Data = (string?)null
                });
            }

            year.Isdelete = true; // SOFT DELETE
            year.Udate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Year deleted successfully",
                Data = year
            });

        }
    }
}
