using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using MVC.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Core.Entities;
using Core.Enums;
using Core.Xml;

namespace MVC.Controllers
{
    public class RealEstateController(IRealEstateService realEstateService, IGoogleMapsService googleMapsService) : Controller
    {
        private readonly IRealEstateService _realEstateService = realEstateService;
        private readonly IGoogleMapsService _googleMapsService= googleMapsService;

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
        public async Task<IActionResult> Create(RealEstate model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userId, out Guid userGuid))
                {
                    model.UserId = userGuid;
                    var result = await _realEstateService.CreateRealEstateAsync(model);

                    if (result == "Success")
                    {
                        return RedirectToAction("Index");
                    }

                    ModelState.AddModelError(string.Empty, result);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateByIdAsync(id);

            if (realEstate == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid userGuid) || realEstate.UserId != userGuid)
            {
                return Forbid();
            }

            return View(realEstate);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(RealEstate model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userId, out Guid userGuid) && model.UserId == userGuid)
                {
                    var result = await _realEstateService.UpdateRealEstateAsync(model);

                    if (result == "Success")
                    {
                        return RedirectToAction("Details", new { id = model.Id });
                    }

                    ModelState.AddModelError(string.Empty, result);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Unauthorized");
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var realEstate = await _realEstateService.GetRealEstateByIdAsync(id);

            if (realEstate == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid userGuid) || realEstate.UserId != userGuid)
            {
                return Forbid();
            }

            var result = await _realEstateService.DeleteRealEstateAsync(id);

            if (result == "Success")
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, result);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyRealEstates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return Forbid();
            }

            var realEstates = await _realEstateService.GetUserRealEstatesAsync(userGuid);
            return View(realEstates);
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
            return View(new CreateRealEstateViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRealEstateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                    return View(model);
                }

                // Create RealEstate entity
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

                // Handle image uploads
                if (model.Images != null && model.Images.Any())
                {
                    var imageList = new List<RealEstateImage>();
                    int priority = 1;

                    foreach (var imageFile in model.Images)
                    {
                        if (imageFile.Length > 0)
                        {
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
    }
}