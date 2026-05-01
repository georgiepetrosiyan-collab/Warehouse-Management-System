using System;

namespace WarehouseAPI.Models
{
    public enum Category { Food, Other }

    public class Product
    {
        public int Id { get; set; }
        public int BitIndex { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public int TotalSold { get; set; }
        public Category Category { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }
    }
}