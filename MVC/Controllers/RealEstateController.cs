using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using MVC.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Core.Entities;
using Core.Enums;
using Core.Xml;
using Infrastructure.Repositories;

namespace MVC.Controllers
{
    public class RealEstateController(IRealEstateService realEstateService, IGoogleMapsService googleMapsService, IRealEstateImageRepository realEstateImageRepository) : Controller
    {
        private readonly IRealEstateService _realEstateService = realEstateService;
        private readonly IGoogleMapsService _googleMapsService = googleMapsService;
        private readonly IRealEstateImageRepository _realEstateImageRepository = realEstateImageRepository;

        [HttpGet]
        public async Task<IActionResult> Index(string? searchQuery = "",
            RealEstateCategoryEnum? category = null,
            RealEstateTypeEnum? realtyType = null,
            DealTypeEnum? deal = null,
            bool? isNewBuilding = null,
            string? locality = "",
            string? region = "",
            int? minPrice = null,
            int? maxPrice = null,
            CurrencyEnum? currency = null,
            int? minRoomCount = null,
            int? maxRoomCount = null,
            float? minAreaTotal = null,
            float? maxAreaTotal = null,
            string sortOrder = "date_desc",
            int page = 1,
            int pageSize = 9)
        {
            var criteria = new RealEstateSearchCriteria
            {
                SearchQuery = searchQuery,
                Category = category,
                RealtyType = realtyType,
                Deal = deal,
                IsNewBuilding = isNewBuilding,
                Locality = locality,
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Currency = currency,
                MinRoomCount = minRoomCount,
                MaxRoomCount = maxRoomCount,
                MinAreaTotal = minAreaTotal,
                MaxAreaTotal = maxAreaTotal,
                SortBy = GetSortField(sortOrder),
                SortDescending = GetSortDirection(sortOrder),
                Skip = (page - 1) * pageSize,
                Take = pageSize
            };

            var realEstates = await _realEstateService.SearchRealEstateAsync(criteria);
            var totalItems = await _realEstateService.GetSearchCountAsync(criteria);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var model = new RealEstateSearchViewModel
            {
                SearchQuery = searchQuery,
                Category = category,
                RealtyType = realtyType,
                Deal = deal,
                IsNewBuilding = isNewBuilding,
                Locality = locality,
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Currency = currency,
                MinRoomCount = minRoomCount,
                MaxRoomCount = maxRoomCount,
                MinAreaTotal = minAreaTotal,
                MaxAreaTotal = maxAreaTotal,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize,
                RealEstates = realEstates,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateWithImagesAsync(id);

            if (realEstate == null)
            {
                return NotFound();
            }

            ViewBag.GoogleMapsApiKey = _googleMapsService.GetApiKey();

            return View(realEstate);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RealEstateViewCreateModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                    return View(model);
                }

                // Конвертуємо ViewModel в Entity
                var realEstate = new RealEstate
                {
                    Title = model.Title,
                    Description = model.Description,
                    Category = model.Category,
                    RealtyType = model.RealtyType,
                    Deal = model.Deal,
                    IsNewBuilding = model.IsNewBuilding,
                    Country = model.Country,
                    Region = model.Region,
                    Locality = model.Locality,
                    Borough = model.Borough ?? string.Empty,
                    Street = model.Street,
                    StreetType = model.StreetType ?? string.Empty,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Floor = model.Floor,
                    TotalFloors = model.TotalFloors,
                    AreaTotal = model.AreaTotal,
                    AreaLiving = model.AreaLiving,
                    AreaKitchen = model.AreaKitchen,
                    RoomCount = model.RoomCount,
                    NewBuildingName = model.NewBuildingName,
                    Price = model.Price,
                    Currency = model.Currency,
                    UserId = userGuid
                };

                if (model.Images != null && model.Images.Any())
                {
                    var imageList = new List<RealEstateImage>();
                    int priority = 1;

                    foreach (var imageFile in model.Images)
                    {
                        if (imageFile.Length > 0)
                        {
                            // Валідація зображення
                            var validationResult = ValidateImage(imageFile);
                            if (!validationResult.IsValid)
                            {
                                ModelState.AddModelError(string.Empty, validationResult.ErrorMessage);
                                return View(model);
                            }

                            // Зберегти файл і отримати URL
                            var fileName = await SaveImageAsync(imageFile);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                imageList.Add(new RealEstateImage
                                {
                                    Url = fileName,
                                    UiPriority = priority++,
                                    RealEstateId = realEstate.Id
                                });
                            }
                        }
                    }
                    realEstate.Images = imageList;
                }

