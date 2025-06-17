using System.Xml.Serialization;

[XmlRoot("response")]
public class XmlResponse
{
    [XmlElement("item")]
    public List<XmlRealEstateItem> Items { get; set; } = new List<XmlRealEstateItem>();
}

public class XmlRealEstateItem
{
    [XmlAttribute("internal-id")]
    public string? InternalId { get; set; }

    [XmlElement("status")]
    public string? Status { get; set; }

    [XmlElement("is_new_building")]
    public string? IsNewBuildingString { get; set; }

    [XmlElement("title")]
    public string? Title { get; set; }

    [XmlElement("description")]
    public string? Description { get; set; }

    [XmlElement("category")]
    public XmlCategory? Category { get; set; }

    [XmlElement("realty_type")]
    public XmlRealtyType? RealtyType { get; set; }

    [XmlElement("deal")]
    public XmlDeal? Deal { get; set; }

    [XmlElement("location")]
    public XmlLocation? Location { get; set; }

    [XmlElement("total_floors")]
    public string? TotalFloorsString { get; set; }

    [XmlElement("floor")]
    public string? FloorString { get; set; }

    [XmlElement("area_total")]
    public string? AreaTotalString { get; set; }

    [XmlElement("area_living")]
    public string? AreaLivingString { get; set; }

    [XmlElement("area_kitchen")]
    public string? AreaKitchenString { get; set; }

    [XmlElement("room_count")]
    public string? RoomCountString { get; set; }

    [XmlElement("newbuilding_name")]
    public string? NewBuildingName { get; set; }

    [XmlElement("price")]
    public XmlPrice? Price { get; set; }

    [XmlElement("images")]
    public XmlImages? Images { get; set; }

    [XmlElement("created_at")]
    public string? CreatedAtString { get; set; }

    [XmlElement("updated_at")]
    public string? UpdatedAtString { get; set; }

    // Helper properties with safe parsing
    public bool IsNewBuilding => ParseBool(IsNewBuildingString);
    public int TotalFloors => ParseInt(TotalFloorsString);
    public int Floor => ParseInt(FloorString);
    public float AreaTotal => ParseFloat(AreaTotalString);
    public float? AreaLiving => ParseNullableFloat(AreaLivingString);
    public float? AreaKitchen => ParseNullableFloat(AreaKitchenString);
    public int RoomCount => ParseInt(RoomCountString, 1); // Default to 1 room
    public DateTime? CreatedAt => ParseDateTime(CreatedAtString);
    public DateTime? UpdatedAt => ParseDateTime(UpdatedAtString);

    private static bool ParseBool(string? value)
    {
        return value == "1" || value?.ToLower() == "true";
    }

    private static int ParseInt(string? value, int defaultValue = 0)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static float ParseFloat(string? value, float defaultValue = 0f)
    {
        return float.TryParse(value, out var result) ? result : defaultValue;
    }

    private static float? ParseNullableFloat(string? value)
    {
        return float.TryParse(value, out var result) ? result : null;
    }

    private static DateTime? ParseDateTime(string? value)
    {
        return DateTime.TryParse(value, out var result) ? result : null;
    }
}

public class XmlCategory
{
    [XmlAttribute("value")]
    public string? Value { get; set; }

    [XmlText]
    public string? Text { get; set; }

    public int ValueInt => int.TryParse(Value, out var result) ? result : 1;
}

public class XmlRealtyType
{
    [XmlAttribute("value")]
    public string? Value { get; set; }

    [XmlText]
    public string? Text { get; set; }

    public int ValueInt => int.TryParse(Value, out var result) ? result : 1;
}

public class XmlDeal
{
    [XmlAttribute("value")]
    public string? Value { get; set; }

    [XmlText]
    public string? Text { get; set; }

    public int ValueInt => int.TryParse(Value, out var result) ? result : 2; // Default to rent
}

public class XmlLocation
{
    [XmlElement("country")]
    public XmlLocationItem? Country { get; set; }

    [XmlElement("region")]
    public XmlLocationItem? Region { get; set; }

    [XmlElement("city")]
    public XmlLocationItem? City { get; set; }

    [XmlElement("borough")]
    public XmlLocationItem? Borough { get; set; }

    [XmlElement("street")]
    public XmlLocationItem? Street { get; set; }

    [XmlElement("street_type")]
    public XmlLocationItem? StreetType { get; set; }

    [XmlElement("map_lat")]
    public string? MapLatString { get; set; }

    [XmlElement("map_lng")]
    public string? MapLngString { get; set; }

    public double? MapLat => double.TryParse(MapLatString, out var result) ? result : null;
    public double? MapLng => double.TryParse(MapLngString, out var result) ? result : null;
}

public class XmlLocationItem
{
    [XmlAttribute("value")]
    public string? Value { get; set; }

    [XmlText]
    public string? Text { get; set; }
}

public class XmlPrice
{
    [XmlAttribute("currency")]
    public string? Currency { get; set; }

    [XmlText]
    public string? ValueString { get; set; }

    public decimal Value => decimal.TryParse(ValueString, out var result) ? result : 0;
}

public class XmlImages
{
    [XmlElement("image_url")]
    public List<string> ImageUrls { get; set; } = new List<string>();
}