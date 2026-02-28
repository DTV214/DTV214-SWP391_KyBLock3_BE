using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Common;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{

    [Route("api/configs")]
    [ApiController]
    public class ProductConfigsController(IProductConfigService service) : ControllerBase
    {
        private readonly IProductConfigService _service = service;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductConfigDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductConfigDto>>>> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<ProductConfigDto>>
            {
                Status = 200,
                Msg = "OK",
                Data = data
            });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProductConfigDto>>> Get(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound("Không tìm thấy cấu hình");
            return Ok(new ApiResponse<ProductConfigDto>
            {
                Status = 200,
                Msg = "OK",
                Data = data
            });
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductConfigDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductConfigDto>>>> Create([FromBody] CreateConfigRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var configs = await _service.CreateAsync(request);
            return Ok(new ApiResponse<IEnumerable<ProductConfigDto>>
            {
                Status = 200,
                Msg = "OK",
                Data = configs
            });
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductConfigDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductConfigDto>>>> Update(int id, [FromBody] UpdateConfigRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var configs = await _service.UpdateAsync(id, request);
                return Ok(new ApiResponse<IEnumerable<ProductConfigDto>>
                {
                    Status = 200,
                    Msg = "OK",
                    Data = configs
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductConfigDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductConfigDto>>>> Delete(int id)
        {
            var configs = await _service.DeleteAsync(id);
            return Ok(new ApiResponse<IEnumerable<ProductConfigDto>>
            {
                Status = 200,
                Msg = "OK",
                Data = configs
            });
        }
    }
}
