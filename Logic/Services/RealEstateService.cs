using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Xml;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

namespace Logic.Services
{
    public class RealEstateService(IRealEstateRepository realEstateRepository, IRealEstateImageService imageService, IRealEstateImageRepository realEstateImageRepository) : IRealEstateService
    {
        private readonly IRealEstateRepository _realEstateRepository = realEstateRepository;
        private readonly IRealEstateImageService _imageService = imageService;
        private readonly IRealEstateImageRepository _realEstateImageRepository = realEstateImageRepository;
        

        public async Task<IEnumerable<RealEstate>> SearchRealEstateAsync(RealEstateSearchCriteria criteria)
        {
            return await _realEstateRepository.SearchAsync(criteria);
        }

        public async Task<int> GetSearchCountAsync(RealEstateSearchCriteria criteria)
        {
            return await _realEstateRepository.GetSearchCountAsync(criteria);
        }

        public async Task<RealEstate?> GetRealEstateByIdAsync(Guid id)
        {
            return await _realEstateRepository.GetByIdAsync(id);
        }

        public async Task<RealEstate?> GetRealEstateWithImagesAsync(Guid id)
        {
            return await _realEstateRepository.GetByIdWithImagesAsync(id);
        }

        public async Task<string> CreateRealEstateAsync(RealEstate realEstate)
        {
            try
            {
                realEstate.CreatedAt = DateTime.UtcNow;
                await _realEstateRepository.AddAsync(realEstate);
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<IEnumerable<RealEstate>> GetUserRealEstatesAsync(Guid userId)
        {
            return await _realEstateRepository.GetByUserIdAsync(userId);
        }

        public async Task<string> UpdateRealEstateAsync(RealEstate realEstate, List<IFormFile>? newImages = null, List<Guid>? removeImageIds = null)
        {
            try
            {
                var existingProperty = await _realEstateRepository.GetByIdAsync(realEstate.Id);
                if (existingProperty == null)
                {
                    return "Property not found.";
                }

                var validationResult = ValidateRealEstate(realEstate);
                if (validationResult != "Success")
                {
                    return validationResult;
                }

                existingProperty.Title = realEstate.Title;
                existingProperty.Description = realEstate.Description;
                existingProperty.Category = realEstate.Category;
                existingProperty.RealtyType = realEstate.RealtyType;
                existingProperty.Deal = realEstate.Deal;
                existingProperty.IsNewBuilding = realEstate.IsNewBuilding;
                existingProperty.Country = realEstate.Country;
                existingProperty.Region = realEstate.Region;
                existingProperty.Locality = realEstate.Locality;
                existingProperty.Borough = realEstate.Borough;
                existingProperty.Street = realEstate.Street;
                existingProperty.StreetType = realEstate.StreetType;
                existingProperty.Latitude = realEstate.Latitude;
                existingProperty.Longitude = realEstate.Longitude;
                existingProperty.Floor = realEstate.Floor;
                existingProperty.TotalFloors = realEstate.TotalFloors;
                existingProperty.AreaTotal = realEstate.AreaTotal;
                existingProperty.AreaLiving = realEstate.AreaLiving;
                existingProperty.AreaKitchen = realEstate.AreaKitchen;
                existingProperty.RoomCount = realEstate.RoomCount;
                existingProperty.NewBuildingName = realEstate.NewBuildingName;
                existingProperty.Price = realEstate.Price;
                existingProperty.Currency = realEstate.Currency;
                existingProperty.UpdatedAt = DateTime.UtcNow;

                await _realEstateRepository.UpdateAsync(existingProperty);
                Console.WriteLine("Repository.UpdateAsync completed");

                if (removeImageIds != null && removeImageIds.Any())
                {
                    foreach (var imageId in removeImageIds)
                    {
                        var image = await _realEstateImageRepository.GetByIdAsync(imageId);
                        if (image != null)
                        {
                            // Видалити файл з диску
                            DeleteImageFile(image.Url);

                            // Видалити з БД
                            await _realEstateImageRepository.DeleteAsync(imageId);
                        }
                    }
                }

                // 5. Додати нові зображення
                if (newImages != null && newImages.Any())
                {
                    // Отримати максимальний пріоритет
                    var existingImages = await _realEstateImageRepository.GetImagesByRealEstateIdAsync(realEstate.Id);
                    var maxPriority = existingImages.Any() ? existingImages.Max(i => i.UiPriority) : 0;
                    var priority = maxPriority + 1;

                    foreach (var imageFile in newImages)
                    {
                        if (imageFile.Length > 0)
                        {
                            // Валідація
                            var (isValid, errorMessage) = ValidateImageFile(imageFile);
                            if (!isValid)
                            {
                                return errorMessage;
                            }

                            // Зберегти файл
                            var fileName = await SaveImageFileAsync(imageFile);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                // Додати до БД
                                await _realEstateImageRepository.AddAsync(new RealEstateImage
                                {
                                    Url = fileName,
                                    UiPriority = priority++,
                                    RealEstateId = realEstate.Id
                                });
                            }
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed to update property. Error: {ex.Message}";
            }
        }

        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // TODO
            }
        }

        public async Task<string> DeleteRealEstateAsync(Guid id)
        {
            try
            {
                var property = await _realEstateRepository.GetByIdAsync(id);
                if (property == null)
                {
                    return "Property not found.";
                }

                // Видалити всі зображення спочатку
                var images = await _imageService.GetImagesByPropertyIdAsync(id);
                foreach (var image in images)
                {
                    await _imageService.DeleteImageAsync(image.Id);
                }

                // Видалити основний запис
                await _realEstateRepository.DeleteAsync(id);

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed to delete property. Error: {ex.Message}";
            }
        }

        private async Task<string?> SaveImageFileAsync(IFormFile imageFile)
        {
            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                return "/images/properties/" + uniqueFileName;
            }
            catch
            {
                return null;
            }
        }

        private static (bool isValid, string errorMessage) ValidateImageFile(IFormFile file)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return (false, "Image size exceeds maximum limit of 5MB.");
            }

