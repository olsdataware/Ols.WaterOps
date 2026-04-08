namespace WaterOps.Calculations.Models;

/// <summary>
/// Physical properties of a chemical product used in dose calculations.
/// </summary>
/// <param name="WeightPerGallon">
/// Weight of the product in lbs per gallon (e.g., 10.65 for sodium hypochlorite).
/// Must be greater than zero.
/// </param>
/// <param name="Concentration">
/// Active ingredient concentration as a decimal fraction (e.g., 0.125 for 12.5% bleach).
/// Must be greater than zero and at most 1.0.
/// </param>
public record Chemical(double WeightPerGallon, double Concentration)
{
    public double WeightPerGallon { get; init; } =
        WeightPerGallon > 0
            ? WeightPerGallon
            : throw new ArgumentOutOfRangeException(
                nameof(WeightPerGallon),
                "Chemical weight per gallon must be greater than zero."
            );

    public double Concentration { get; init; } =
        Concentration is > 0 and <= 1
            ? Concentration
            : throw new ArgumentOutOfRangeException(
                nameof(Concentration),
                "Chemical concentration must be between 0 (exclusive) and 1.0 (inclusive)."
            );
}
