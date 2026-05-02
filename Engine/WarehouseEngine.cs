using System;
using System.Collections.Generic;
using System.Linq;
using WarehouseAPI.Models;

namespace WarehouseAPI.Engine
{

    public class WarehouseEngine
    {

        public List<Product> MasterInventory = new();


        public List<User> UserRegistry = new();

        private readonly Dictionary<Gender, List<string>> GenderPreferences = new()
        {
            { Gender.Male, new List<string> { 
                "Laptop", "Gaming Console", "Tools", "Electronics", 
                "Smartphone", "Watch", "Headphones", "Sports Equipment",
                "Beer", "Coffee", "Protein Powder", "Gym Bag"
            }},
            { Gender.Female, new List<string> { 
                "Handbag", "Jewelry", "Cosmetics", "Perfume", 
                "Dress", "Shoes", "Skincare", "Hair Care",
                "Chocolate", "Flowers", "Wine", "Yoga Mat"
            }},
            { Gender.Unspecified, new List<string> { 
                "Book", "Gift Card", "Water Bottle", "Notebook",
                "Pen", "Backpack", "Charger", "Umbrella"
            }}
        };


        private ProductBST productTree = new();

        private const int ID_PREFIX = 2026000;
        private const int MAX_SLOTS = 20;


        private PriorityQueue<Product, DateTime> ExpiryQueue = new();

        private void SortUserRegistry()
        {
            UserRegistry = UserRegistry
                .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        private User ExponentialSearchByName(string targetName)
        {
            if (UserRegistry.Count == 0) return null;

            SortUserRegistry();

            // Phase 1: Exponential range finding (bound doubles each iteration)
            int bound = 1;
            while (bound < UserRegistry.Count && 
                   string.Compare(UserRegistry[bound].Username, targetName, 
                                  StringComparison.OrdinalIgnoreCase) < 0)
            {
                bound *= 2;
            }

            // Phase 2: Binary search within identified range
            int left = bound / 2;
            int right = Math.Min(bound, UserRegistry.Count - 1);

            return BinarySearchByName(left, right, targetName);
        }


        private User BinarySearchByName(int left, int right, string target)
        {
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                int comparison = string.Compare(UserRegistry[mid].Username, target, 
                                                StringComparison.OrdinalIgnoreCase);
                
                if (comparison == 0)
                    return UserRegistry[mid];
                else if (comparison < 0)
                    left = mid + 1;
                else
                    right = mid - 1;
            }
            return null;
        }


        public HashSet<int> GetPurchasedProductSet(User user)
        {
            var purchased = new HashSet<int>();
            int maxLength = Math.Min(user.PurchaseFingerprint.Length, MasterInventory.Count);
            
            for (int i = 0; i < maxLength; i++)
            {
                if (user.PurchaseFingerprint[i] == 1)
                {
                    purchased.Add(MasterInventory[i].Id);
                }
            }
            return purchased;
        }


        public double CalculateMeanPrice()
        {
            if (MasterInventory.Count == 0) return 0;
            
            double sum = 0;
            foreach (var product in MasterInventory)
            {
                sum += product.Price;
            }
            return sum / MasterInventory.Count;
        }

        public double CalculateMeanSales()
        {
            if (MasterInventory.Count == 0) return 0;
            
            double sum = 0;
            foreach (var product in MasterInventory)
            {
                sum += product.TotalSold;
            }
            return sum / MasterInventory.Count;
        }

        public Product FindModeMostPurchased()
        {
            if (MasterInventory.Count == 0) return null;
            
            Product mode = MasterInventory[0];
            int maxFrequency = mode.TotalSold;
            
            foreach (var product in MasterInventory)
            {
                if (product.TotalSold > maxFrequency)
                {
                    maxFrequency = product.TotalSold;
                    mode = product;
                }
            }
            
            return mode;
        }


        public List<Product> FindAllModesMostPurchased()
        {
            if (MasterInventory.Count == 0) return new List<Product>();
            
            int maxFrequency = MasterInventory.Max(p => p.TotalSold);
            return MasterInventory.Where(p => p.TotalSold == maxFrequency).ToList();
        }

 
        public List<Product> GetGenderBasedRecommendations(User user)
        {
            if (user.Gender == Gender.Unspecified)
                return new List<Product>();

            var genderPrefs = GenderPreferences[user.Gender];
            var purchasedSet = GetPurchasedProductSet(user);
            var recommendations = new List<Product>();

            foreach (var product in MasterInventory)
            {
                // Check if product matches gender preference
                bool matchesGender = genderPrefs.Any(pref => 
                    product.Name.Contains(pref, StringComparison.OrdinalIgnoreCase));
                
                // Check if user hasn't purchased it yet
                bool notPurchased = !purchasedSet.Contains(product.Id);
                
                // Check if in stock
                bool inStock = product.Quantity > 0;

                if (matchesGender && notPurchased && inStock)
                {
                    recommendations.Add(product);
                }
            }

            return recommendations.Take(5).ToList(); // Limit to top 5
        }

