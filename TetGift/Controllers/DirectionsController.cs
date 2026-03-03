using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/directions")]
    public class DirectionsController : BaseApiController
    {
        private readonly IDirectionsService _svc;

        public DirectionsController(IDirectionsService svc)
        {
            _svc = svc;
        }

        [HttpPost("to-store/{storeLocationId:int}")]
        public async Task<IActionResult> ToStore(int storeLocationId, [FromBody] DirectionsRequest req)
        {
            // accountId không bắt buộc cho chức năng này, nhưng nếu bạn muốn log thì lấy được:
            // var accountId = GetAccountId();

            var result = await _svc.BuildDirectionsUrlAsync(storeLocationId, req);
            return Ok(result);
        }
    }
}
