using Microsoft.EntityFrameworkCore;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Data;
public static class DemoData {
 public static async Task SeedAsync(StockDbContext db) {
  if(await db.Products.AnyAsync() || await db.LensFamilies.AnyAsync())return;
  await using var tx=await db.Database.BeginTransactionAsync();
  var optic=new LensFamily{Name="Visión simple · Óptica",Sector="OPTICA",Rule=new StockRule{Name="ESF + CIL",UsesSphere=true,UsesCylinder=true}};
  var multi=new LensFamily{Name="Multifocal · Laboratorio",Sector="LABORATORIO",Rule=new StockRule{Name="ADD + BASE",UsesAdd=true,UsesBase=true}};
  db.LensFamilies.AddRange(optic,multi);
  var white=new LensProduct{Code="DEMO-100",Name="Orgánico blanco · DEMO",Family=optic,IsDemo=true};
  var multifocal=new LensProduct{Code="DEMO-1395",Name="Multifocal · DEMO",Family=multi,IsDemo=true};
  var free=new LensProduct{Code="DEMO-LIBRE",Name="Lente especial sin control · DEMO",Family=optic,TracksStock=false,IsDemo=true};
  db.Products.AddRange(white,multifocal,free);db.OpticalStores.Add(new OpticalStore{Name="Óptica de demostración",IsDemo=true});
  await db.SaveChangesAsync();
  foreach(var (p,s,c,a,b,q) in new[]{(white,(int?)100,(int?)0,(int?)null,(int?)null,10),(white,(int?)200,(int?)-100,(int?)null,(int?)null,0),(multifocal,(int?)null,(int?)null,(int?)200,(int?)625,6)}) {
   var stock=new StockBalance{Product=p,Sphere100=s,Cylinder100=c,Add100=a,Base100=b,CombinationKey=LensValues.Key(p.Family.Rule,s,c,a,b),QuantityHalfPairs=q};
   db.Stock.Add(stock);db.Movements.Add(new StockMovement{Balance=stock,ActualProduct=p,ActualProductCode=p.Code,ActualProductName=p.Name,Kind="INICIAL",QuantityHalfPairs=q,StockBeforeHalfPairs=0,StockAfterHalfPairs=q,Reason="Datos de demostración. No representan stock real.",ActualSphere100=s,ActualCylinder100=c,ActualAdd100=a,ActualBase100=b});
  }
  await db.SaveChangesAsync();await tx.CommitAsync();
 }
}
