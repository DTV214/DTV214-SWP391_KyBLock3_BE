using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _cfg;
        public JwtTokenGenerator(IConfiguration cfg) => _cfg = cfg;

        public string Generate(Account acc)
        {
            var key = _cfg["Jwt:Key"] ?? throw new Exception("Missing config: Jwt:Key");
            var issuer = _cfg["Jwt:Issuer"];
            var audience = _cfg["Jwt:Audience"];
            var expireMinutes = int.TryParse(_cfg["Jwt:ExpireMinutes"], out var m) ? m : 60;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, acc.Accountid.ToString()),
                new Claim(ClaimTypes.NameIdentifier, acc.Accountid.ToString()),
                new Claim(ClaimTypes.Name, acc.Username),
                new Claim(ClaimTypes.Email, acc.Email ?? ""),
                new Claim(ClaimTypes.Role, (acc.Role ?? UserRole.CUSTOMER).ToUpper())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: string.IsNullOrWhiteSpace(issuer) ? null : issuer,
                audience: string.IsNullOrWhiteSpace(audience) ? null : audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