                var result = await _realEstateService.CreateRealEstateAsync(realEstate);

                if (result == "Success")
                {
                    TempData["SuccessMessage"] = "Property added successfully!";
                    return RedirectToAction("Details", new { id = realEstate.Id });
                }

                ModelState.AddModelError(string.Empty, result);
            }

            return View(model);
        }

        private (bool IsValid, string ErrorMessage) ValidateImage(IFormFile file)
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateWithImagesAsync(id);

            if (realEstate == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid userGuid) || realEstate.UserId != userGuid)
            {
                return Forbid();
            }

            var model = new RealEstateViewCreateModel
            {
                Id = realEstate.Id,
                UserId = realEstate.UserId,
                CreatedAt = realEstate.CreatedAt,
                UpdatedAt = realEstate.UpdatedAt,
                Title = realEstate.Title,
                Description = realEstate.Description,
                Category = realEstate.Category,
                RealtyType = realEstate.RealtyType,
                Deal = realEstate.Deal,
                IsNewBuilding = realEstate.IsNewBuilding,
                Country = realEstate.Country,
                Region = realEstate.Region,
                Locality = realEstate.Locality,
                Borough = realEstate.Borough,
                Street = realEstate.Street,
                StreetType = realEstate.StreetType,
                Latitude = realEstate.Latitude,
                Longitude = realEstate.Longitude,
                Floor = realEstate.Floor,
                TotalFloors = realEstate.TotalFloors,
                AreaTotal = (float)realEstate.AreaTotal,
                AreaLiving = (float?)realEstate.AreaLiving,
                AreaKitchen = (float?)realEstate.AreaKitchen,
                RoomCount = realEstate.RoomCount,
                NewBuildingName = realEstate.NewBuildingName,
                Price = realEstate.Price,
                Currency = realEstate.Currency,
                ExistingImages = realEstate.Images?.ToList() ?? new List<RealEstateImage>()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RealEstateViewCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out Guid userGuid) || model.UserId != userGuid)
                {
                    ModelState.AddModelError(string.Empty, "Unauthorized");
                    return View(model);
                }

                try
                {
                    var realEstate = new RealEstate
                    {
                        Id = model.Id ?? Guid.Empty,
                        UserId = model.UserId ?? Guid.Empty,
                        CreatedAt = model.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = model.UpdatedAt,
                        Title = model.Title,
                        Description = model.Description,
                        Category = model.Category,
                        RealtyType = model.RealtyType,
                        Deal = model.Deal,
                        IsNewBuilding = model.IsNewBuilding,
                        Country = model.Country,
                        Region = model.Region,
                        Locality = model.Locality,
                        Borough = model.Borough ?? string.Empty,
                        Street = model.Street,
                        StreetType = model.StreetType ?? string.Empty,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,
                        Floor = model.Floor,
                        TotalFloors = model.TotalFloors,
                        AreaTotal = model.AreaTotal,
                        AreaLiving = model.AreaLiving,
                        AreaKitchen = model.AreaKitchen,
                        RoomCount = model.RoomCount,
                        NewBuildingName = model.NewBuildingName,
                        Price = model.Price,
                        Currency = model.Currency
                    };

                    var result = await _realEstateService.UpdateRealEstateAsync(realEstate, model.NewImages, model.RemoveImageIds);

                    if (result == "Success")
                    {
                        TempData["SuccessMessage"] = "Property updated successfully!";
                        return RedirectToAction("Details", new { id = model.Id });
                    }

                    ModelState.AddModelError(string.Empty, result);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error updating property: {ex.Message}");
                }
            }

            // Перезавантажити зображення при помилці
            if (model.Id.HasValue)
            {
                var realEstateWithImages = await _realEstateService.GetRealEstateWithImagesAsync(model.Id.Value);
                if (realEstateWithImages?.Images != null)
                {
                    model.ExistingImages = realEstateWithImages.Images.ToList();
                }
            }

            return View(model);
        }

        private async Task<int> GetMaxImagePriority(Guid realEstateId)
        {
            var realEstate = await _realEstateService.GetRealEstateWithImagesAsync(realEstateId);
            return realEstate?.Images?.Any() == true ? realEstate.Images.Max(i => i.UiPriority) : 0;
        }

        private async Task AddImageRecord(Guid realEstateId, string fileName, int priority)
        {
            var image = new RealEstateImage
            {
                RealEstateId = realEstateId,
                Url = fileName,
                UiPriority = priority
            };

            // Використайте ваш image repository
            await _realEstateImageRepository.AddAsync(image);
        }

        private async Task DeleteImageFileAndRecord(Guid imageId)
        {
            // Отримати запис зображення
            var image = await _realEstateImageRepository.GetByIdAsync(imageId);
            if (image != null)
            {
                // Видалити файл з файлової системи
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Видалити запис з бази
                await _realEstateImageRepository.DeleteAsync(imageId);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateByIdAsync(id);

            if (realEstate == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!Guid.TryParse(userId, out Guid userGuid) || (realEstate.UserId != userGuid && !isAdmin))
            {
                return Forbid();
            }

            var result = await _realEstateService.DeleteRealEstateAsync(id);

            if (result == "Success")
            {
                TempData["SuccessMessage"] = "Property deleted successfully!";

                if (isAdmin && Request.Headers.Referer.ToString().Contains("Admin"))
                {
                    return RedirectToAction("AdminIndex");
                }

                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, result);
            return RedirectToAction("Index");
        }

        private string GetSortField(string sortOrder)
        {
            return sortOrder switch
            {
                "price_asc" or "price_desc" => "Price",
                "area_asc" or "area_desc" => "AreaTotal",
                "rooms_asc" or "rooms_desc" => "RoomCount",
                "floor_asc" or "floor_desc" => "Floor",
                _ => "CreatedAt"
            };
        }

        private bool GetSortDirection(string sortOrder)
        {
            return sortOrder.EndsWith("_desc");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            return View(new RealEstateViewCreateModel());
        }


        private async Task<string?> SaveImageAsync(IFormFile imageFile)
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(string? searchQuery = "",
    RealEstateCategoryEnum? category = null,
    RealEstateTypeEnum? realtyType = null,
    DealTypeEnum? deal = null,
    bool? isNewBuilding = null,
    string? locality = "",
    string? region = "",
    int? minPrice = null,
    int? maxPrice = null,
    CurrencyEnum? currency = null,
    int? minRoomCount = null,
    int? maxRoomCount = null,
    float? minAreaTotal = null,
    float? maxAreaTotal = null,
    string sortOrder = "date_desc",
    int page = 1,
    int pageSize = 12)
        {
            var criteria = new RealEstateSearchCriteria
            {
                SearchQuery = searchQuery,
                Category = category,
                RealtyType = realtyType,
                Deal = deal,
                IsNewBuilding = isNewBuilding,
                Locality = locality,
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Currency = currency,
                MinRoomCount = minRoomCount,
                MaxRoomCount = maxRoomCount,
                MinAreaTotal = minAreaTotal,
                MaxAreaTotal = maxAreaTotal,
                SortBy = GetSortField(sortOrder),
                SortDescending = GetSortDirection(sortOrder),
                Skip = (page - 1) * pageSize,
                Take = pageSize
            };

            var realEstates = await _realEstateService.SearchRealEstateAsync(criteria);
            var totalItems = await _realEstateService.GetSearchCountAsync(criteria);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var model = new RealEstateSearchViewModel
            {
                SearchQuery = searchQuery,
                Category = category,
                RealtyType = realtyType,
                Deal = deal,
                IsNewBuilding = isNewBuilding,
                Locality = locality,
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Currency = currency,
                MinRoomCount = minRoomCount,
                MaxRoomCount = maxRoomCount,
                MinAreaTotal = minAreaTotal,
                MaxAreaTotal = maxAreaTotal,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize,
                RealEstates = realEstates,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDelete(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateByIdAsync(id);

            if (realEstate == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction("AdminIndex");
            }

            var result = await _realEstateService.DeleteRealEstateAsync(id);

            if (result == "Success")
            {
                TempData["SuccessMessage"] = $"Property '{realEstate.Title}' has been deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to delete property: {result}";
            }

            return RedirectToAction("AdminIndex");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEdit(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateWithImagesAsync(id);

            if (realEstate == null)
            {
                return NotFound();
            }

            var model = new RealEstateViewCreateModel
            {
                Id = realEstate.Id,
                UserId = realEstate.UserId,
                CreatedAt = realEstate.CreatedAt,
                UpdatedAt = realEstate.UpdatedAt,
                Title = realEstate.Title,
                Description = realEstate.Description,
                Category = realEstate.Category,
                RealtyType = realEstate.RealtyType,
                Deal = realEstate.Deal,
                IsNewBuilding = realEstate.IsNewBuilding,
                Country = realEstate.Country,
                Region = realEstate.Region,
                Locality = realEstate.Locality,
                Borough = realEstate.Borough,
                Street = realEstate.Street,
                StreetType = realEstate.StreetType,
                Latitude = realEstate.Latitude,
                Longitude = realEstate.Longitude,
                Floor = realEstate.Floor,
                TotalFloors = realEstate.TotalFloors,
                AreaTotal = (float)realEstate.AreaTotal,
                AreaLiving = (float?)realEstate.AreaLiving,
                AreaKitchen = (float?)realEstate.AreaKitchen,
                RoomCount = realEstate.RoomCount,
                NewBuildingName = realEstate.NewBuildingName,
                Price = realEstate.Price,
                Currency = realEstate.Currency,
                ExistingImages = realEstate.Images?.ToList() ?? new List<RealEstateImage>()
            };

            ViewBag.IsAdminEdit = true;
            return View("Edit", model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEdit(RealEstateViewCreateModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var realEstate = new RealEstate
                    {
                        Id = model.Id ?? Guid.Empty,
                        UserId = model.UserId ?? Guid.Empty,
                        CreatedAt = model.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = model.UpdatedAt,
                        Title = model.Title,
                        Description = model.Description,
                        Category = model.Category,
                        RealtyType = model.RealtyType,
                        Deal = model.Deal,
                        IsNewBuilding = model.IsNewBuilding,
                        Country = model.Country,
                        Region = model.Region,
                        Locality = model.Locality,
                        Borough = model.Borough ?? string.Empty,
                        Street = model.Street,
                        StreetType = model.StreetType ?? string.Empty,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,
                        Floor = model.Floor,
                        TotalFloors = model.TotalFloors,
                        AreaTotal = model.AreaTotal,
                        AreaLiving = model.AreaLiving,
                        AreaKitchen = model.AreaKitchen,
                        RoomCount = model.RoomCount,
                        NewBuildingName = model.NewBuildingName,
                        Price = model.Price,
                        Currency = model.Currency
                    };

                    var result = await _realEstateService.UpdateRealEstateAsync(realEstate, model.NewImages, model.RemoveImageIds);

                    if (result == "Success")
                    {
                        TempData["SuccessMessage"] = "Property updated successfully by Admin!";
                        return RedirectToAction("AdminIndex");
                    }

                    ModelState.AddModelError(string.Empty, result);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error updating property: {ex.Message}");
                }
            }

            if (model.Id.HasValue)
            {
                var realEstateWithImages = await _realEstateService.GetRealEstateWithImagesAsync(model.Id.Value);
                if (realEstateWithImages?.Images != null)
                {
                    model.ExistingImages = realEstateWithImages.Images.ToList();
                }
            }

            ViewBag.IsAdminEdit = true;
            return View("Edit", model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyRealEstates(string? searchQuery = "",
    RealEstateCategoryEnum? category = null,
    RealEstateTypeEnum? realtyType = null,
    DealTypeEnum? deal = null,
    bool? isNewBuilding = null,
    string? locality = "",
    string? region = "",
    int? minPrice = null,
    int? maxPrice = null,
    CurrencyEnum? currency = null,
    int? minRoomCount = null,
    int? maxRoomCount = null,
    float? minAreaTotal = null,
    float? maxAreaTotal = null,
    string sortOrder = "date_desc",
    int page = 1,
    int pageSize = 9)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return Forbid();
            }

            var criteria = new RealEstateSearchCriteria
            {
                UserId = userGuid,
                SearchQuery = searchQuery,
                Category = category,
                RealtyType = realtyType,
                Deal = deal,
                IsNewBuilding = isNewBuilding,
                Locality = locality,
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Currency = currency,
                MinRoomCount = minRoomCount,
                MaxRoomCount = maxRoomCount,
                MinAreaTotal = minAreaTotal,
                MaxAreaTotal = maxAreaTotal,
                SortBy = GetSortField(sortOrder),
                SortDescending = GetSortDirection(sortOrder),
                Skip = (page - 1) * pageSize,
                Take = pageSize
            };

            var realEstates = await _realEstateService.SearchRealEstateAsync(criteria);
            var totalItems = await _realEstateService.GetSearchCountAsync(criteria);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var model = new MyRealEstatesViewModel
            {
                SearchQuery = searchQuery,
                Category = category,
                RealtyType = realtyType,
                Deal = deal,
                IsNewBuilding = isNewBuilding,
                Locality = locality,
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Currency = currency,
                MinRoomCount = minRoomCount,
                MaxRoomCount = maxRoomCount,
                MinAreaTotal = minAreaTotal,
                MaxAreaTotal = maxAreaTotal,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize,
                RealEstates = realEstates,
                TotalPages = totalPages,
                TotalItems = totalItems,
                UserId = userGuid
            };

            return View(model);
        }
    }
}