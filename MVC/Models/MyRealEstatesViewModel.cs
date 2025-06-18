using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVC.Models
{
    public class MyRealEstatesViewModel
    {
        // Фільтри
        public string? SearchQuery { get; set; }
        public RealEstateCategoryEnum? Category { get; set; }
        public RealEstateTypeEnum? RealtyType { get; set; }
        public DealTypeEnum? Deal { get; set; }
        public bool? IsNewBuilding { get; set; }
        public string? Locality { get; set; }
        public string? Region { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public CurrencyEnum? Currency { get; set; }
        public int? MinRoomCount { get; set; }
        public int? MaxRoomCount { get; set; }
        public float? MinAreaTotal { get; set; }
        public float? MaxAreaTotal { get; set; }
        public string SortOrder { get; set; } = "date_desc";

        // Пагінація
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        // Дані
        public IEnumerable<RealEstate> RealEstates { get; set; } = new List<RealEstate>();
        public Guid UserId { get; set; }

        // Статистика
        public int ActiveListings => RealEstates.Count();
        public int ForSale => RealEstates.Count(r => r.Deal == DealTypeEnum.Sale);
        public int ForRent => RealEstates.Count(r => r.Deal == DealTypeEnum.Rent);
    }
}
