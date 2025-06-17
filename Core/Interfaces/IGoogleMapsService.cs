using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IGoogleMapsService
    {
        string GetApiKey();
        string GetMapEmbedUrl(double latitude, double longitude, int zoom = 15);
        string GetStaticMapUrl(double latitude, double longitude, int zoom = 15, string size = "600x400");
    }

}
