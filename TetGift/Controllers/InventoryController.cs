using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos.TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // GET api/inventory/low-stock?threshold=5
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 10)
        {
            var result = await _inventoryService.GetLowStockReportAsync(threshold);
            return Ok(result);
        }
        [HttpGet("stocks")]
        public async Task<IActionResult> GetAllStocks()
        {
            var result = await _inventoryService.GetAllStocksAsync();
            return Ok(result);
        }

        [HttpGet("stocks/{id}")]
        public async Task<IActionResult> GetStockById(int id)
        {
            var result = await _inventoryService.GetStockByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("stocks")]
        public async Task<IActionResult> CreateStock([FromBody] CreateStockRequest req)
        {
            var result = await _inventoryService.CreateStockAsync(req);
            return Ok(result);
        }

        [HttpPut("stocks/{id}")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest req)
        {
            await _inventoryService.UpdateStockAsync(id, req);
            return Ok(new { message = "Cập nhật lô hàng thành công." });
        }
        // --- API MỚI: LẤY STOCK CỦA 1 SẢN PHẨM ---
        [HttpGet("products/{productId}/stocks")]
        public async Task<IActionResult> GetStocksByProductId(int productId)
        {
            var result = await _inventoryService.GetStocksByProductIdAsync(productId);
            return Ok(result);
        }

        [HttpDelete("stocks/{id}")]
        public async Task<IActionResult> DeleteStock(int id)
        {
            await _inventoryService.DeleteStockAsync(id);
            return Ok(new { message = "Xóa lô hàng thành công." });
        }
    }
}