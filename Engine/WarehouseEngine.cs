using System.Collections.Generic;
using WarehouseAPI.Models;

namespace WarehouseAPI.Engine
{
    public class WarehouseEngine
    {
        public List<Product> MasterInventory = new();
        public List<User>    UserRegistry    = new();

        private ProductBST productTree = new();
        private const int ID_PREFIX = 2026000;

        public User GetOrCreateUser(string name, UserRole role)
        {
            foreach (var u in UserRegistry)
                if (u.Username.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return u;

            var newUser = new User(name, role);
            UserRegistry.Add(newUser);
            return newUser;
        }

        public Product AddProduct(string name, double price, int quantity,
                                  Category category, System.DateTime? expiry)
        {
            var existing = MasterInventory.Find(p =>
                p.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Quantity += quantity;
                return existing;
            }

            var product = new Product
            {
                Id         = ID_PREFIX + MasterInventory.Count + 1,
                BitIndex   = MasterInventory.Count,
                Name       = name,
                Price      = price,
                Quantity   = quantity,
                Category   = category,
                ExpiryDate = expiry
            };

            MasterInventory.Add(product);
            productTree.Insert(product);
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

                var node = new PurchaseNode(p) { Next = buyer.HistoryHead };
                buyer.HistoryHead = node;
                bought.Add(p.Name);
            }
            return bought;
        }
    }
}