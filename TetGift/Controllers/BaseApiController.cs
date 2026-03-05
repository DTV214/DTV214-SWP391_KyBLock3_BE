using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Common.Constraint;

namespace TetGift.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected int GetAccountId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(idStr, out var id) || id <= 0)
                throw new UnauthorizedAccessException("Invalid token (missing account id).");
            return id;
        }

        protected string GetRole()
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRole.CUSTOMER;
            return role.ToUpper(); // Normalize to uppercase
        }

        protected int GetId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            return int.Parse(id);
        }
    }
}
