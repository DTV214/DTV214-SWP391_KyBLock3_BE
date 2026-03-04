using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/account/addresses")]
    [Authorize]
    public class AccountAddressesController : BaseApiController
    {
        private readonly IAccountAddressService _svc;

        public AccountAddressesController(IAccountAddressService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAddresses()
        {
            var accountId = GetAccountId();
            var result = await _svc.GetMyAddressesAsync(accountId);
            return Ok(result);
        }

        [HttpGet("default")]
        public async Task<IActionResult> GetMyDefault()
        {
            var accountId = GetAccountId();
            var result = await _svc.GetMyDefaultAsync(accountId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountAddressUpsertRequest req)
        {
            var accountId = GetAccountId();
            var result = await _svc.CreateMyAddressAsync(accountId, req);
            return Ok(result);
        }

        [HttpPut("{addressId:int}")]
        public async Task<IActionResult> Update(int addressId, [FromBody] AccountAddressUpsertRequest req)
        {
            var accountId = GetAccountId();
            var result = await _svc.UpdateMyAddressAsync(accountId, addressId, req);
            return Ok(result);
        }

        [HttpPut("{addressId:int}/default")]
        public async Task<IActionResult> SetDefault(int addressId)
        {
            var accountId = GetAccountId();
            var ok = await _svc.SetMyDefaultAsync(accountId, addressId);
            return Ok(new { success = ok });
        }

        [HttpDelete("{addressId:int}")]
        public async Task<IActionResult> Delete(int addressId)
        {
            var accountId = GetAccountId();
            var ok = await _svc.DeleteMyAddressAsync(accountId, addressId);
            return Ok(new { success = ok });
        }
    }
}