        public Dictionary<string, int> GetGenderPopularityScores(Gender gender)
        {
            var scores = new Dictionary<string, int>();
            var genderPrefs = GenderPreferences[gender];

            foreach (var product in MasterInventory)
            {
                bool matchesGender = genderPrefs.Any(pref => 
                    product.Name.Contains(pref, StringComparison.OrdinalIgnoreCase));
                
                if (matchesGender)
                {
                    scores[product.Name] = product.TotalSold;
                }
            }

            return scores.OrderByDescending(x => x.Value).ToDictionary();
        }

   
        public User GetOrCreateUser(string name, UserRole role, Gender gender = Gender.Unspecified)
        {
            User existingUser = ExponentialSearchByName(name);
            
            if (existingUser != null)
                return existingUser;

            var newUser = new User(name, role, MAX_SLOTS)
            {
                Gender = gender
            };
            UserRegistry.Add(newUser);
            SortUserRegistry();
            
            return newUser;
        }

  
        public void UpdateUserGender(string username, Gender gender)
        {
            var user = ExponentialSearchByName(username);
            if (user != null)
            {
                user.Gender = gender;
            }
        }



        public Product AddProduct(string name, double price, int quantity,
                                  Category category, DateTime? expiry)
        {
            var existing = MasterInventory.Find(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.AuditTrail.Push($"Restocked +{quantity} on {DateTime.Now}");
                return existing;
            }

            var product = new Product
            {
                Id         = ID_PREFIX + MasterInventory.Count + 1,
                BitIndex   = MasterInventory.Count,
                Name       = name,
                Price      = price,
                Quantity   = quantity,
                TotalSold  = 0,
                Category   = category,
                ExpiryDate = expiry,
                DateAdded  = DateTime.Now,
                AuditTrail = new Stack<string>()
            };

            product.AuditTrail.Push($"Created on {DateTime.Now} with quantity {quantity}");
            MasterInventory.Add(product);
            productTree.Insert(product);

            if (category == Category.Food && expiry.HasValue)
                ExpiryQueue.Enqueue(product, expiry.Value);

            return product;
        }

        public bool DeleteProduct(int id)
        {
            var product = productTree.Search(id);
            if (product == null) return false;
            productTree.Delete(id);
            MasterInventory.Remove(product);
            return true;
        }



        public List<string> Buy(User buyer, List<int> productIds)
        {
            var bought = new List<string>();
            foreach (var id in productIds)
            {
                var p = MasterInventory.Find(x => x.Id == id && x.Quantity > 0);
                if (p == null) continue;

                p.Quantity--;
                p.TotalSold++;
                buyer.PurchaseFingerprint[p.BitIndex] = 1;

                p.AuditTrail.Push($"Purchased by {buyer.Username} on {DateTime.Now}");

                var node = new PurchaseNode(p) { Next = buyer.HistoryHead };
                buyer.HistoryHead = node;
                bought.Add(p.Name);
            }
            return bought;
        }

        public int CalculateHammingDistance(int[] fingerprintA, int[] fingerprintB)
        {
            if (fingerprintA.Length != fingerprintB.Length)
                return int.MaxValue;

            int distance = 0;
            for (int i = 0; i < fingerprintA.Length; i++)
            {
                if (fingerprintA[i] != fingerprintB[i])
                    distance++;
            }
            return distance;
        }



        private bool HasPurchases(User user)
        {
            for (int i = 0; i < user.PurchaseFingerprint.Length && i < MasterInventory.Count; i++)
            {
                if (user.PurchaseFingerprint[i] == 1)
                    return true;
            }
            return false;
        }


        public User FindMostSimilarUserHamming(User currentUser)
        {
            if (!HasPurchases(currentUser))
                return null;

            User mostSimilar = null;
            int smallestDistance = int.MaxValue;

            foreach (var user in UserRegistry)
            {
                if (user.Username == currentUser.Username || user.Role != UserRole.Buyer)
                    continue;

                if (!HasPurchases(user)) continue;

                int distance = CalculateHammingDistance(currentUser.PurchaseFingerprint, user.PurchaseFingerprint);
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    mostSimilar = user;
                }
            }

