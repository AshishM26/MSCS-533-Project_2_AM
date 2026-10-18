using LocationTracker.Models;
using LocationTracker.Services;

namespace LocationTracker.Tests;

public sealed class HeatMapServiceTests
{
    private readonly HeatMapService _service = new();

    [Fact]
    public void EmptyHistoryProducesNoOverlays() => Assert.Empty(_service.Aggregate([]));

    [Fact]
    public void OnePointRetainsCoordinatesAndNormalizesToOne()
    {
        HeatMapCell cell = Assert.Single(_service.Aggregate([Point(36.737, -84.164)]));
        Assert.Equal(36.737, cell.Latitude, 6);
        Assert.Equal(-84.164, cell.Longitude, 6);
        Assert.Equal(1, cell.Count);
        Assert.Equal(1, cell.Density);
        Assert.InRange(cell.RadiusMeters, 30, 100);
    }

    [Fact]
    public void NearbyPointsShareACell()
    {
        HeatMapCell cell = Assert.Single(_service.Aggregate([Point(0, 0), Point(0.000001, 0.000001)]));
        Assert.Equal(2, cell.Count);
        Assert.Equal(0.0000005, cell.Latitude, 8);
    }

    [Fact]
    public void SeparateCellsNormalizeAgainstDensestCell()
    {
        var cells = _service.Aggregate([Point(0, 0), Point(0, 0), Point(1, 1)]);
        Assert.Equal(2, cells.Count);
        Assert.Equal(1, cells.Single(cell => cell.Count == 2).Density);
        Assert.Equal(0.5, cells.Single(cell => cell.Count == 1).Density);
        Assert.True(cells.Single(cell => cell.Count == 2).RadiusMeters > cells.Single(cell => cell.Count == 1).RadiusMeters);
    }

    [Fact]
    public void InvalidCoordinatesAreIgnored()
    {
        Assert.Empty(_service.Aggregate([Point(double.NaN, 0), Point(91, 0), Point(0, -181), Point(0, double.PositiveInfinity)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void InvalidCellSizeIsRejected(double size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Aggregate([], size));

    [Fact]
    public void EquivalentAntimeridianCoordinatesShareACell()
    {
        HeatMapCell cell = Assert.Single(_service.Aggregate([Point(0, -180), Point(0, 180)]));
        Assert.Equal(180, Math.Abs(cell.Longitude), 6);
        Assert.Equal(2, cell.Count);
    }

    [Fact]
    public void LargeHistoryIsBoundedWithoutLosingSampleCounts()
    {
        var samples = Enumerable.Range(0, 3000).Select(index => Point(-70 + index * 0.04, -170 + index * 0.1)).ToArray();
        var cells = _service.Aggregate(samples);
        Assert.InRange(cells.Count, 1, HeatMapService.MaximumCells);
        Assert.Equal(samples.Length, cells.Sum(cell => cell.Count));
    }

    private static LocationRecord Point(double latitude, double longitude) => new()
    {
        Latitude = latitude,
        Longitude = longitude,
        TimestampUtc = DateTime.UtcNow
    };
}
