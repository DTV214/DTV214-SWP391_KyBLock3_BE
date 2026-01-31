using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            return User.FindFirstValue(ClaimTypes.Role) ?? "Customer";
        }
    }
}
