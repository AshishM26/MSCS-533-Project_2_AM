using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>Builds a density grid without depending on GPS or UI APIs.</summary>
public sealed class HeatMapService
{
    private const double EarthRadiusMeters = 6_371_000;
    private const double RadiansPerDegree = Math.PI / 180;

    /// <summary>Upper bound on returned cells, which keeps the number of map overlays responsive.</summary>
    public const int MaximumCells = 400;

    /// <summary>Groups valid readings into geographic cells and scores each cell's relative density.</summary>
    /// <param name="records">Saved readings; invalid coordinates are skipped.</param>
    /// <param name="cellSizeMeters">Target cell edge length; doubled until at most <see cref="MaximumCells"/> remain.</param>
    /// <returns>Cells in a stable latitude/longitude order, or an empty list when there are no valid readings.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cellSizeMeters"/> is not a positive finite number.</exception>
    public IReadOnlyList<HeatMapCell> Aggregate(
        IEnumerable<LocationRecord> records, double cellSizeMeters = 75)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (!double.IsFinite(cellSizeMeters) || cellSizeMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMeters));
        }

        var points = records.Where(record => record.HasValidCoordinates).ToArray();
        if (points.Length == 0)
        {
            return [];
        }

        Dictionary<(long Latitude, long Longitude), List<LocationRecord>> cells;
        do
        {
            cells = GroupPoints(points, cellSizeMeters);
            if (cells.Count <= MaximumCells)
            {
                break;
            }

            // Coarsen the grid instead of silently discarding history on long routes.
            cellSizeMeters *= 2;
        } while (true);

        int largestCount = cells.Values.Max(cell => cell.Count);
        return cells.OrderBy(cell => cell.Key.Latitude).ThenBy(cell => cell.Key.Longitude)
            .Select(cell =>
            {
                List<LocationRecord> samples = cell.Value;
                double density = (double)samples.Count / largestCount;
                // A circular mean avoids putting points near +/-180 degrees at Greenwich.
                double longitude = Math.Atan2(
                    samples.Average(sample => Math.Sin(sample.Longitude * RadiansPerDegree)),
                    samples.Average(sample => Math.Cos(sample.Longitude * RadiansPerDegree))) / RadiansPerDegree;
                // Radius grows from 50% to 85% of the cell size, so dense cells stand out without hiding neighbors.
                return new HeatMapCell(samples.Average(sample => sample.Latitude), longitude,
                    samples.Count, density, cellSizeMeters * (0.5 + 0.35 * density));
            }).ToArray();
    }

    // Splits the globe into latitude bands of roughly cellSizeMeters, then divides each band into
    // columns based on that band's circumference, so cells stay close to square away from the equator.
    private static Dictionary<(long, long), List<LocationRecord>> GroupPoints(
        IEnumerable<LocationRecord> points, double cellSizeMeters)
    {
        var cells = new Dictionary<(long, long), List<LocationRecord>>();
        long latitudeBands = Math.Max(1, (long)Math.Ceiling(Math.PI * EarthRadiusMeters / cellSizeMeters));
        double bandHeight = 180.0 / latitudeBands;
        foreach (LocationRecord point in points)
        {
            long band = Math.Min(latitudeBands - 1, (long)Math.Floor((point.Latitude + 90) / bandHeight));
            double bandLatitude = -90 + (band + 0.5) * bandHeight;
            double circumference = 2 * Math.PI * EarthRadiusMeters * Math.Cos(bandLatitude * RadiansPerDegree);
            long columns = Math.Max(1, (long)Math.Ceiling(circumference / cellSizeMeters));
            double normalizedLongitude = (point.Longitude + 360) % 360;
            // Center one cell on longitude 0 so +180 and -180 use the same grid.
            long column = (long)Math.Floor(normalizedLongitude / 360 * columns + 0.5) % columns;
            var key = (band, column);
            if (!cells.TryGetValue(key, out List<LocationRecord>? cell))
            {
                cell = [];
                cells.Add(key, cell);
            }

            cell.Add(point);
        }

        return cells;
    }
}
