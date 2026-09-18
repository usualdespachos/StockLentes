using System.Text.Json;
using StockLentes.Models;
namespace StockLentes.Services;

public static class MovementDisplay {
 public static string Kind(string kind)=>kind switch {
  "BAJA"=>"BAJA POR PEDIDO", "BAJA_SIN_STOCK_CONTROLADO"=>"USO SIN CONTROL DE STOCK",
  "AJUSTE"=>"AJUSTE MANUAL", "ENTRADA"=>"ENTRADA", "INICIAL"=>"CARGA INICIAL", _=>kind};
 public static string Combination(StockMovement m)=>string.Join(" · ",new[]{
  m.ActualSphere100.HasValue?"ESF "+LensValues.Grade(m.ActualSphere100):null,
  m.ActualCylinder100.HasValue?"CIL "+LensValues.Grade(m.ActualCylinder100):null,
  m.ActualAxis.HasValue?"EJE "+m.ActualAxis:null,
  m.ActualAdd100.HasValue?"ADD "+LensValues.Grade(m.ActualAdd100):null,
  m.ActualBase100.HasValue?"BASE "+LensValues.Grade(m.ActualBase100):null}.Where(x=>x!=null));
 public static LensEntry? Original(StockMovement movement){
  try{
   using var snapshot=JsonDocument.Parse(movement.RequestedSnapshotJson);
   if(snapshot.RootElement.TryGetProperty("OriginalInput",out var input)&&input.ValueKind==JsonValueKind.String&&!string.IsNullOrEmpty(input.GetString()))return JsonSerializer.Deserialize<LensEntry>(input.GetString()!);
  }catch(JsonException){ /* Older movements may not contain a recipe snapshot. */ }
  return null;
 }
}
