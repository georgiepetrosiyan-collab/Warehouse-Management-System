//user.cs

namespace WarehouseAPI.Models
{
    public enum UserRole { Admin, Buyer }
    
    /// <summary>
    
    /// </summary>
    public enum Gender { Male, Female, Unspecified }

    public class User
    {
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public Gender Gender { get; set; } = Gender.Unspecified;
        public int[] PurchaseFingerprint { get; set; }
        public PurchaseNode? HistoryHead { get; set; }

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
        public PurchaseNode? Next;
        public PurchaseNode(Product product) { Data = product; }
    }
}