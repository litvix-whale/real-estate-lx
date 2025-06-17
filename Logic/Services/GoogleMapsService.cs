using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly IConfiguration _configuration;

        public GoogleMapsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetApiKey()
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Google Maps API key is not configured. Please add GOOGLE_MAPS_API_KEY to your .env file.");
            }
            return apiKey;
        }

        public string GetMapEmbedUrl(double latitude, double longitude, int zoom = 15)
        {
            var apiKey = GetApiKey();
            return $"https://www.google.com/maps/embed/v1/place?key={apiKey}&q={latitude},{longitude}&zoom={zoom}";
        }

        public string GetStaticMapUrl(double latitude, double longitude, int zoom = 15, string size = "600x400")
        {
            var apiKey = GetApiKey();
            return $"https://maps.googleapis.com/maps/api/staticmap?center={latitude},{longitude}&zoom={zoom}&size={size}&markers=color:red%7C{latitude},{longitude}&key={apiKey}";
        }
    }
}
