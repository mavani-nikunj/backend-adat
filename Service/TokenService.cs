using JangadHisabApp.Dtos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JangadHisabApp.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config)
        {
            _config = config;
        }
        public string GenerateToken(tokendto tokenDto)
        {
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //var claims = new List<Claim>();

            //foreach (PropertyInfo prop in clientdto.GetType().GetProperties())
            //{
            //    var value = prop.GetValue(clientdto)?.ToString() ?? "";
            //    claims.Add(new Claim(prop.Name, value));
            //}
            ////claims.Add(new Claim("yid", yid.ToString()));
            //claims.Add(new Claim(ClaimTypes.Role, clientdto.AccountName));
            var claims = new[]
 {
            new Claim(JwtRegisteredClaimNames.Sub, _config["Jwt:Subject"] ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,tokenDto.Role),
            new Claim("UserId", tokenDto.UserId ?? ""),
            new Claim("Phoneno", tokenDto.Phoneno ?? ""),
            new Claim("ClientId", tokenDto.Id.ToString() ?? ""),
            new Claim("RoleId", tokenDto.RoleId.ToString() ?? ""),
            new Claim("Name",tokenDto.Name ?? ""),
           new Claim("YearId",tokenDto.YearId.ToString() ?? ""),
            new Claim("Email",tokenDto.Email ?? ""),



        };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:DurationInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
