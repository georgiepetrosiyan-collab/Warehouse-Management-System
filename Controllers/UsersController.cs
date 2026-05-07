using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Dtos;
using WarehouseAPI.Engine;
using WarehouseAPI.Models;

namespace WarehouseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly WarehouseEngine _engine;

        public UsersController(WarehouseEngine engine)
        {
            _engine = engine;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _engine.GetOrCreateUser(dto.Username, dto.Role);
            
            return Ok(new 
            { 
                user.Username, 
                user.Role,
                user.PurchaseFingerprint
            });
        }

        [HttpGet]
        public IActionResult GetAll() 
        {
            return Ok(_engine.UserRegistry.Select(u => new 
            {
                u.Username, 
                u.Role,
                u.PurchaseFingerprint
            }));
        }

        [HttpPost("{username}/buy")]
        public IActionResult Buy(string username, [FromBody] BuyDto dto)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null) 
                return NotFound(new { message = "User not found" });

            var bought = _engine.Buy(user, dto.ProductIds);
            
            return Ok(new { bought });
        }

        [HttpGet("{username}/history")]
        public IActionResult History(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null) 
                return NotFound(new { message = "User not found" });

            var history = new List<object>();
            var node = user.HistoryHead;
            
            while (node != null) 
            { 
                history.Add(new 
                { 
                    node.Data.Id,
                    node.Data.Name,
                    node.Data.Price,
                    node.Data.Quantity,
                    node.Data.Category
                }); 
                node = node.Next; 
            }
            
            return Ok(history);
        }

        [HttpGet("{username}/recommendations")]
        public IActionResult GetRecommendations(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var recommendations = _engine.GetHybridRecommendations(user);
            
            return Ok(recommendations.Select(p => new { p.Id, p.Name, p.Price, p.Category }));
        }

        [HttpGet("{username}/recommendations/collaborative")]
        public IActionResult GetCollaborativeRecommendations(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var recommendations = _engine.GetCollaborativeRecommendations(user, 20);
            
            return Ok(new
            {
                username = user.Username,
                recommendations = recommendations.Select(p => new { p.Id, p.Name, p.Price, p.Category }),
                totalRecommendations = recommendations.Count
            });
        }

        [HttpGet("{username}/recommendations/debug")]
        public IActionResult GetRecommendationsDebug(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var similarUsers = _engine.FindSimilarUsers(user, 5);
            var recommendations = _engine.GetCollaborativeRecommendations(user, 10);
            var binaryFingerprint = _engine.GetUserFingerprintBinary(user);
            
            return Ok(new
            {
                user = new
                {
                    user.Username,
                    user.Role,
                    user.Gender,
                    purchasedCount = _engine.GetPurchasedProductSet(user).Count,
                    binaryFingerprint = binaryFingerprint,
                    purchasedProducts = _engine.GetPurchasedProductNames(user)
                },
                similarUsers = similarUsers.Select(s => new
                {
                    s.User.Username,
                    s.User.Gender,
                    s.Distance,
                    SimilarityPercentage = $"{s.Similarity:P1}",
                    SimilarUserPurchases = _engine.GetPurchasedProductSet(s.User).Count,
                    SimilarUserFingerprint = _engine.GetUserFingerprintBinary(s.User)
                }),
                recommendations = recommendations.Select(p => new { p.Id, p.Name, p.Price, p.Category }),
                interpretation = similarUsers.Any() 
                    ? $"Found {similarUsers.Count} similar users. Recommendations based on products they purchased that you haven't."
                    : "No similar users found with purchase history. Try making some purchases first!"
            });
        }

        [HttpGet("similarity/hamming")]
        public IActionResult GetHammingDistance([FromQuery] string userA, [FromQuery] string userB)
        {
            var user1 = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(userA, StringComparison.OrdinalIgnoreCase));
            var user2 = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(userB, StringComparison.OrdinalIgnoreCase));
            
            if (user1 == null || user2 == null)
                return NotFound(new { message = "One or both users not found" });
            
            var distance = _engine.CalculateHammingDistance(user1.PurchaseFingerprint, user2.PurchaseFingerprint);
            var similarity = _engine.CalculateHammingSimilarity(user1.PurchaseFingerprint, user2.PurchaseFingerprint);
            
            string interpretation;
            if (distance == 0)
                interpretation = "Identical purchase patterns";
            else if (distance <= 5)
                interpretation = "Very similar purchase patterns";
            else if (distance <= 10)
                interpretation = "Moderately similar purchase patterns";
            else if (distance <= 15)
                interpretation = "Somewhat different purchase patterns";
            else
                interpretation = "Very different purchase patterns";
            
            var binaryA = _engine.GetUserFingerprintBinary(user1);
            var binaryB = _engine.GetUserFingerprintBinary(user2);
            
            return Ok(new
            {
                userA = user1.Username,
                userB = user2.Username,
                binaryFingerprintA = binaryA,
                binaryFingerprintB = binaryB,
                hammingDistance = distance,
                similarityScore = $"{similarity:P1}",
                formula = "d(x,y) = Σ |xᵢ - yᵢ|",
                maxPossible = user1.PurchaseFingerprint.Length,
                interpretation = interpretation
            });
        }

        [HttpGet("{username}/most-similar")]
        public IActionResult GetMostSimilarUser(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var mostSimilar = _engine.FindMostSimilarUserHamming(user);
            
            if (mostSimilar == null)
            {
                var hasPurchases = _engine.GetPurchasedProductSet(user).Any();
                return Ok(new 
                { 
                    currentUser = user.Username,
                    message = !hasPurchases 
                        ? "User has no purchase history. Make some purchases to get recommendations."
                        : "No similar users found with purchase history."
                });
            }
            
            var distance = _engine.CalculateHammingDistance(user.PurchaseFingerprint, mostSimilar.PurchaseFingerprint);
            var binaryCurrent = _engine.GetUserFingerprintBinary(user);
            var binarySimilar = _engine.GetUserFingerprintBinary(mostSimilar);
            
            return Ok(new
            {
                currentUser = user.Username,
                currentUserFingerprint = binaryCurrent,
                mostSimilarUser = mostSimilar.Username,
                mostSimilarUserFingerprint = binarySimilar,
                hammingDistance = distance,
                interpretation = distance == 0 ? "Identical purchase patterns" :
                                 distance <= 5 ? "Very similar" :
                                 distance <= 10 ? "Moderately similar" :
                                 distance <= 15 ? "Somewhat different" :
                                 "Very different",
                formula = "d(x,y) = Σ |xᵢ - yᵢ|"
            });
        }

        [HttpGet("{username}/fingerprint")]
        public IActionResult GetUserFingerprint(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null) 
                return NotFound(new { message = "User not found" });
            
            var purchasedSet = _engine.GetPurchasedProductSet(user);
            var products = _engine.MasterInventory
                .Where(p => purchasedSet.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, p.Price })
                .ToList();
            
            string binaryFingerprint = _engine.GetUserFingerprintBinary(user);
            
            return Ok(new
            {
                username = user.Username,
                role = user.Role,
                fingerprint = user.PurchaseFingerprint,
                binaryFingerprint = binaryFingerprint,
                purchasedProducts = products,
                setCardinality = purchasedSet.Count,
                interpretation = purchasedSet.Count == 0 ? "User has made no purchases yet (all zeros)" : $"User has purchased {purchasedSet.Count} product(s)",
                setNotation = $"P({user.Username}) = {{{string.Join(", ", products.Select(p => p.Name))}}}"
            });
        }
    }
}