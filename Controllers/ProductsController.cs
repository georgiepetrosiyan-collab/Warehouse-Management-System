//ProductController.cs

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

        [HttpGet("recommendations/{username}")]
        public IActionResult GetRecommendations(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u => 
                u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
            
            if (user == null) 
                return NotFound(new { message = "User not found" });
            
            var recommendations = _engine.GetRecommendations(user);
            return Ok(recommendations);
        }


        [HttpGet("statistics/mean-price")]
        public IActionResult GetMeanPrice()
        {
            var mean = _engine.CalculateMeanPrice();
            return Ok(new { mean, formula = "x̄ = (1/n) Σ xᵢ", count = _engine.MasterInventory.Count });
        }

  
        [HttpGet("statistics/mode")]
        public IActionResult GetModeMostPurchased()
        {
            var mode = _engine.FindModeMostPurchased();
            if (mode == null) return Ok(new { message = "No products found" });
            return Ok(new { product = mode.Name, totalSold = mode.TotalSold });
        }

        [HttpGet("statistics/mean-sales")]
        public IActionResult GetMeanSales()
        {
            var mean = _engine.CalculateMeanSales();
            return Ok(new { meanSales = mean, formula = "x̄ = (1/n) Σ sᵢ" });
        }


        [HttpGet("statistics/comprehensive")]
        public IActionResult GetComprehensiveStatistics()
        {
            var stats = _engine.GetStatistics();
            var meanPrice = _engine.CalculateMeanPrice();
            var meanSales = _engine.CalculateMeanSales();
            var mode = _engine.FindModeMostPurchased();
            var allModes = _engine.FindAllModesMostPurchased();
            
            return Ok(new
            {
                mostDemanded = stats.mostDemanded,
                leastDemanded = stats.leastDemanded,
                averagePrice = stats.avgPrice,
                totalSold = stats.totalSold,
                meanPrice = meanPrice,
                meanSales = meanSales,
                modeProduct = mode?.Name,
                modeSales = mode?.TotalSold,
                allModes = allModes.Select(p => new { p.Name, p.TotalSold }),
                productCount = _engine.MasterInventory.Count
            });
        }


        [HttpGet("audit/{productId}")]
        public IActionResult GetAuditTrail(int productId)
        {
            var product = _engine.MasterInventory.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();
            
            var auditList = product.AuditTrail.ToList();
            return Ok(new { productId, productName = product.Name, auditTrail = auditList });
        }
    }
}