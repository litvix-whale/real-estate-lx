using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVC.Models
{
    public class RealEstateViewCreateModel
    {
        public Guid? Id { get; set; }
        public Guid? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<RealEstateImage>? ExistingImages { get; set; }

        public List<Guid>? RemoveImageIds { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public RealEstateCategoryEnum Category { get; set; }

        [Required(ErrorMessage = "Property type is required")]
        public RealEstateTypeEnum RealtyType { get; set; }

        [Required(ErrorMessage = "Deal type is required")]
        public DealTypeEnum Deal { get; set; }

        public bool IsNewBuilding { get; set; }

        // Location
        [Required(ErrorMessage = "Country is required")]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required")]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string Locality { get; set; } = string.Empty;

        [StringLength(100)] public string? Borough { get; set; }

        [Required(ErrorMessage = "Street is required")]
        [StringLength(200)]
        public string Street { get; set; } = string.Empty;

        [StringLength(50)] public string? StreetType { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        // Property details
        [Range(1, 200, ErrorMessage = "Floor must be between 1 and 200")]
        public int Floor { get; set; }

        [Range(1, 200, ErrorMessage = "Total floors must be between 1 and 200")]
        public int TotalFloors { get; set; }

        [Required(ErrorMessage = "Total area is required")]
        [Range(1, 10000, ErrorMessage = "Total area must be between 1 and 10000 m²")]
        public float AreaTotal { get; set; }

        [Range(1, 10000, ErrorMessage = "Living area must be between 1 and 10000 m²")]
        public float? AreaLiving { get; set; }

        [Range(1, 500, ErrorMessage = "Kitchen area must be between 1 and 500 m²")]
        public float? AreaKitchen { get; set; }

        [Range(1, 50, ErrorMessage = "Room count must be between 1 and 50")]
        public int RoomCount { get; set; }

        [StringLength(200)] public string? NewBuildingName { get; set; }

        // Price
        [Required(ErrorMessage = "Price is required")]
        [Range(1, 999999999, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Currency is required")]
        public CurrencyEnum Currency { get; set; }

        // Images
        public List<IFormFile>? Images { get; set; }
        public List<IFormFile>? NewImages { get; set; }
    }
}
