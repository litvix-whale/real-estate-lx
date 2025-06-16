using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Enums;

namespace Core.Entities
{
    public class RealEstate : EntityBase
    {
        public bool IsNewBuilding { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;

        public RealEstateCategoryEnum Category { get; set; }
        public RealEstateTypeEnum RealtyType { get; set; }
        public DealTypeEnum Deal { get; set; }

        // Локація
        public string Country { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Locality { get; set; } = default!;
        public string Borough { get; set; } = default!;
        public string Street { get; set; } = default!;
        public string StreetType { get; set; } = default!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Квартира
        public int Floor { get; set; }
        public int TotalFloors { get; set; }
        public float AreaTotal { get; set; }
        public float? AreaLiving { get; set; }
        public float? AreaKitchen { get; set; }
        public int RoomCount { get; set; }
        public string? NewBuildingName { get; set; }

        public decimal Price { get; set; }
        public CurrencyEnum Currency { get; set; }

        public virtual ICollection<RealEstateImage> Images { get; set; } = new List<RealEstateImage>();

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

    }
}
