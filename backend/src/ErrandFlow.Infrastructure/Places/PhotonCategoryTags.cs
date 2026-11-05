namespace ErrandFlow.Infrastructure.Places;

internal static class PhotonCategoryTags
{
    public static string GetTag(string category) => category switch
    {
        "pharmacy" => "amenity:pharmacy",
        "supermarket" => "shop:supermarket",
        "bakery" => "shop:bakery",
        "restaurant" => "amenity:restaurant",
        "cafe" => "amenity:cafe",
        "bank" => "amenity:bank",
        "atm" => "amenity:atm",
        "post_office" => "amenity:post_office",
        "hospital" => "amenity:hospital",
        "clinic" => "amenity:clinic",
        "dentist" => "amenity:dentist",
        "veterinary" => "amenity:veterinary",
        "hairdresser" => "shop:hairdresser",
        "hardware" => "shop:hardware",
        "clothes" => "shop:clothes",
        "books" => "shop:books",
        "convenience" => "shop:convenience",
        "fuel" => "amenity:fuel",
        _ => throw new ArgumentException("Unsupported place category.", nameof(category))
    };
}
