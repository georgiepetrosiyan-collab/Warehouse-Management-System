using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Dtos;
using WarehouseAPI.Engine;

namespace WarehouseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly WarehouseEngine _engine;
        public UsersController(WarehouseEngine engine) { _engine = engine; }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _engine.GetOrCreateUser(dto.Username, dto.Role);
            return Ok(new { user.Username, user.Role, user.PurchaseFingerprint });
        }

        [HttpPost("{username}/buy")]
        public IActionResult Buy(string username, [FromBody] BuyDto dto)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return NotFound();

            var bought = _engine.Buy(user, dto.ProductIds);
            return Ok(new { bought });
        }

        [HttpGet("{username}/history")]
        public IActionResult History(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return NotFound();

            var history = new List<object>();
            var node = user.HistoryHead;
            while (node != null) { history.Add(node.Data); node = node.Next; }
            return Ok(history);
        }

        [HttpGet]
        public IActionResult GetAll() => Ok(_engine.UserRegistry.Select(u => new {
            u.Username, u.Role, u.PurchaseFingerprint
        }));
    }
}