using AdatHisabdubai.Data;
using AdatHisabdubai.Dto;
using AdatHisabdubai.Extensions;
using JangadHisabApp.Dtos;
using JangadHisabApp.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdatHisabdubai.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly AdatHisabAppContext _context;
        public UserController(ITokenService tokenService, AdatHisabAppContext context)
        {
            _tokenService = tokenService;
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            try
            {
                var user = await _context.Clientmasters.FirstOrDefaultAsync(z => z.UserName == login.Username);
                if (user == null)
                {
                    return StatusCode(500, new { Success = false, Message = "User does not exist", Data = (object?)null });
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(login.Password, user.UserPassword);
                if (!isPasswordValid)
                {
                    return StatusCode(500, new { Success = false, Message = "Password is incorrect", Data = (object?)null });
                }

                var role = await _context.Rolemsts.FirstOrDefaultAsync(r => r.Id == user.UserRoleId);
                var year = await _context.Yearmasters.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                tokendto client = new tokendto
                {
                    Id = user.Id, // Default ClientId set to 3 as requested
                    Name = user.Name,
                    Phoneno = user.Phoneno,
                    Email = user.Email,
                    YearId = year?.Id ?? null, // Default YearId set to 1 as requested
                    RoleId = user.UserRoleId,
                    UserId = user.UserName,
                    Role = role?.Name ?? "Admin",
                };

                var token = _tokenService.GenerateToken(client);
                return Ok(new { Success = true, Message = "Login successfully", Data = client, Token = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpPost]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> Register([FromBody] Registerdot register)
        {
            try
            {
                bool duplicate = await _context.Clientmasters.AnyAsync(x =>
                  x.Email == register.Email ||
                  x.UserName == register.UserName
                 || x.Name == register.Name);


                if (duplicate)
                {
                    return StatusCode(500, new { Success = true, Message = "User already exists with same Email / Username / Name", Data = (object?)null });
                }
                bool roleidcheck = await _context.Rolemsts.AnyAsync(x => x.Id == register.UserRoleId);
                if (!roleidcheck)
                {
                    return StatusCode(500, new { Success = false, Message = "RoleId is invalid", Data = (object?)null });
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(register.UserPassword);
                var newUser = new Clientmaster
                {
                    UserName = register.UserName,
                    UserPassword = hashedPassword,
                    Name = register.Name,
                    Email = register.Email,
                    Address = register.Address,
                    Phoneno = register.Phoneno,
                    Remark = register.Remark,

                    Cdate = DateTime.Now,
                    Isdelete = false,
                    UserRoleId = register.UserRoleId, // Default role as User
                    ClientId = User.GetClientId()

                };
                _context.Clientmasters.Add(newUser);
                await _context.SaveChangesAsync();


                return Ok(new { Success = true, Message = "User registered successfully", Data = newUser });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpGet]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                int clientId = User.GetClientId();
                var users = await _context.Clientmasters

                    .Select(x => new
                    {
                        x.Id,
                        x.UserName,
                        x.Name,
                        x.Email,
                        x.Phoneno,
                        x.Address,
                        x.Remark,
                        x.UserRoleId,
                        RoleName = _context.Rolemsts.FirstOrDefault(r => r.Id == x.UserRoleId)!.Name
                    })
                    .ToListAsync();
                if (users == null || users.Count == 0)
                {
                    return Ok(new { Success = true, Message = "No users found", Data = users });
                }
                return Ok(new { Success = true, Message = "Users fetched successfully", Data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpGet]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> GetUserById(int? id)
        {
            try
            {
                int clientId = User.GetClientId();
                var user = await _context.Clientmasters
                    .Where(x => x.ClientId == clientId && x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.UserName,
                        x.Name,
                        x.Email,
                        x.Phoneno,
                        x.Address,
                        x.Remark,
                        x.UserRoleId,
                        RoleName = _context.Rolemsts.FirstOrDefault(r => r.Id == x.UserRoleId)!.Name
                    })
                    .FirstOrDefaultAsync();
                if (user == null)
                {
                    return StatusCode(500, new { Success = true, Message = "User not found", Data = (object?)null });
                }
                return Ok(new { Success = true, Message = "User fetched successfully", Data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpPut]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] Updateuserdto updateUser)
        {
            int clientId = User.GetClientId();

            var user = await _context.Clientmasters
                .FirstOrDefaultAsync(x => x.Id == id && x.ClientId == clientId);

            if (user == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            bool duplicate = await _context.Clientmasters.AnyAsync(x =>
                x.ClientId == clientId &&
                x.Id != id &&
                (x.Email == updateUser.Email ||
                 x.UserName == updateUser.UserName ||
                 x.Name == updateUser.Name));

            if (duplicate)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Another user already exists with same Email / Username / Name"
                });
            }

            bool roleExists = await _context.Rolemsts
                .AnyAsync(x => x.Id == updateUser.UserRoleId);

            if (!roleExists)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Invalid role selected"
                });
            }

            if (!string.IsNullOrWhiteSpace(updateUser.UserName))
                user.UserName = updateUser.UserName;

            if (!string.IsNullOrWhiteSpace(updateUser.Name))
                user.Name = updateUser.Name;

            if (!string.IsNullOrWhiteSpace(updateUser.Email))
                user.Email = updateUser.Email;

            if (updateUser.Address != null)
                user.Address = updateUser.Address;

            if (updateUser.Phoneno != null)
                user.Phoneno = updateUser.Phoneno;

            if (updateUser.Remark != null)
                user.Remark = updateUser.Remark;

            if (updateUser.UserRoleId.HasValue)
                user.UserRoleId = updateUser.UserRoleId.Value;

            user.Udate = DateTime.Now;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "User updated successfully"
            });
        }

    }
}
