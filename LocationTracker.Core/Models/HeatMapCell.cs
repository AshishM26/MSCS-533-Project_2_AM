namespace LocationTracker.Models;

public sealed record HeatMapCell(
    double Latitude, double Longitude, int Count, double Density, double RadiusMeters);
