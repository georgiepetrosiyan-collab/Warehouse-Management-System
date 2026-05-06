using System;
using WarehouseAPI.Models;

namespace WarehouseAPI.Dtos
{
    public record AddProductDto(
        string    Name,
        double    Price,
        int       Quantity,
        Category  Category,
        DateTime? ExpiryDate
    );

    public record LoginDto(string Username, UserRole Role, Gender Gender = Gender.Unspecified);

    public record BuyDto(List<int> ProductIds);
}