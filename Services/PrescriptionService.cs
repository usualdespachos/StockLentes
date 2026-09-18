using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockLentes.Models;

namespace StockLentes.Services;

public class EyePrescription {
 public string? LejosSphere {get;set;}
 public string? LejosCylinder {get;set;}
 public string? LejosAxis {get;set;}
 public string? Add {get;set;}
 public string? IntermediaSphere {get;set;}
 public string? IntermediaCylinder {get;set;}
 public string? IntermediaAxis {get;set;}
 public string? CercaSphere {get;set;}
 public string? CercaCylinder {get;set;}
 public string? CercaAxis {get;set;}
 public string? Base {get;set;}
 public List<SectionEntry> Sections()=>[
  new("LEJOS",LejosSphere,LejosCylinder,LejosAxis),
  new("INTERMEDIA",IntermediaSphere,IntermediaCylinder,IntermediaAxis),
  new("CERCA",CercaSphere,CercaCylinder,CercaAxis)];
}
public class GlassesEntry {
 public Guid EntryId {get;set;}=Guid.NewGuid();
 public int? ProductId {get;set;}
 public string? Code {get;set;}
 public string? Name {get;set;}
 public bool IncludeOD {get;set;}=true;
 public bool IncludeOI {get;set;}=true;
 public EyePrescription OD {get;set;}=new();
 public EyePrescription OI {get;set;}=new();
}

public partial class OrderService {
 public async Task<int> SaveGlassesAsync(int orderId,GlassesEntry input){
  if(input.EntryId==Guid.Empty)throw new InvalidOperationException("Volvé a abrir el formulario para obtener una identificación de carga.");
  var saved=new List<(OrderLens lens,LensEntry entry)>();
  await using(var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)){
   var order=await db.Orders.FindAsync(orderId)??throw new InvalidOperationException("Pedido inexistente.");
   // Reposting either save button must not create another pair.
   var existing=await db.OrderLenses.Where(x=>x.LensOrderId==orderId&&x.PrescriptionGroupId==input.EntryId).CountAsync();
   if(existing>0)return existing;
   if(order.Status!="PENDIENTE")throw new InvalidOperationException("El pedido está cerrado.");
   var pair=(await db.OrderLenses.Where(x=>x.LensOrderId==orderId).MaxAsync(x=>(int?)x.PairNumber)??0)+1;
   var product=await db.Products.SingleOrDefaultAsync(x=>x.Id==input.ProductId&&x.IsActive);
   // No eye marked means an incomplete pair, not a discarded recipe.
   var bothMissing=!input.IncludeOD&&!input.IncludeOI;
   foreach(var (eye,recipe,include) in new[]{("OD",input.OD,input.IncludeOD||bothMissing),("OI",input.OI,input.IncludeOI||bothMissing)}){
    if(!include)continue;
    var sections=recipe.Sections();
    var primary=sections.FirstOrDefault(x=>!string.IsNullOrWhiteSpace(x.Sphere)||!string.IsNullOrWhiteSpace(x.Cylinder)||!string.IsNullOrWhiteSpace(x.Axis))??sections[0];
    var entry=new LensEntry{ProductId=product?.Id,Code=product?.Code??input.Code,Name=product?.Name??input.Name,
     Eye=eye,Section="ANTEOJO",Pair=pair.ToString(System.Globalization.CultureInfo.InvariantCulture),
     Sphere=primary.Sphere,Cylinder=primary.Cylinder,Axis=primary.Axis,Add=recipe.Add,Base=recipe.Base,Sections=sections};
    var raw=JsonSerializer.Serialize(entry);
    var lens=new OrderLens{LensOrderId=orderId,PrescriptionGroupId=input.EntryId,Eye=eye,PairNumber=pair,PairType="ANTEOJO",
     RawInputJson=raw,OriginalInputJson=raw,InterpretationNote="Interpretación pendiente"};
    foreach(var s in sections)lens.PrescriptionSections.Add(new(){Section=s.Section,Sphere=s.Sphere,Cylinder=s.Cylinder,Axis=s.Axis});
    db.OrderLenses.Add(lens);saved.Add((lens,entry));
   }
   order.Version=Guid.NewGuid();
   await db.SaveChangesAsync();await tx.CommitAsync();
  }
  // The full raw prescription is already committed before catalog/number interpretation.
  foreach(var (lens,entry) in saved)await InterpretAsync(lens,entry);
  return saved.Count;
 }

 private static bool DifferentDistanceGrades(LensEntry entry){
  if(entry.Sections==null)return false;
  string Canonical(string? value){var issues=new List<string>();var parsed=LensEntry.Grade(value,"valor",issues);return issues.Count==0?parsed?.ToString()??"":value?.Trim()??"";}
  return entry.Sections.Where(x=>!string.IsNullOrWhiteSpace(x.Sphere)||!string.IsNullOrWhiteSpace(x.Cylinder))
   .Select(x=>Canonical(x.Sphere)+"|"+Canonical(x.Cylinder)).Distinct().Count()>1;
 }
}
