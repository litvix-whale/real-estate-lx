using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Xml.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Infrastructure.Xml.Configs;
using Infrastructure.Xml.Interfaces;

namespace Infrastructure.Xml.Services
{
    public class XmlDataService : IXmlDataService
    {
        private readonly HttpClient _httpClient;
        private readonly XmlDataSettings _settings;
        private readonly ILogger<XmlDataService> _logger;

        public XmlDataService(HttpClient httpClient, IOptions<XmlDataSettings> settings, ILogger<XmlDataService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<List<XmlRealEstateItem>> LoadRealEstateDataAsync()
        {
            if (!_settings.EnableXmlSeeding)
            {
                _logger.LogInformation("XML seeding is disabled in settings");
                return new List<XmlRealEstateItem>();
            }

            try
            {
                _logger.LogInformation("Loading XML data from: {Url}", _settings.FeedUrl);

                var xmlContent = await _httpClient.GetStringAsync(_settings.FeedUrl);

                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    _logger.LogWarning("XML content is empty");
                    return new List<XmlRealEstateItem>();
                }

                _logger.LogInformation("XML content loaded, length: {Length} characters", xmlContent.Length);

                var serializer = new XmlSerializer(typeof(XmlResponse));
                using var reader = new StringReader(xmlContent);

                var response = (XmlResponse?)serializer.Deserialize(reader);

                if (response?.Items == null)
                {
                    _logger.LogWarning("No items found in XML response");
                    return new List<XmlRealEstateItem>();
                }

                var validItems = response.Items
                    .Where(IsValidItem)
                    .ToList();

                // Якщо потрібно завантажувати з кінця - реверсуємо список
                if (_settings.LoadFromEnd)
                {
                    validItems.Reverse();
                    _logger.LogInformation("Loading items from the end of XML feed");
                }

                // Обмежуємо кількість
                var itemsToTake = Math.Min(validItems.Count, _settings.MaxItemsToLoad);
                var resultItems = validItems.Take(itemsToTake).ToList();

                _logger.LogInformation("Found {Total} total items, {Valid} valid items, taking {Count} items",
                    response.Items.Count, validItems.Count, resultItems.Count);

                return resultItems;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error loading XML data");
                return new List<XmlRealEstateItem>();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout loading XML data");
                return new List<XmlRealEstateItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading XML data");
                return new List<XmlRealEstateItem>();
            }
        }

        public async Task SeedRealEstateInBatchesAsync(AppDbContext context, Guid userId)
        {
            var xmlItems = await LoadRealEstateDataAsync();

            if (!xmlItems.Any())
            {
                _logger.LogWarning("No XML items to process");
                return;
            }

            _logger.LogInformation("Starting batch processing of {Count} items with batch size {BatchSize}",
                xmlItems.Count, _settings.BatchSize);

            var processedCount = 0;
            var batches = xmlItems.Chunk(_settings.BatchSize);

            foreach (var batch in batches)
            {
                try
                {
                    var realEstates = batch
                        .Select(item => XmlRealEstateMapper.MapToRealEstate(item, userId))
                        .Where(re => re != null)
                        .ToList();

                    if (realEstates.Any())
                    {
                        await context.RealEstates.AddRangeAsync(realEstates);
                        await context.SaveChangesAsync();

                        processedCount += realEstates.Count;
                        _logger.LogInformation("Processed batch: {Count} items. Total processed: {Total}",
                            realEstates.Count, processedCount);
                    }

                    // Невелика затримка між батчами щоб не перевантажувати базу
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch of {Count} items", batch.Count());
                    // Продовжуємо обробку наступного батчу
                }
            }

            _logger.LogInformation("Batch processing completed. Total items processed: {Count}", processedCount);
        }

        private static bool IsValidItem(XmlRealEstateItem item)
        {
            return !string.IsNullOrWhiteSpace(item.Status) &&
                   item.Status.Equals("active", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(item.Title) &&
                   !string.IsNullOrWhiteSpace(item.Description);
        }
    }
}
