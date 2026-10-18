using SQLite;

namespace LocationTracker.Models;

/// <summary>A single device reading. Timestamps are stored and interpreted as UTC.</summary>
[Table("Locations")]
public sealed class LocationRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [Indexed]
    public DateTime TimestampUtc { get; set; }

    public double? AccuracyMeters { get; set; }

    [Ignore]
    public bool HasValidCoordinates =>
        double.IsFinite(Latitude) && Latitude is >= -90 and <= 90 &&
        double.IsFinite(Longitude) && Longitude is >= -180 and <= 180;
}
