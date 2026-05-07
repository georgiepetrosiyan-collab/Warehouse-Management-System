using System;
using System.Collections.Generic;
using System.Linq;
using WarehouseAPI.Models;

namespace WarehouseAPI.Engine
{

    public class WarehouseEngine
    {

        // ─────────────────────────────────────────────────────────────────────
        public List<Product> MasterInventory = new();

        public List<User> UserRegistry = new();

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

            int bound = 1;
            while (bound < UserRegistry.Count && 
                   string.Compare(UserRegistry[bound].Username, targetName, 
                                  StringComparison.OrdinalIgnoreCase) < 0)
            {
                bound *= 2;
            }

            int left = bound / 2;
            int right = Math.Min(bound, UserRegistry.Count - 1);

            // Delegates to another private helper — internal collaboration
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


        public User GetOrCreateUser(string name, UserRole role)
        {
            // OOP: Using a private method internally — Encapsulation
            User existingUser = ExponentialSearchByName(name);
            if (existingUser != null)
                return existingUser;

            // OOP: Object Instantiation — creating a new User MODEL OBJECT
            var newUser = new User(name, role, MAX_SLOTS);
            UserRegistry.Add(newUser);
            SortUserRegistry();
            return newUser;
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
                
                if (category == Category.Food && expiry.HasValue)
                {
                    
                    ExpiryQueue.Enqueue(existing, expiry.Value);
                }
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
            {
              
                ExpiryQueue.Enqueue(product, expiry.Value);
            }

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

 
        public double CalculateHammingSimilarity(int[] fingerprintA, int[] fingerprintB)
        {
            if (fingerprintA.Length != fingerprintB.Length)
                return 0;
            
            int distance = CalculateHammingDistance(fingerprintA, fingerprintB);
            return 1.0 - (distance / (double)fingerprintA.Length);
        }


        public string GetUserFingerprintBinary(User user)
        {
            int maxLength = Math.Min(user.PurchaseFingerprint.Length, MasterInventory.Count);
            return string.Join("", user.PurchaseFingerprint.Take(maxLength).Select(b => b == 1 ? "1" : "0"));
        }


        public List<string> GetPurchasedProductNames(User user)
        {
            var purchasedNames = new List<string>();
            int maxLength = Math.Min(user.PurchaseFingerprint.Length, MasterInventory.Count);
            
            for (int i = 0; i < maxLength; i++)
            {
                if (user.PurchaseFingerprint[i] == 1)
                {
                    purchasedNames.Add(MasterInventory[i].Name);
                }
            }
            return purchasedNames;
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

        public List<(User User, int Distance, double Similarity)> FindSimilarUsers(User currentUser, int maxUsers = 10, double minSimilarity = 0.2)
        {
            if (!HasPurchases(currentUser))
                return new List<(User, int, double)>();

            var similarUsers = new List<(User User, int Distance, double Similarity)>();
            
            foreach (var user in UserRegistry)
            {

                if (user.Username == currentUser.Username || user.Role != UserRole.Buyer)
                    continue;

                if (!HasPurchases(user)) continue;

                int distance = CalculateHammingDistance(currentUser.PurchaseFingerprint, user.PurchaseFingerprint);
                double similarity = CalculateHammingSimilarity(currentUser.PurchaseFingerprint, user.PurchaseFingerprint);
                
                if (similarity >= minSimilarity)
                {
                    similarUsers.Add((user, distance, similarity));
                }
            }
            
            return similarUsers
                .OrderBy(x => x.Distance)  
                .Take(maxUsers)
                .ToList();
        }

        public List<Product> GetCollaborativeRecommendations(User user, int maxRecommendations = 20)
        {
            if (!HasPurchases(user))
                return new List<Product>();

            
            var similarUsers = FindSimilarUsers(user, maxUsers: 10, minSimilarity: 0.2);
            if (!similarUsers.Any()) 
                return new List<Product>();

            var candidateProducts = new Dictionary<int, double>(); // ProductId -> weighted score
            var userPurchased = GetPurchasedProductSet(user);
            
            foreach (var (similarUser, distance, similarity) in similarUsers)
            {
                
                if (distance > user.PurchaseFingerprint.Length / 2)
                    continue;
                
                
                double weight = similarity;
                
                
                int maxLength = Math.Min(similarUser.PurchaseFingerprint.Length, MasterInventory.Count);
                for (int i = 0; i < maxLength; i++)
                {
                    if (similarUser.PurchaseFingerprint[i] == 1 && 
                        user.PurchaseFingerprint[i] == 0 && 
                        i < MasterInventory.Count)
                    {
                        var product = MasterInventory[i];
                        if (product.Quantity > 0) 
                        {
                            if (!candidateProducts.ContainsKey(product.Id))
                                candidateProducts[product.Id] = 0;
                            
                            candidateProducts[product.Id] += weight;
                        }
                    }
                }
            }
            
            
            var recommendedProductIds = candidateProducts
                .OrderByDescending(x => x.Value)
                .Take(maxRecommendations)
                .Select(x => x.Key)
                .ToList();
            
            return MasterInventory
                .Where(p => recommendedProductIds.Contains(p.Id))
                .OrderByDescending(p => candidateProducts[p.Id])
                .ToList();
        }

        
        public (List<Product> Recommendations, Dictionary<string, object> Metadata) 
            GetCollaborativeRecommendationsWithMetadata(User user, int maxRecommendations = 5)
        {
            var similarUsers = FindSimilarUsers(user, maxUsers: 5);
            var recommendations = GetCollaborativeRecommendations(user, maxRecommendations);
            
            var metadata = new Dictionary<string, object>
            {
                ["targetUser"] = user.Username,
                ["targetPurchasedCount"] = GetPurchasedProductSet(user).Count,
                ["targetFingerprint"] = GetUserFingerprintBinary(user),
                ["similarUsersFound"] = similarUsers.Count,
                ["similarUsers"] = similarUsers.Select(s => new
                {
                    Username = s.User.Username,
                    HammingDistance = s.Distance,
                    SimilarityScore = $"{s.Similarity:P1}", // Percentage format
                    PurchasedCount = GetPurchasedProductSet(s.User).Count,
                    Fingerprint = GetUserFingerprintBinary(s.User)
                }).ToList(),
                ["algorithm"] = "Hamming Distance Collaborative Filtering",
                ["formula"] = "d(x,y) = Σ |xᵢ - yᵢ|, similarity = 1 - d/n",
                ["recommendationCount"] = recommendations.Count
            };
            
            return (recommendations, metadata);
        }

        public double CalculateJaccardSimilarity(User userA, User userB)
        {
            var purchasesA = GetPurchasedProductSet(userA);
            var purchasesB = GetPurchasedProductSet(userB);
            
            if (purchasesA.Count == 0 && purchasesB.Count == 0)
                return 1.0; 
            
            if (purchasesA.Count == 0 || purchasesB.Count == 0)
                return 0.0; 
            
            var intersection = purchasesA.Intersect(purchasesB).Count();
            var union = purchasesA.Union(purchasesB).Count();
            
            return union == 0 ? 0 : (double)intersection / union;
        }


        public List<Product> GetDemographicRecommendations(User user, int maxRecommendations = 5)
        {
            if (!HasPurchases(user) || user.Gender == Gender.Unspecified)
                return new List<Product>();
            

            var similarGenderUsers = UserRegistry
                .Where(u => u.Username != user.Username && 
                            u.Role == UserRole.Buyer && 
                            u.Gender == user.Gender &&
                            HasPurchases(u))
                .ToList();
            
            if (!similarGenderUsers.Any())
                return new List<Product>();
            
            var candidateProducts = new Dictionary<int, int>(); 
            var userPurchased = GetPurchasedProductSet(user);
            
            foreach (var genderUser in similarGenderUsers)
            {
                int maxLength = Math.Min(genderUser.PurchaseFingerprint.Length, MasterInventory.Count);
                for (int i = 0; i < maxLength; i++)
                {
                    if (genderUser.PurchaseFingerprint[i] == 1 && 
                        !userPurchased.Contains(MasterInventory[i].Id))
                    {
                        var productId = MasterInventory[i].Id;
                        if (!candidateProducts.ContainsKey(productId))
                            candidateProducts[productId] = 0;
                        candidateProducts[productId]++;
                    }
                }
            }
            
            var recommendedProductIds = candidateProducts
                .OrderByDescending(x => x.Value)
                .Take(maxRecommendations)
                .Select(x => x.Key)
                .ToList();
            
            return MasterInventory
                .Where(p => recommendedProductIds.Contains(p.Id))
                .ToList();
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


        public List<Product> GetHybridRecommendations(User user, int maxRecommendations = 5)
        {
            var recommendations = new List<Product>();
            var addedIds = new HashSet<int>();

            
            var collabRecs = GetCollaborativeRecommendations(user, maxRecommendations * 2);
            foreach (var product in collabRecs)
            {
                if (!addedIds.Contains(product.Id))
                {
                    recommendations.Add(product);
                    addedIds.Add(product.Id);
                }
                if (recommendations.Count >= maxRecommendations)
                    break;
            }

   
            if (recommendations.Count < maxRecommendations && user.Gender != Gender.Unspecified)
            {
                var demographicRecs = GetDemographicRecommendations(user, maxRecommendations - recommendations.Count);
                foreach (var product in demographicRecs)
                {
                    if (!addedIds.Contains(product.Id))
                    {
                        recommendations.Add(product);
                        addedIds.Add(product.Id);
                    }
                    if (recommendations.Count >= maxRecommendations)
                        break;
                }
            }


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

 
        public List<Product> GetExpiredProducts()
        {
            var expired = new List<Product>();
            var today = DateTime.Now.Date;
            
            while (ExpiryQueue.Count > 0)
            {

                if (ExpiryQueue.TryPeek(out Product earliestProduct, out DateTime earliestExpiry))
                {
                    if (earliestExpiry.Date < today)
                    {
                        ExpiryQueue.Dequeue();
                        

                        if (MasterInventory.Contains(earliestProduct))
                        {
                            expired.Add(earliestProduct);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            
            return expired;
        }
        

        public List<Product> GetExpiringWithinDays(int days = 7)
        {
            var expiring = new List<Product>();
            var thresholdDate = DateTime.Now.Date.AddDays(days);
            
            foreach (var product in MasterInventory)
            {

                if (product.Category == Category.Food && 
                    product.ExpiryDate.HasValue &&
                    product.ExpiryDate.Value.Date <= thresholdDate &&
                    product.ExpiryDate.Value.Date >= DateTime.Now.Date)
                {
                    expiring.Add(product);
                }
            }
            
            return expiring.OrderBy(p => p.ExpiryDate).ToList();
        }
        

        public List<Product> GetExpiringSoon()
        {
            return GetExpiringWithinDays(7);
        }
        

        public Product GetNextExpiringProduct()
        {
            if (ExpiryQueue.Count == 0) return null;
            
            while (ExpiryQueue.Count > 0)
            {
                // OOP: COMPOSITION — TryPeek called on the composed PriorityQueue
                if (ExpiryQueue.TryPeek(out Product product, out _))
                {
                    if (MasterInventory.Contains(product))
                    {
                        return product;
                    }
                    else
                    {
                        ExpiryQueue.Dequeue();
                    }
                }
            }
            return null;
        }
        

        public int GetExpiryQueueCount()
        {
            return ExpiryQueue.Count;
        }
        

        public void RebuildExpiryQueue()
        {

            ExpiryQueue.Clear();
            foreach (var product in MasterInventory)
            {


                if (product.Category == Category.Food && product.ExpiryDate.HasValue)
                {
                    ExpiryQueue.Enqueue(product, product.ExpiryDate.Value);
                }
            }
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
            
            Console.WriteLine($"=== WAREHOUSE STATISTICS ===");
            Console.WriteLine($"Most demanded (Mode): {stats.mostDemanded}");
            Console.WriteLine($"Least demanded: {stats.leastDemanded}");
            Console.WriteLine($"Average price (Mean): ${stats.avgPrice:F2}");
            Console.WriteLine($"Average sales per product: {meanSales:F2} units");
            Console.WriteLine($"Total sold: {stats.totalSold} units");
            

            Console.WriteLine($"\n=== EXPIRY QUEUE (PriorityQueue) ===");
            Console.WriteLine($"Products in expiry queue: {ExpiryQueue.Count}");


            var nextExpiring = GetNextExpiringProduct();
            if (nextExpiring != null)
            {

                Console.WriteLine($"Next expiring: {nextExpiring.Name} on {nextExpiring.ExpiryDate:yyyy-MM-dd}");
            }
        }
    }
}