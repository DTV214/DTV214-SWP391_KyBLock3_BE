using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/store-locations")]
    public class StoreLocationsController : BaseApiController
    {
        private readonly IStoreLocationService _svc;

        public StoreLocationsController(IStoreLocationService svc)
        {
            _svc = svc;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var result = await _svc.GetActiveAsync();
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _svc.GetByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _svc.GetAllAsync();
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreLocationUpsertRequest req)
        {
            var result = await _svc.CreateAsync(req);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StoreLocationUpsertRequest req)
        {
            var result = await _svc.UpdateAsync(id, req);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return Ok(new { success = ok });
        }
    }
}
