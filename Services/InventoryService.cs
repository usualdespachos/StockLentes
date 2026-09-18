using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Services;
public static class LensValues {
 public static string NormalizeCode(string code) => code.Trim().TrimEnd('*').Trim().ToUpperInvariant();
 public static string Pairs(int halves) => (halves/2m).ToString("0.##",CultureInfo.GetCultureInfo("es-AR"));
 public static string Grade(int? hundredths) => hundredths.HasValue ? (hundredths.Value/100m).ToString("0.00",CultureInfo.GetCultureInfo("es-AR")) : "—";
 public static int? Hundredths(decimal? value) {
  if(value is null) return null;
  if(value < -100 || value > 100 || value*100 != decimal.Truncate(value.Value*100)) throw new InvalidOperationException("La graduación debe estar entre -100 y 100 y tener hasta dos decimales.");
  return (int)(value.Value*100);
 }
 public static int Halves(decimal pairs) {
  if(pairs<0 || pairs>100000 || pairs*2!=decimal.Truncate(pairs*2)) throw new InvalidOperationException("Ingresá una cantidad no negativa en pasos de 0,5 pares.");
  return (int)(pairs*2);
 }
 public static string Key(StockRule rule,int? esf,int? cil,int? add,int? basis) {
  var parts=new List<string>();
  void Part(bool required,string name,int? value) {
   if(!required && value.HasValue) throw new InvalidOperationException($"La regla de este producto no utiliza {name}.");
   if(required) {if(!value.HasValue)throw new InvalidOperationException($"Falta {name} para esta familia.");parts.Add(name+"="+value.Value.ToString(CultureInfo.InvariantCulture));}
  }
  Part(rule.UsesSphere,"ESF",esf);Part(rule.UsesCylinder,"CIL",cil);Part(rule.UsesAdd,"ADD",add);Part(rule.UsesBase,"BASE",basis);
  return parts.Count==0?"UNICA":string.Join("|",parts);
 }
}
public class InventoryService(StockDbContext db) {
 public async Task SaveAsync(int? id,int productId,decimal? sphere,decimal? cylinder,decimal? add,decimal? basis,decimal pairs,string reason,Guid version,string operationKey,bool entry=false) {
  if(string.IsNullOrWhiteSpace(reason))throw new InvalidOperationException("Indicá el motivo de la carga o ajuste.");
  if(reason.Length>300)throw new InvalidOperationException("El motivo admite hasta 300 caracteres.");
  if(entry&&(!id.HasValue||pairs<=0))throw new InvalidOperationException("La entrada requiere una combinación existente y una cantidad mayor que cero.");
  if(!Guid.TryParse(operationKey,out _))throw new InvalidOperationException("Formulario inválido. Volvé a abrirlo.");
  await using var tx=await db.Database.BeginTransactionAsync();
  if(await db.Movements.AnyAsync(x=>x.IdempotencyKey==operationKey))return;
  var product=await db.Products.Include(x=>x.Family).ThenInclude(x=>x.Rule).SingleOrDefaultAsync(x=>x.Id==productId)
      ?? throw new InvalidOperationException("Producto inexistente.");
  if(!product.TracksStock || !product.IsActive)throw new InvalidOperationException("Este producto no admite carga de stock.");
  var s=LensValues.Hundredths(sphere);var c=LensValues.Hundredths(cylinder);var a=LensValues.Hundredths(add);var b=LensValues.Hundredths(basis);
  var key=LensValues.Key(product.Family.Rule,s,c,a,b);var quantity=LensValues.Halves(pairs);
  StockBalance balance;int before;
  if(id.HasValue) {
   balance=await db.Stock.SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("La combinación ya no existe.");
   if(balance.LensProductId!=productId || balance.CombinationKey!=key)throw new InvalidOperationException("La combinación no se puede cambiar en un ajuste.");
   if(balance.Version!=version)throw new InvalidOperationException("Otro usuario cambió el stock. Volvé a abrir el ajuste.");
   before=balance.QuantityHalfPairs;
  } else {
   if(await db.Stock.AnyAsync(x=>x.LensProductId==productId&&x.CombinationKey==key))throw new InvalidOperationException("Esta combinación ya existe. Usá Ajustar.");
   balance=new StockBalance{LensProductId=productId,CombinationKey=key,Sphere100=s,Cylinder100=c,Add100=a,Base100=b};db.Stock.Add(balance);before=0;
  }
  if(entry)quantity=LensValues.Halves((before+quantity)/2m);
  balance.QuantityHalfPairs=quantity;balance.Version=Guid.NewGuid();
  db.Movements.Add(new StockMovement{Balance=balance,ActualProductId=productId,ActualProductName=product.Name,ActualProductCode=product.Code,Kind=entry?"ENTRADA":id.HasValue?"AJUSTE":"INICIAL",QuantityHalfPairs=quantity-before,StockBeforeHalfPairs=before,StockAfterHalfPairs=quantity,Reason=reason.Trim(),IdempotencyKey=operationKey,ActualSphere100=s,ActualCylinder100=c,ActualAdd100=a,ActualBase100=b,ManualSelection=true});
  await db.SaveChangesAsync();await tx.CommitAsync();
 }
}
