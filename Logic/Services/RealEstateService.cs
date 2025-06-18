using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Xml;
using Microsoft.AspNetCore.Http;

namespace Logic.Services
{
    public class RealEstateService(IRealEstateRepository realEstateRepository, IRealEstateImageService imageService) : IRealEstateService
    {
        private readonly IRealEstateRepository _realEstateRepository = realEstateRepository;
        private readonly IRealEstateImageService _imageService = imageService;

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

        public async Task<string> UpdateRealEstateAsync(RealEstate realEstate)
        {
            try
            {
                realEstate.UpdatedAt = DateTime.UtcNow;
                await _realEstateRepository.UpdateAsync(realEstate);
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

        public async Task<(string, Guid)> CreateRealEstateAsync(RealEstate realEstate, List<IFormFile>? images)
        {
            var realEstateId = Guid.NewGuid();
            try
            {
                realEstate.Id = realEstateId;
                realEstate.CreatedAt = DateTime.UtcNow;

                // Валідація основних даних
                var validationResult = ValidateRealEstate(realEstate);
                if (validationResult != "Success")
                {
                    return (validationResult, Guid.Empty);
                }

                // Створити основний запис
                await _realEstateRepository.AddAsync(realEstate);

                // Обробити зображення якщо є
                if (images != null && images.Any())
                {
                    var imageResult = await _imageService.AddImagesAsync(realEstateId, images);
                    if (imageResult != "Success")
                    {
                        // Видалити створений запис якщо зображення не збереглися
                        await _realEstateRepository.DeleteAsync(realEstateId);
                        return ($"Property created but failed to save images: {imageResult}", Guid.Empty);
                    }
                }

                return ("Success", realEstateId);
            }
            catch (Exception ex)
            {
                return ($"Failed to create property. Error: {ex.Message}", Guid.Empty);
            }
        }

        public async Task<string> UpdateRealEstateAsync(RealEstate realEstate, List<IFormFile>? newImages, List<Guid>? removeImageIds)
        {
            try
            {
                // Перевірити що об'єкт існує
                var existingProperty = await _realEstateRepository.GetByIdAsync(realEstate.Id);
                if (existingProperty == null)
                {
                    return "Property not found.";
                }

                // Валідація
                var validationResult = ValidateRealEstate(realEstate);
                if (validationResult != "Success")
                {
                    return validationResult;
                }

                realEstate.UpdatedAt = DateTime.UtcNow;
                await _realEstateRepository.UpdateAsync(realEstate);

                // Видалити зображення якщо потрібно
                if (removeImageIds != null && removeImageIds.Any())
                {
                    foreach (var imageId in removeImageIds)
                    {
                        await _imageService.DeleteImageAsync(imageId);
                    }
                }

                // Додати нові зображення якщо є
                if (newImages != null && newImages.Any())
                {
                    var imageResult = await _imageService.AddImagesAsync(realEstate.Id, newImages);
                    if (imageResult != "Success")
                    {
                        return $"Property updated but failed to save new images: {imageResult}";
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed to update property. Error: {ex.Message}";
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