            return mostSimilar;
        }


        public List<Product> GetCollaborativeRecommendations(User user)
        {
            if (!HasPurchases(user))
                return new List<Product>();

            var similarUser = FindMostSimilarUserHamming(user);
            if (similarUser == null) return new List<Product>();

            var recommendations = new List<Product>();
            
            for (int i = 0; i < similarUser.PurchaseFingerprint.Length && i < MasterInventory.Count; i++)
            {
                if (similarUser.PurchaseFingerprint[i] == 1 && 
                    user.PurchaseFingerprint[i] == 0)
                {
                    var product = MasterInventory[i];
                    if (product.Quantity > 0)
                        recommendations.Add(product);
                }
            }

            return recommendations;
        }


        public List<Product> GetHybridRecommendations(User user, int maxRecommendations = 5)
        {
            var recommendations = new List<Product>();
            var addedIds = new HashSet<int>();

            // STEP 1: Gender-based recommendations (highest priority)
            var genderRecs = GetGenderBasedRecommendations(user);
            foreach (var product in genderRecs)
            {
                if (!addedIds.Contains(product.Id))
                {
                    recommendations.Add(product);
                    addedIds.Add(product.Id);
                }
            }

            // STEP 2: Collaborative filtering recommendations
            var collabRecs = GetCollaborativeRecommendations(user);
            foreach (var product in collabRecs)
            {
                if (!addedIds.Contains(product.Id))
                {
                    recommendations.Add(product);
                    addedIds.Add(product.Id);
                }
            }

            // STEP 3: Fallback to popular products if needed
            if (recommendations.Count < maxRecommendations)
            {
                var popularProducts = MasterInventory
                    .Where(p => p.Quantity > 0 && !addedIds.Contains(p.Id))
                    .OrderByDescending(p => p.TotalSold)
                    .Take(maxRecommendations - recommendations.Count)
                    .ToList();
                
                recommendations.AddRange(popularProducts);
            }

            return recommendations.Take(maxRecommendations).ToList();
        }


        public List<Product> GetRecommendations(User user)
        {
            return GetCollaborativeRecommendations(user);
        }


        public void SortByPrice()
        {
            QuickSort(0, MasterInventory.Count - 1);
        }

        private void QuickSort(int low, int high)
        {
            if (low >= high) return;

            int p = Partition(low, high);
            QuickSort(low, p - 1);
            QuickSort(p + 1, high);
        }

        private int Partition(int low, int high)
        {
            double pivot = MasterInventory[high].Price;
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                if (MasterInventory[j].Price < pivot)
                {
                    i++;
                    (MasterInventory[i], MasterInventory[j]) = (MasterInventory[j], MasterInventory[i]);
                }
            }

            (MasterInventory[i + 1], MasterInventory[high]) = (MasterInventory[high], MasterInventory[i + 1]);
            return i + 1;
        }
        public (string mostDemanded, string leastDemanded, double avgPrice, int totalSold) GetStatistics()
        {
            if (MasterInventory.Count == 0)
                return (null, null, 0, 0);

            var modeProduct = FindModeMostPurchased();
            var min = MasterInventory.OrderBy(p => p.TotalSold).First();
            var avg = CalculateMeanPrice();
            var total = MasterInventory.Sum(p => p.TotalSold);

            return (modeProduct?.Name, min.Name, avg, total);
        }

        public void ShowStats()
        {
            if (MasterInventory.Count == 0)
            {
                Console.WriteLine("No products.");
                return;
            }

            var stats = GetStatistics();
            var meanSales = CalculateMeanSales();
            var modeProduct = FindModeMostPurchased();
            
            Console.WriteLine($"=== WAREHOUSE STATISTICS ===");
            Console.WriteLine($"Most demanded (Mode): {stats.mostDemanded}");
            Console.WriteLine($"Least demanded: {stats.leastDemanded}");
            Console.WriteLine($"Average price (Mean): ${stats.avgPrice:F2}");
            Console.WriteLine($"Average sales per product: {meanSales:F2} units");
            Console.WriteLine($"Total sold: {stats.totalSold} units");
        }


        public List<Product> GetExpiringSoon()
        {
            var expiring = new List<Product>();
            var sevenDaysLater = DateTime.Now.AddDays(7);
    
            foreach (var product in MasterInventory)
            {
                if (product.Category == Category.Food && 
                    product.ExpiryDate.HasValue &&
                    product.ExpiryDate.Value <= sevenDaysLater)
                {
                    expiring.Add(product);
                }
            }
            return expiring.OrderBy(p => p.ExpiryDate).ToList();
        }
    }
}