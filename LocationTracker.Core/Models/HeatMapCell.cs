namespace LocationTracker.Models;

/// <summary>One aggregated heat-map area ready to be drawn as a map circle.</summary>
/// <param name="Latitude">Mean latitude of the samples in the cell.</param>
/// <param name="Longitude">Circular mean longitude of the samples in the cell.</param>
/// <param name="Count">Number of saved samples in the cell.</param>
/// <param name="Density">Count relative to the densest cell, from just above 0 to 1.</param>
/// <param name="RadiusMeters">Circle radius; denser cells are drawn larger.</param>
public sealed record HeatMapCell(
    double Latitude, double Longitude, int Count, double Density, double RadiusMeters);
