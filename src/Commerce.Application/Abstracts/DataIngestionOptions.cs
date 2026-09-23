namespace Commerce.Application.Abstracts;

public sealed class DataIngestionOptions
{
    public const string SectionName = "DataIngestion";

    public int DispatchIntervalMinutes { get; set; } = 15;

    /// <summary>Max products to ingest per run. Null or &lt;= 0 means no limit (fetch all).</summary>
    public int? Products { get; set; }

    /// <summary>Max orders to ingest per run. Null or &lt;= 0 means no limit (fetch all).</summary>
    public int? Orders { get; set; }

    public int? EffectiveProductLimit => PositiveOrNull(Products);

    public int? EffectiveOrderLimit => PositiveOrNull(Orders);

    private static int? PositiveOrNull(int? value)
        => value is > 0 ? value : null;
}
