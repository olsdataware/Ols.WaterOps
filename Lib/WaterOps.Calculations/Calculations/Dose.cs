namespace WaterOps.Calculations.Calculations;

using WaterOps.Calculations.Models;

/// <summary>
/// Represents a chemical dose value in one of several unit types and provides unit conversions.
/// </summary>
/// <remarks>
/// Construct with a specific nested record type (for example <see cref="Ml"/> or <see cref="Ppm"/>)
/// and call the target conversion method with a <see cref="Flow"/> and a <see cref="Chemical"/> instance.
/// </remarks>
public abstract record class Dose
{
    // 1 US gallon = 3785.41 mL
    private const double MlPerGallon = 3785.41;

    // Weight of water at 60°F in lbs/gallon — used in ppm dose calculations (flow × 8.34 = lbs/day)
    private const double WaterLbsPerGallon = 8.34;

    private const string NegativeValueError = "Dose value cannot be negative.";

    /// <summary>
    /// Dose value expressed in milliliters.
    /// </summary>
    public record Ml(double Value) : Dose;

    /// <summary>
    /// Dose value expressed in pounds.
    /// </summary>
    public record Lbs(double Value) : Dose;

    /// <summary>
    /// Dose value expressed in gallons.
    /// </summary>
    public record Gals(double Value) : Dose;

    /// <summary>
    /// Dose value expressed as concentration in ppm.
    /// </summary>
    public record Ppm(double Value) : Dose;

    /// <summary>
    /// Converts the current dose into milliliters.
    /// </summary>
    public double ToMl(Flow flow, Chemical chemical) =>
        this switch
        {
            // Already in mL.
            Ml ml => ml.Value >= 0
                ? ml.Value
                : throw new InvalidOperationException(NegativeValueError),

            // lbs -> gallons -> mL using chemical specific weight.
            Lbs lbs => lbs.Value >= 0
                ? lbs.Value * MlPerGallon / chemical.WeightPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            // gallons -> mL.
            Gals gals => gals.Value >= 0
                ? gals.Value * MlPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            // ppm -> lbs/day equivalent -> gallons -> mL.
            Ppm ppm => ppm.Value >= 0
                ? ppm.Value
                    * flow.ToMgd()
                    * WaterLbsPerGallon
                    / chemical.Concentration
                    * MlPerGallon
                    / chemical.WeightPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            _ => throw new InvalidOperationException("Unknown dose type."),
        };

    /// <summary>
    /// Converts the current dose into pounds.
    /// </summary>
    public double ToLbs(Flow flow, Chemical chemical) =>
        this switch
        {
            // mL -> gallons -> lbs.
            Ml ml => ml.Value >= 0
                ? ml.Value / MlPerGallon * chemical.WeightPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            // Already in lbs.
            Lbs lbs => lbs.Value >= 0
                ? lbs.Value
                : throw new InvalidOperationException(NegativeValueError),

            // gallons -> lbs.
            Gals gals => gals.Value >= 0
                ? gals.Value * chemical.WeightPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            // ppm with flow -> lbs/day.
            Ppm ppm => ppm.Value >= 0
                ? ppm.Value * flow.ToMgd() * WaterLbsPerGallon / chemical.Concentration
                : throw new InvalidOperationException(NegativeValueError),

            _ => throw new InvalidOperationException("Unknown dose type."),
        };

    /// <summary>
    /// Converts the current dose into gallons.
    /// </summary>
    public double ToGals(Flow flow, Chemical chemical) =>
        this switch
        {
            // mL -> gallons.
            Ml ml => ml.Value >= 0
                ? ml.Value / MlPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            // lbs -> gallons.
            Lbs lbs => lbs.Value >= 0
                ? lbs.Value / chemical.WeightPerGallon
                : throw new InvalidOperationException(NegativeValueError),

            // Already in gallons.
            Gals gals => gals.Value >= 0
                ? gals.Value
                : throw new InvalidOperationException(NegativeValueError),

            // ppm with flow -> gallons/day.
            Ppm ppm => ppm.Value >= 0
                ? ppm.Value
                    * flow.ToMgd()
                    * WaterLbsPerGallon
                    / (chemical.Concentration * chemical.WeightPerGallon)
                : throw new InvalidOperationException(NegativeValueError),

            _ => throw new InvalidOperationException("Unknown dose type."),
        };

    /// <summary>
    /// Converts the current dose into ppm.
    /// </summary>
    public double ToPpm(Flow flow, Chemical chemical) =>
        this switch
        {
            // mL -> gallons -> lbs -> ppm based on flow.
            Ml ml => ml.Value >= 0
                ? ml.Value
                    / MlPerGallon
                    * chemical.WeightPerGallon
                    * chemical.Concentration
                    / (flow.ToMgd() * WaterLbsPerGallon)
                : throw new InvalidOperationException(NegativeValueError),

            // lbs -> ppm based on flow.
            Lbs lbs => lbs.Value >= 0
                ? lbs.Value * chemical.Concentration / (flow.ToMgd() * WaterLbsPerGallon)
                : throw new InvalidOperationException(NegativeValueError),

            // gallons -> lbs -> ppm based on flow.
            Gals gals => gals.Value >= 0
                ? gals.Value
                    * chemical.WeightPerGallon
                    * chemical.Concentration
                    / (flow.ToMgd() * WaterLbsPerGallon)
                : throw new InvalidOperationException(NegativeValueError),

            // Already in ppm.
            Ppm ppm => ppm.Value >= 0
                ? ppm.Value
                : throw new InvalidOperationException(NegativeValueError),

            _ => throw new InvalidOperationException("Unknown dose type."),
        };
}
