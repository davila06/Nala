using PawTrack.Domain.Pets;

namespace PawTrack.Application.LostPets.SearchRadius;

public sealed class LostPetSearchRadiusCalculator : ILostPetSearchRadiusCalculator
{
    private static readonly string[] ActiveDogKeywords =
    [
        "labrador", "golden retriever", "golden", "husky", "siberian",
        "border collie", "collie", "australian shepherd", "pastor australiano",
        "german shepherd", "pastor alemán", "pastor aleman", "malinois",
        "doberman", "dobermann", "rottweiler", "weimaraner", "vizsla",
        "pointer", "setter", "dalmatian", "dálmata", "dalmata",
        "boxer", "pit bull", "pitbull", "american staffordshire", "stafford",
        "greyhound", "galgo", "whippet", "beagle", "jack russell",
        "springer spaniel", "cocker spaniel",
    ];

    private static readonly string[] SmallDogKeywords =
    [
        "chihuahua", "yorkshire", "yorkie", "maltese", "maltés", "maltes",
        "pomeranian", "pomerania", "pomeranio",
        "toy poodle", "poodle miniatura", "poodle toy",
        "shih tzu", "shih-tzu", "shihtzu",
        "cavalier", "bichon", "frisé", "frise",
        "papillon", "affenpinscher", "brussels griffon",
        "miniature dachshund", "salchicha miniatura",
        "chipin", "pomchi", "maltipoo",
    ];

    private static readonly IReadOnlyDictionary<string, int[]> RadiusMatrix = new Dictionary<string, int[]>
    {
        ["dog-active"] = [500, 750, 1_000, 1_500],
        ["dog-medium"] = [350, 525, 700, 875],
        ["dog-small"] = [200, 250, 320, 400],
        ["cat"] = [300, 375, 480, 600],
        ["rabbit"] = [50, 60, 75, 100],
        ["bird"] = [500, 750, 1_000, 1_500],
        ["other"] = [300, 375, 450, 600],
    };

    public int Calculate(
        PetSpecies species,
        string? breed,
        DateTimeOffset lastSeenAt,
        DateTimeOffset? referenceTime = null)
    {
        var hoursElapsed = Math.Max(0, ((referenceTime ?? DateTimeOffset.UtcNow) - lastSeenAt).TotalHours);
        var key = ResolveKey(species, breed);
        var bracket = GetTimeBracket(hoursElapsed);

        return RadiusMatrix[key][bracket];
    }

    private static string ResolveKey(PetSpecies species, string? breed) => species switch
    {
        PetSpecies.Dog => $"dog-{ClassifyDog(breed)}",
        PetSpecies.Cat => "cat",
        PetSpecies.Rabbit => "rabbit",
        PetSpecies.Bird => "bird",
        _ => "other",
    };

    private static string ClassifyDog(string? breed)
    {
        if (string.IsNullOrWhiteSpace(breed))
            return "medium";

        var normalizedBreed = breed.Trim().ToLowerInvariant();

        if (ActiveDogKeywords.Any(normalizedBreed.Contains))
            return "active";

        if (SmallDogKeywords.Any(normalizedBreed.Contains))
            return "small";

        return "medium";
    }

    private static int GetTimeBracket(double hoursElapsed) => hoursElapsed switch
    {
        < 2 => 0,
        < 6 => 1,
        < 24 => 2,
        _ => 3,
    };
}