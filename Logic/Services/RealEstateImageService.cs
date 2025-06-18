using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Services
{
    public class RealEstateImageService(IRealEstateImageRepository imageRepository) : IRealEstateImageService
    {
        private readonly IRealEstateImageRepository _imageRepository = imageRepository;

        public async Task<string> AddImagesAsync(Guid realEstateId, List<IFormFile> images)
        {
            try
            {
                var currentMaxPriority = await _imageRepository.GetMaxPriorityAsync(realEstateId);
                var priority = currentMaxPriority + 1;

                foreach (var imageFile in images)
                {
                    var (isValid, errorMessage) = ValidateImageFile(imageFile);
                    if (!isValid)
                    {
                        return errorMessage;
                    }

                    var (imageContent, fileName, contentType) = await ProcessImageAsync(imageFile);
                    if (imageContent == null)
                    {
                        return "Failed to process image file.";
                    }

                    var image = new RealEstateImage
                    {
                        Url = Convert.ToBase64String(imageContent), // Або шлях до файлу
                        UiPriority = priority++,
                        RealEstateId = realEstateId
                    };

                    await _imageRepository.AddAsync(image);
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed to add images. Error: {ex.Message}";
            }
        }

        public async Task<string> DeleteImageAsync(Guid imageId)
        {
            try
            {
                var image = await _imageRepository.GetByIdAsync(imageId);
                if (image == null)
                {
                    return "Image not found.";
                }

                await _imageRepository.DeleteAsync(imageId);
                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed to delete image. Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateImagePriorityAsync(Guid imageId, int newPriority)
        {
            try
            {
                var image = await _imageRepository.GetByIdAsync(imageId);
                if (image == null)
                {
                    return "Image not found.";
                }

                image.UiPriority = newPriority;
                image.UpdatedAt = DateTime.UtcNow;

                await _imageRepository.UpdateAsync(image);
                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed to update image priority. Error: {ex.Message}";
            }
        }

        public async Task<IEnumerable<RealEstateImage>> GetImagesByPropertyIdAsync(Guid realEstateId)
        {
            return await _imageRepository.GetImagesByRealEstateIdAsync(realEstateId);
        }

        public async Task<byte[]?> GetImageContentAsync(Guid imageId)
        {
            try
            {
                var image = await _imageRepository.GetByIdAsync(imageId);
                if (image?.Url == null)
                {
                    return null;
                }

                // Якщо зберігаєте як Base64
                return Convert.FromBase64String(image.Url);
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

        private static async Task<(byte[]?, string?, string?)> ProcessImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (null, null, null);
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                return (
                    memoryStream.ToArray(),
                    Path.GetFileName(file.FileName),
                    file.ContentType
                );
            }
            catch
            {
                return (null, null, null);
            }
        }
    }
}
