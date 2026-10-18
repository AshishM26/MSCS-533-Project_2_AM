using SQLite;

namespace LocationTracker.Models;

/// <summary>A single device reading. Timestamps are stored and interpreted as UTC.</summary>
[Table("Locations")]
public sealed class LocationRecord
{
    /// <summary>Database-generated row identifier.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Latitude in decimal degrees (-90 to 90).</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees (-180 to 180).</summary>
    public double Longitude { get; set; }

    /// <summary>When the platform captured the reading, in UTC; indexed for ordered reloads.</summary>
    [Indexed]
    public DateTime TimestampUtc { get; set; }

    /// <summary>Reported horizontal accuracy in meters, or <see langword="null"/> when unknown.</summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>
    /// <see langword="true"/> when both coordinates are finite and within geographic bounds.
    /// Not stored; used to keep corrupt readings out of the database and the heat map.
    /// </summary>
    [Ignore]
    public bool HasValidCoordinates =>
        double.IsFinite(Latitude) && Latitude is >= -90 and <= 90 &&
        double.IsFinite(Longitude) && Longitude is >= -180 and <= 180;
}
