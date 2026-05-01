using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Dtos;
using WarehouseAPI.Engine;

namespace WarehouseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly WarehouseEngine _engine;
        public ProductsController(WarehouseEngine engine) { _engine = engine; }

        [HttpGet]
        public IActionResult GetAll() => Ok(_engine.MasterInventory);

        [HttpPost]
        public IActionResult Add([FromBody] AddProductDto dto)
        {
            var p = _engine.AddProduct(dto.Name, dto.Price, dto.Quantity,
                                       dto.Category, dto.ExpiryDate);
            return Ok(p);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id) =>
            _engine.DeleteProduct(id) ? Ok() : NotFound();

        [HttpGet("stats")]
        public IActionResult Stats()
        {
            var inv = _engine.MasterInventory;
            if (inv.Count == 0) return Ok(null);
            var sorted = inv.OrderByDescending(p => p.TotalSold).ToList();
            return Ok(new {
                mostDemanded  = sorted.First().Name,
                leastDemanded = sorted.Last().Name,
                avgPrice      = inv.Average(p => p.Price),
                totalSold     = inv.Sum(p => p.TotalSold)
            });
        }
    }
}