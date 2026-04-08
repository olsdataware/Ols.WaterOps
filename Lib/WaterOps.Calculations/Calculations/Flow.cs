namespace WaterOps.Calculations.Calculations;

/// <summary>
/// Represents a flow value in one of several units and provides unit conversions.
/// </summary>
/// <remarks>
/// Construct with a specific nested record type (for example <see cref="Gpm"/> or <see cref="Mgd"/>)
/// and call the target conversion method.
/// </remarks>
public abstract record class Flow
{
    private const string NonPositiveError =
        "Flow must be strictly greater than zero for dose calculations.";

    /// <summary>
    /// Flow value expressed in gallons per minute.
    /// </summary>
    public record Gpm(double Value) : Flow;

    /// <summary>
    /// Flow value expressed in gallons per hour.
    /// </summary>
    public record Gph(double Value) : Flow;

    /// <summary>
    /// Flow value expressed in gallons per day.
    /// </summary>
    public record Gpd(double Value) : Flow;

    /// <summary>
    /// Flow value expressed in million gallons per day.
    /// </summary>
    public record Mgd(double Value) : Flow;

    /// <summary>
    /// Converts the current flow value into gallons per minute.
    /// </summary>
    public double ToGpm() =>
        this switch
        {
            // Already in GPM.
            Gpm gpm => gpm.Value > 0
                ? gpm.Value
                : throw new InvalidOperationException(NonPositiveError),

            // GPH -> GPM.
            Gph gph => gph.Value > 0
                ? gph.Value / 60
                : throw new InvalidOperationException(NonPositiveError),

            // GPD -> GPM.
            Gpd gpd => gpd.Value > 0
                ? gpd.Value / 1440
                : throw new InvalidOperationException(NonPositiveError),

            // MGD -> GPM.
            Mgd mgd => mgd.Value > 0
                ? mgd.Value * 1000000 / 1440
                : throw new InvalidOperationException(NonPositiveError),

            _ => throw new InvalidOperationException("Unknown flow type."),
        };

    /// <summary>
    /// Converts the current flow value into gallons per hour.
    /// </summary>
    public double ToGph() =>
        this switch
        {
            // GPM -> GPH.
            Gpm gpm => gpm.Value > 0
                ? gpm.Value * 60
                : throw new InvalidOperationException(NonPositiveError),

            // Already in GPH.
            Gph gph => gph.Value > 0
                ? gph.Value
                : throw new InvalidOperationException(NonPositiveError),

            // GPD -> GPH.
            Gpd gpd => gpd.Value > 0
                ? gpd.Value / 24
                : throw new InvalidOperationException(NonPositiveError),

            // MGD -> GPH.
            Mgd mgd => mgd.Value > 0
                ? mgd.Value * 1000000 / 24
                : throw new InvalidOperationException(NonPositiveError),

            _ => throw new InvalidOperationException("Unknown flow type."),
        };

    /// <summary>
    /// Converts the current flow value into gallons per day.
    /// </summary>
    public double ToGpd() =>
        this switch
        {
            // GPM -> GPD.
            Gpm gpm => gpm.Value > 0
                ? gpm.Value * 1440
                : throw new InvalidOperationException(NonPositiveError),

            // GPH -> GPD.
            Gph gph => gph.Value > 0
                ? gph.Value * 24
                : throw new InvalidOperationException(NonPositiveError),

            // Already in GPD.
            Gpd gpd => gpd.Value > 0
                ? gpd.Value
                : throw new InvalidOperationException(NonPositiveError),

            // MGD -> GPD.
            Mgd mgd => mgd.Value > 0
                ? mgd.Value * 1000000
                : throw new InvalidOperationException(NonPositiveError),

            _ => throw new InvalidOperationException("Unknown flow type."),
        };

    /// <summary>
    /// Converts the current flow value into million gallons per day.
    /// </summary>
    public double ToMgd() =>
        this switch
        {
            // GPM -> MGD.
            Gpm gpm => gpm.Value > 0
                ? gpm.Value * 1440 / 1000000
                : throw new InvalidOperationException(NonPositiveError),

            // GPH -> MGD.
            Gph gph => gph.Value > 0
                ? gph.Value * 24 / 1000000
                : throw new InvalidOperationException(NonPositiveError),

            // GPD -> MGD.
            Gpd gpd => gpd.Value > 0
                ? gpd.Value / 1000000
                : throw new InvalidOperationException(NonPositiveError),

            // Already in MGD.
            Mgd mgd => mgd.Value > 0
                ? mgd.Value
                : throw new InvalidOperationException(NonPositiveError),

            _ => throw new InvalidOperationException("Unknown flow type."),
        };
}
