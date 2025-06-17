using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Xml.Configs
{
    public class XmlDataSettings
    {
        public const string SectionName = "XmlDataSettings";

        public string FeedUrl { get; set; } = string.Empty;
        public int MaxItemsToLoad { get; set; } = 100;
        public int BatchSize { get; set; } = 20;
        public bool LoadFromEnd { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableXmlSeeding { get; set; } = true;
        public bool FallbackToStaticData { get; set; } = true;
    }
}
