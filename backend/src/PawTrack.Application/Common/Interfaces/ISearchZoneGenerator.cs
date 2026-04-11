using PawTrack.Domain.LostPets;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Generates a grid of <see cref="SearchZone"/>s around a geographic centre point.
/// The grid is composed of approximately 300×300 m cells, forming a 7×7 coverage area.
/// </summary>
public interface ISearchZoneGenerator
{
    /// <summary>
    /// Generates a rectangular grid of <see cref="SearchZone"/>s centred on
    /// <paramref name="centerLat"/> / <paramref name="centerLng"/>.
    /// </summary>
    /// <param name="lostPetEventId">Foreign key for the owning lost-pet event.</param>
    /// <param name="centerLat">WGS-84 latitude of the last-seen location.</param>
    /// <param name="centerLng">WGS-84 longitude of the last-seen location.</param>
    /// <param name="cellSizeMetres">Side length (in metres) of each grid cell. Defaults to 300 m.</param>
    /// <param name="gridSize">Number of cells per side. Must be odd so the centre cell is centred. Defaults to 7.</param>
    IReadOnlyList<SearchZone> Generate(
        Guid lostPetEventId,
        double centerLat,
        double centerLng,
        int cellSizeMetres = 300,
        int gridSize = 7);
}
