using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVC.Models
{
    public class RealEstateSearchViewModel
    {
        public string? SearchQuery { get; set; }
        public RealEstateCategoryEnum? Category { get; set; }
        public RealEstateTypeEnum? RealtyType { get; set; }
        public DealTypeEnum? Deal { get; set; }
        public bool? IsNewBuilding { get; set; }

        // Локація
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Locality { get; set; }
        public string? Borough { get; set; }

        // Характеристики
        public int? MinFloor { get; set; }
        public int? MaxFloor { get; set; }
        public float? MinAreaTotal { get; set; }
        public float? MaxAreaTotal { get; set; }
        public int? MinRoomCount { get; set; }
        public int? MaxRoomCount { get; set; }

        // Ціна
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public CurrencyEnum? Currency { get; set; }

        // Пагінація та сортування
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public string SortOrder { get; set; } = "date_desc";

        // Результати
        public IEnumerable<RealEstate> RealEstates { get; set; } = new List<RealEstate>();
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}
