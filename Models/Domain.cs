using System.ComponentModel.DataAnnotations;
namespace StockLentes.Models;

public class OpticalStore {
 public int Id { get; set; }
 [MaxLength(100)] public string Name { get; set; } = "";
 public bool IsDemo { get; set; }
}
public class StockRule {
 public int Id { get; set; }
 [MaxLength(100)] public string Name { get; set; } = "";
 public bool UsesSphere { get; set; }
 public bool UsesCylinder { get; set; }
 public bool UsesAdd { get; set; }
 public bool UsesBase { get; set; }
 public bool ManualBase { get; set; }
}
public class LensFamily {
 public int Id { get; set; }
 [MaxLength(100)] public string Name { get; set; } = "";
 [MaxLength(40)] public string Sector { get; set; } = "OPTICA";
 public int StockRuleId { get; set; }
 public StockRule Rule { get; set; } = null!;
}
public class LensProduct {
 public int Id { get; set; }
 [MaxLength(60)] public string Code { get; set; } = "";
 [MaxLength(160)] public string Name { get; set; } = "";
 public int LensFamilyId { get; set; }
 public LensFamily Family { get; set; } = null!;
 public bool TracksStock { get; set; } = true;
 public bool IsActive { get; set; } = true;
 public bool IsDemo { get; set; }
 public int LowStockHalfPairs { get; set; } = 2;
 public Guid Version { get; set; } = Guid.NewGuid();
}
public class StockBalance {
 public int Id { get; set; }
 public int LensProductId { get; set; }
 public LensProduct Product { get; set; } = null!;
 [MaxLength(180)] public string CombinationKey { get; set; } = "";
 // Graduations stored as integer hundredths; avoid floating point and provider-specific decimals.
 public int? Sphere100 { get; set; }
 public int? Cylinder100 { get; set; }
 public int? Add100 { get; set; }
 public int? Base100 { get; set; }
 // One unit = one physical lens = half a pair.
 public int QuantityHalfPairs { get; set; }
 public Guid Version { get; set; } = Guid.NewGuid();
}
public class LensOrder {
 public int Id { get; set; }
 public int? OpticalStoreId { get; set; }
 public OpticalStore? Store { get; set; }
 [MaxLength(60)] public string ExternalNumber { get; set; } = "";
 [MaxLength(160)] public string PatientName { get; set; } = "";
 [MaxLength(30)] public string Status { get; set; } = "PENDIENTE";
 public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
 public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
 public string OriginalHeaderJson { get; set; } = "";
 public string HeaderReviewReason { get; set; } = "";
 public Guid Version { get; set; } = Guid.NewGuid();
 public ICollection<OrderLens> Lenses { get; set; } = new List<OrderLens>();
}
public class OrderLens {
 public int Id { get; set; }
 public int LensOrderId { get; set; }
 public LensOrder Order { get; set; } = null!;
 [MaxLength(2)] public string Eye { get; set; } = "OD";
 [MaxLength(30)] public string PairType { get; set; } = "LEJOS";
 public int PairNumber { get; set; } = 1;
 public int? RequestedProductId { get; set; }
 public LensProduct? RequestedProduct { get; set; }
 [MaxLength(60)] public string OriginalProductCode { get; set; } = "";
 [MaxLength(160)] public string OriginalProductName { get; set; } = "";
 public int? Sphere100 { get; set; }
 public int? Cylinder100 { get; set; }
 public int? Axis { get; set; }
 public int? Add100 { get; set; }
 public int? Base100 { get; set; }
 // Initial input is immutable. RawInputJson is the latest operator interpretation/correction.
 public string OriginalInputJson { get; set; } = "";
 public string RawInputJson { get; set; } = "";
 public string InterpretationNote { get; set; } = "";
 public int? SelectedProductId { get; set; }
 public int? SelectedBase100 { get; set; }
 public int? SelectedStockId { get; set; }
 public string SelectionReason { get; set; } = "";
 public Guid Version { get; set; } = Guid.NewGuid();
 public Guid? PrescriptionGroupId { get; set; }
 public bool RequiresUsedPrescription { get; set; }
 public bool UsesActualGraduation { get; set; }
 public int? UsedSphere100 { get; set; }
 public int? UsedCylinder100 { get; set; }
 public int? UsedAdd100 { get; set; }
 public int? UsedAxis { get; set; }
 public ICollection<PrescriptionSection> PrescriptionSections { get; set; } = new List<PrescriptionSection>();
}
// Recipe distances describe one physical lens; they never create movements by themselves.
public class PrescriptionSection {
 public int Id { get; set; }
 public int OrderLensId { get; set; }
 public OrderLens Lens { get; set; } = null!;
 public string Section { get; set; } = "";
 public string? Sphere { get; set; }
 public string? Cylinder { get; set; }
 public string? Axis { get; set; }
}
public class StockMovement {
 public int Id { get; set; }
 // Nullable for inventory adjustments. Unique when recording a used order lens.
 public int? OrderLensId { get; set; }
 public OrderLens? OrderLens { get; set; }
 public int ActualProductId { get; set; }
 public LensProduct ActualProduct { get; set; } = null!;
 public int? StockBalanceId { get; set; }
 public StockBalance? Balance { get; set; }
 [MaxLength(36)] public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();
 [MaxLength(30)] public string Kind { get; set; } = "AJUSTE";
 public int QuantityHalfPairs { get; set; }
 public int? StockBeforeHalfPairs { get; set; }
 public int? StockAfterHalfPairs { get; set; }
 public int? ActualSphere100 { get; set; }
 public int? ActualCylinder100 { get; set; }
 public int? ActualAxis { get; set; }
 public int? ActualAdd100 { get; set; }
 public int? SuggestedBase100 { get; set; }
 public int? ActualBase100 { get; set; }
 public bool ManualSelection { get; set; }
 [MaxLength(300)] public string Reason { get; set; } = "";
 // Immutable snapshot of requested lens/order/store/patient at confirmation (stage 2).
 public string RequestedSnapshotJson { get; set; } = "{}";
 [MaxLength(160)] public string ActualProductName { get; set; } = "";
 [MaxLength(60)] public string ActualProductCode { get; set; } = "";
 public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
