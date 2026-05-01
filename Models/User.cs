namespace WarehouseAPI.Models
{
    public enum UserRole { Admin, Buyer }

    public class User
    {
        public string Username { get; set; }
        public UserRole Role { get; set; }
        public int[] PurchaseFingerprint { get; set; }
        public PurchaseNode HistoryHead { get; set; }

        public User(string name, UserRole role, int size = 20)
        {
            Username = name;
            Role = role;
            PurchaseFingerprint = new int[size];
        }
    }

    public class PurchaseNode
    {
        public Product Data;
        public PurchaseNode Next;
        public PurchaseNode(Product product) { Data = product; }
    }
}