            var allowedImageTypes = new[]
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/webp"
            };

            if (!allowedImageTypes.Contains(file.ContentType))
            {
                return (false, "Invalid image type. Only JPEG, PNG, GIF, and WebP are allowed.");
            }

            return (true, string.Empty);
        }

        public async Task<string> AddImagesToPropertyAsync(Guid realEstateId, List<IFormFile> images)
        {
            var property = await _realEstateRepository.GetByIdAsync(realEstateId);
            if (property == null)
            {
                return "Property not found.";
            }

            return await _imageService.AddImagesAsync(realEstateId, images);
        }

        public async Task<string> RemoveImageAsync(Guid imageId)
        {
            return await _imageService.DeleteImageAsync(imageId);
        }

        public async Task<byte[]?> GetImageContentAsync(Guid imageId)
        {
            return await _imageService.GetImageContentAsync(imageId);
        }

        private static string ValidateRealEstate(RealEstate realEstate)
        {
            if (string.IsNullOrWhiteSpace(realEstate.Title))
                return "Title is required.";

            if (string.IsNullOrWhiteSpace(realEstate.Description))
                return "Description is required.";

            if (realEstate.Price <= 0)
                return "Price must be greater than zero.";

            if (realEstate.AreaTotal <= 0)
                return "Total area must be greater than zero.";

            if (realEstate.RoomCount < 0)
                return "Room count cannot be negative.";

            if (realEstate.Floor <= 0)
                return "Floor must be greater than zero.";

            if (realEstate.TotalFloors <= 0)
                return "Total floors must be greater than zero.";

            if (realEstate.Floor > realEstate.TotalFloors)
                return "Floor cannot be higher than total floors.";

            if (string.IsNullOrWhiteSpace(realEstate.Country))
                return "Country is required.";

            if (string.IsNullOrWhiteSpace(realEstate.Region))
                return "Region is required.";

            if (string.IsNullOrWhiteSpace(realEstate.Locality))
                return "City is required.";

            if (string.IsNullOrWhiteSpace(realEstate.Street))
                return "Street is required.";

            return "Success";
        }
    }
}