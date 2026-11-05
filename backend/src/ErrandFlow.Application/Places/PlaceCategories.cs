using System.Globalization;
using System.Text;

namespace ErrandFlow.Application.Places;

public static class PlaceCategories
{
    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["farmacia"] = "pharmacy",
        ["farmacias"] = "pharmacy",
        ["pharmacies"] = "pharmacy",
        ["pharmacy"] = "pharmacy",
        ["supermarket"] = "supermarket",
        ["supermarkets"] = "supermarket",
        ["supermercado"] = "supermarket",
        ["supermercados"] = "supermarket",
        ["bakeries"] = "bakery",
        ["bakery"] = "bakery",
        ["panaderia"] = "bakery",
        ["panaderias"] = "bakery",
        ["restaurant"] = "restaurant",
        ["restaurante"] = "restaurant",
        ["restaurantes"] = "restaurant",
        ["restaurants"] = "restaurant",
        ["cafe"] = "cafe",
        ["cafes"] = "cafe",
        ["cafeteria"] = "cafe",
        ["cafeterias"] = "cafe",
        ["banco"] = "bank",
        ["bancos"] = "bank",
        ["bank"] = "bank",
        ["banks"] = "bank",
        ["atm"] = "atm",
        ["atms"] = "atm",
        ["cajero"] = "atm",
        ["cajeros"] = "atm",
        ["correos"] = "post_office",
        ["oficina de correos"] = "post_office",
        ["post office"] = "post_office",
        ["post offices"] = "post_office",
        ["post_office"] = "post_office",
        ["hospital"] = "hospital",
        ["hospitales"] = "hospital",
        ["hospitals"] = "hospital",
        ["clinic"] = "clinic",
        ["clinica"] = "clinic",
        ["clinicas"] = "clinic",
        ["clinics"] = "clinic",
        ["dentist"] = "dentist",
        ["dentista"] = "dentist",
        ["dentistas"] = "dentist",
        ["dentists"] = "dentist",
        ["vet"] = "veterinary",
        ["veterinaria"] = "veterinary",
        ["veterinarias"] = "veterinary",
        ["veterinario"] = "veterinary",
        ["veterinarios"] = "veterinary",
        ["veterinary"] = "veterinary",
        ["vets"] = "veterinary",
        ["hairdresser"] = "hairdresser",
        ["hairdressers"] = "hairdresser",
        ["peluqueria"] = "hairdresser",
        ["peluquerias"] = "hairdresser",
        ["ferreteria"] = "hardware",
        ["ferreterias"] = "hardware",
        ["hardware"] = "hardware",
        ["hardware store"] = "hardware",
        ["hardware stores"] = "hardware",
        ["clothes"] = "clothes",
        ["clothing store"] = "clothes",
        ["clothing stores"] = "clothes",
        ["ropa"] = "clothes",
        ["tienda de ropa"] = "clothes",
        ["tiendas de ropa"] = "clothes",
        ["books"] = "books",
        ["bookshop"] = "books",
        ["bookshops"] = "books",
        ["bookstore"] = "books",
        ["bookstores"] = "books",
        ["libreria"] = "books",
        ["librerias"] = "books",
        ["convenience"] = "convenience",
        ["convenience store"] = "convenience",
        ["convenience stores"] = "convenience",
        ["tienda de alimentacion"] = "convenience",
        ["tiendas de alimentacion"] = "convenience",
        ["ultramarinos"] = "convenience",
        ["fuel"] = "fuel",
        ["gas station"] = "fuel",
        ["gas stations"] = "fuel",
        ["gasolinera"] = "fuel",
        ["gasolineras"] = "fuel",
        ["petrol station"] = "fuel",
        ["petrol stations"] = "fuel",
    };

    public static string? Resolve(string value)
    {
        var normalized = new string(value.Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();
        normalized = string.Join(" ", normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return Aliases.TryGetValue(normalized, out var category) ? category : null;
    }
}
