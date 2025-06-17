using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Xml.Mappers
{
    public static class XmlRealEstateMapper
    {
        public static RealEstate MapToRealEstate(XmlRealEstateItem xmlItem, Guid userId)
        {
            var realEstate = new RealEstate
            {
                // Required fields with safe defaults
                Title = GetSafeString(xmlItem.Title, "Без назви"),
                Description = GetSafeString(xmlItem.Description, "Опис відсутній"),
                IsNewBuilding = xmlItem.IsNewBuilding,

                // Enums with safe mapping
                Category = MapCategory(xmlItem.Category?.ValueInt ?? 1),
                RealtyType = MapRealtyType(xmlItem.RealtyType?.ValueInt ?? 1),
                Deal = MapDeal(xmlItem.Deal?.ValueInt ?? 2),

                // Location with safe defaults
                Country = GetSafeString(xmlItem.Location?.Country?.Text, "Україна"),
                Region = GetSafeString(xmlItem.Location?.Region?.Text, "Невідома область"),
                Locality = GetSafeString(xmlItem.Location?.City?.Text, "Невідоме місто"),
                Borough = GetSafeString(xmlItem.Location?.Borough?.Text, "Невідомий район"),
                Street = GetSafeString(xmlItem.Location?.Street?.Text, "Невідома вулиця"),
                StreetType = GetSafeString(xmlItem.Location?.StreetType?.Text, "вулиця"),
                Latitude = xmlItem.Location?.MapLat,
                Longitude = xmlItem.Location?.MapLng,

                // Property details with safe defaults
                Floor = Math.Max(1, xmlItem.Floor),
                TotalFloors = Math.Max(1, xmlItem.TotalFloors),
                AreaTotal = Math.Max(1f, xmlItem.AreaTotal),
                AreaLiving = xmlItem.AreaLiving,
                AreaKitchen = xmlItem.AreaKitchen,
                RoomCount = Math.Max(1, xmlItem.RoomCount),
                NewBuildingName = xmlItem.NewBuildingName,

                // Price with safe defaults
                Price = Math.Max(1, xmlItem.Price?.Value ?? 1),
                Currency = MapCurrency(xmlItem.Price?.Currency),

                // System fields
                CreatedAt = xmlItem.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = xmlItem.UpdatedAt,
                UserId = userId
            };

            // Map images safely
            realEstate.Images = GetSafeImages(xmlItem.Images?.ImageUrls, realEstate.Id);

            return realEstate;
        }

        private static string GetSafeString(string? value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static List<RealEstateImage> GetSafeImages(List<string>? imageUrls, Guid realEstateId)
        {
            if (imageUrls == null || !imageUrls.Any())
                return new List<RealEstateImage>();

            return imageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url) && IsValidImageUrl(url))
                .Take(12) // Обмежити кількість зображень
                .Select((url, index) => new RealEstateImage
                {
                    Url = url.Trim(),
                    UiPriority = index,
                    RealEstateId = realEstateId
                })
                .ToList();
        }

        private static RealEstateCategoryEnum MapCategory(int categoryValue)
        {
            return categoryValue switch
            {
                1 => RealEstateCategoryEnum.Residential,
                2 => RealEstateCategoryEnum.Commercial,
                _ => RealEstateCategoryEnum.Residential // Default
            };
        }

        private static RealEstateTypeEnum MapRealtyType(int realtyTypeValue)
        {
            return realtyTypeValue switch
            {
                1 => RealEstateTypeEnum.Apartment,
                2 => RealEstateTypeEnum.Room,
                3 => RealEstateTypeEnum.House,
                4 => RealEstateTypeEnum.Office,
                _ => RealEstateTypeEnum.Apartment // Default
            };
        }

        private static DealTypeEnum MapDeal(int dealValue)
        {
            return dealValue switch
            {
                1 => DealTypeEnum.Sale,
                2 => DealTypeEnum.Rent,
                _ => DealTypeEnum.Rent // Default
            };
        }

        private static CurrencyEnum MapCurrency(string? currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return CurrencyEnum.UAH; // Default

            return currency.ToUpper().Trim() switch
            {
                "UAH" => CurrencyEnum.UAH,
                "USD" => CurrencyEnum.USD,
                "EUR" => CurrencyEnum.EUR,
                _ => CurrencyEnum.UAH // Default
            };
        }

        private static bool IsValidImageUrl(string url)
        {
            try
            {
                return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                       (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                       !string.IsNullOrWhiteSpace(uri.Host);
            }
            catch
            {
                return false;
            }
        }
    }
}
