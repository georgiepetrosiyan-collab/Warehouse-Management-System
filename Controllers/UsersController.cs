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
            var user = _engine.GetOrCreateUser(dto.Username, dto.Role, dto.Gender);
            
            return Ok(new 
            { 
                user.Username, 
                user.Role, 
                user.Gender,
                user.PurchaseFingerprint     // Binary vector: [0,0,0,...] for new users
            });
        }


        [HttpPut("{username}/gender")]
        public IActionResult UpdateGender(string username, [FromQuery] Gender gender)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            _engine.UpdateUserGender(username, gender);
            
            return Ok(new { message = $"Gender updated to {gender}", username, gender });
        }

        [HttpGet]
        public IActionResult GetAll() 
        {
            return Ok(_engine.UserRegistry.Select(u => new 
            {
                u.Username, 
                u.Role, 
                u.Gender,
                u.PurchaseFingerprint      // Binary vector representation
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
            
            // After purchase, user's fingerprint bit at product.BitIndex becomes 1
            
            return Ok(new { bought });
        }

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

        public IActionResult GetRecommendations(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var recommendations = _engine.GetHybridRecommendations(user);
            
            return Ok(recommendations);
        }

        public IActionResult GetGenderRecommendations(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var recommendations = _engine.GetGenderBasedRecommendations(user);
            
            return Ok(recommendations);
        }

        [HttpGet("{username}/recommendations/collaborative")]
        public IActionResult GetCollaborativeRecommendations(string username)
        {
            var user = _engine.UserRegistry.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var recommendations = _engine.GetCollaborativeRecommendations(user);
            
            return Ok(recommendations);
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
            
            // Convert fingerprint to binary string for readability
            string binaryFingerprint = string.Join("", user.PurchaseFingerprint.Take(_engine.MasterInventory.Count));
            
            return Ok(new
            {
                username = user.Username,
                role = user.Role,
                gender = user.Gender,
                fingerprint = user.PurchaseFingerprint,
                binaryFingerprint = binaryFingerprint,
                purchasedProducts = products,
                setCardinality = purchasedSet.Count,
                interpretation = purchasedSet.Count == 0 ? "User has made no purchases yet (all zeros)" : $"User has purchased {purchasedSet.Count} product(s)",
                setNotation = $"P({user.Username}) = {{{string.Join(", ", products.Select(p => p.Name))}}}"
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
            
            // Generate interpretation based on distance value
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
            
            // Get binary string representations for visualization
            string binaryA = string.Join("", user1.PurchaseFingerprint.Take(_engine.MasterInventory.Count));
            string binaryB = string.Join("", user2.PurchaseFingerprint.Take(_engine.MasterInventory.Count));
            
            return Ok(new
            {
                userA = user1.Username,
                userB = user2.Username,
                genderA = user1.Gender,
                genderB = user2.Gender,
                fingerprintA = user1.PurchaseFingerprint,
                fingerprintB = user2.PurchaseFingerprint,
                binaryFingerprintA = binaryA,
                binaryFingerprintB = binaryB,
                hammingDistance = distance,
                formula = "d(x,y) = Σ |xᵢ - yᵢ|",
                maxPossible = user1.PurchaseFingerprint.Length,
                interpretation = interpretation,
                visualComparison = GenerateHammingVisualComparison(binaryA, binaryB, distance)
            });
        }

        private string GenerateHammingVisualComparison(string binaryA, string binaryB, int distance)
        {
            var result = new List<string>();
            result.Add($"User A: {binaryA}");
            result.Add($"User B: {binaryB}");
            result.Add($"Different: {distance} positions differ");
            result.Add($"Formula: d(A,B) = Σ |Aᵢ - Bᵢ| = {distance}");
            return string.Join("\n", result);
        }

        [HttpGet("statistics/gender-popularity")]
        public IActionResult GetGenderPopularity([FromQuery] Gender gender)
        {
            var scores = _engine.GetGenderPopularityScores(gender);
            
            return Ok(new
            {
                gender = gender,
                topProducts = scores.Take(10).Select(kv => new { product = kv.Key, sales = kv.Value }),
                totalProducts = scores.Count,
                formula = "Popularity(p) = Σ purchases of product p by users of this gender",
                note = gender == Gender.Unspecified ? "Gender not specified - limited recommendations" : "Gender-based recommendations available"
            });
        }

        [HttpGet("statistics/gender-summary")]
        public IActionResult GetGenderSummary()
        {
            var maleUsers = _engine.UserRegistry.Count(u => u.Gender == Gender.Male && u.Role == UserRole.Buyer);
            var femaleUsers = _engine.UserRegistry.Count(u => u.Gender == Gender.Female && u.Role == UserRole.Buyer);
            var unspecifiedUsers = _engine.UserRegistry.Count(u => u.Gender == Gender.Unspecified && u.Role == UserRole.Buyer);
            var totalBuyers = maleUsers + femaleUsers + unspecifiedUsers;
            
            return Ok(new
            {
                maleCount = maleUsers,
                femaleCount = femaleUsers,
                unspecifiedCount = unspecifiedUsers,
                totalBuyers = totalBuyers,
                malePercentage = totalBuyers > 0 ? Math.Round((double)maleUsers / totalBuyers * 100, 1) : 0,
                femalePercentage = totalBuyers > 0 ? Math.Round((double)femaleUsers / totalBuyers * 100, 1) : 0,
                recommendationNote = "Gender-based recommendations available for Male and Female users",
                hammingDistanceNote = "Hamming distance available for all users regardless of gender"
            });
        }

        // ====================================================================
        // SECTION 6: MOST SIMILAR USER (HAMMING DISTANCE)
        // ====================================================================

        /// <summary>
        /// Finds the most similar user based on Hamming distance.
        /// Uses binary fingerprint comparison to find user with smallest distance.
        /// 
        /// ALGORITHM:
        ///   1. Calculate Hamming distance between current user and all other users
        ///   2. Select user with smallest distance (most similar purchase pattern)
        ///   3. Return the user and the distance value
        /// 
        /// GET: api/users/{username}/most-similar
        /// </summary>
        /// <param name="username">User to find similar users for</param>
        /// <returns>Most similar user and Hamming distance</returns>
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
                return Ok(new 
                { 
                    currentUser = user.Username,
                    message = user.PurchaseFingerprint.All(b => b == 0) 
                        ? "User has no purchase history. Make some purchases to get recommendations."
                        : "No similar users found with purchase history."
                });
            }
            
            var distance = _engine.CalculateHammingDistance(user.PurchaseFingerprint, mostSimilar.PurchaseFingerprint);
            
            string binaryCurrent = string.Join("", user.PurchaseFingerprint.Take(_engine.MasterInventory.Count));
            string binarySimilar = string.Join("", mostSimilar.PurchaseFingerprint.Take(_engine.MasterInventory.Count));
            
            return Ok(new
            {
                currentUser = user.Username,
                currentUserGender = user.Gender,
                currentUserFingerprint = binaryCurrent,
                mostSimilarUser = mostSimilar.Username,
                mostSimilarUserGender = mostSimilar.Gender,
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
    }
}