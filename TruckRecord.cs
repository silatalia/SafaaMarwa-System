using SQLite;

namespace SafaaAndMarwaSystem.Models;

[Table("TruckRecords")]
public class TruckRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string CarNumber { get; set; } = "";
    public string CarLetters { get; set; } = "";
    public string TrailerNumber { get; set; } = "";
    public string TrailerLetters { get; set; } = "";

    public double CarLength { get; set; }
    public double CarWidth { get; set; }
    public double CarHeight { get; set; }

    public double TrailerLength { get; set; }
    public double TrailerWidth { get; set; }
    public double TrailerHeight { get; set; }

    public double GrossVolume { get; set; }
    public double NetVolume { get; set; }
    public double CustomDeduction { get; set; }
    public double FinalVolume { get; set; }

    public DateTime RecordDate { get; set; } = DateTime.Today;
    public string ClientName { get; set; } = "";
    public string MeasurerName { get; set; } = "";

    public string ExtraPosition { get; set; } = "لا يوجد";
    public string ExtraNotes { get; set; } = "";

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedDevice { get; set; } = "";

    public void Recalculate()
    {
        GrossVolume = Math.Max(0, CarLength) * Math.Max(0, CarWidth) * Math.Max(0, CarHeight)
                    + Math.Max(0, TrailerLength) * Math.Max(0, TrailerWidth) * Math.Max(0, TrailerHeight);

        NetVolume = GrossVolume;
        CustomDeduction = Math.Max(0, CustomDeduction);
        FinalVolume = Math.Max(0, NetVolume - CustomDeduction);
    }
}
