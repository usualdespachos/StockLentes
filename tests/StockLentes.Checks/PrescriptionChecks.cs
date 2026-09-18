using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Services;

static class PrescriptionChecks {
 public static async Task RunAsync(){
  var path=Path.Combine(Path.GetTempPath(),"prescription-"+Guid.NewGuid()+".db");
  var options=new DbContextOptionsBuilder<StockDbContext>().UseSqlite("Data Source="+path+";Pooling=False").Options;
  void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("OK "+text);}
  try{
   await using var db=new StockDbContext(options);await db.Database.MigrateAsync();await DemoData.SeedAsync(db);
   var service=new OrderService(db);
   var multi=await db.Products.SingleAsync(x=>x.Code=="DEMO-1395");
   var white=await db.Products.SingleAsync(x=>x.Code=="DEMO-100");
   async Task<int> Create(string number)=>await service.CreateAsync(number,1,"Prueba receta",DateOnly.FromDateTime(DateTime.Today));
   EyePrescription Recipe()=>new(){LejosSphere="1",LejosCylinder="0",LejosAxis="90",IntermediaSphere="2",IntermediaCylinder="0",IntermediaAxis="90",CercaSphere="3",CercaCylinder="0",CercaAxis="90",Add="2",Base="6.25"};
   var id=await Create("RECETA");var entry=new GlassesEntry{ProductId=multi.Id,OD=Recipe(),OI=Recipe()};
   var count=await db.Movements.CountAsync();var balances=await db.Stock.OrderBy(x=>x.Id).Select(x=>x.QuantityHalfPairs).ToArrayAsync();
   Check(await service.SaveGlassesAsync(id,entry)==2,"Receta con tres distancias: dos lentes físicas");
   Check(await db.PrescriptionSections.CountAsync()==6,"Conserva las seis secciones OD/OI");
   await service.SaveGlassesAsync(id,entry);
   Check(await db.OrderLenses.CountAsync(x=>x.LensOrderId==id)==2,"Reenvío de formulario no duplica anteojo");
   var afterSave=await db.Stock.OrderBy(x=>x.Id).Select(x=>x.QuantityHalfPairs).ToArrayAsync();
   Check(await db.Movements.CountAsync()==count&&balances.SequenceEqual(afterSave),"Guardar receta no genera movimientos ni modifica stock");
   Check(await service.FinishAsync(id)==2,"Terminar multifocal de tres distancias genera sólo dos bajas");
   var movements=await db.Movements.Where(x=>x.OrderLens!.LensOrderId==id).OrderBy(x=>x.Id).ToListAsync();
   Check(movements[0].StockBeforeHalfPairs==6&&movements[0].StockAfterHalfPairs==5&&movements[1].StockBeforeHalfPairs==5&&movements[1].StockAfterHalfPairs==4,"Consumo secuencial: 3 → 2,5 → 2 pares");
   Check(movements.All(x=>x.QuantityHalfPairs==-1&&x.RequestedSnapshotJson.Contains("INTERMEDIA")),"Cada baja de 0,5 conserva receta completa");
   Check(await service.FinishAsync(id)==0,"Repetir TERMINAR no vuelve a consumir");
   var more=await Create("VARIOS");await service.SaveGlassesAsync(more,new(){ProductId=multi.Id,OD=Recipe(),OI=Recipe()});
   await service.SaveGlassesAsync(more,new(){ProductId=multi.Id,OD=Recipe(),OI=Recipe()});
   Check(await db.OrderLenses.CountAsync(x=>x.LensOrderId==more)==4&&await db.OrderLenses.CountAsync(x=>x.LensOrderId==more&&x.PairNumber==2)==2,"Agregar otro anteojo crea segundo par, no otras distancias");
   var unknown=await Create("INCOMPLETO");await service.SaveGlassesAsync(unknown,new(){Code="DESCONOCIDO",OD=new(){LejosSphere="ilegible",CercaSphere="200"}});
   Check(await db.OrderLenses.CountAsync(x=>x.LensOrderId==unknown)==2&&!(await service.InspectAsync(unknown)).CanFinish,"Receta incompleta/desconocida se guarda y queda en revisión");
   var saved=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==unknown&&x.Eye=="OD");var original=saved.OriginalInputJson;
   var corrected=LensEntry.Read(saved);corrected.Code=multi.Code;corrected.Sphere="1";corrected.Cylinder="0";corrected.Add="2";corrected.Base="6.25";
   corrected.Sections=[new("LEJOS","1","0","90"),new("INTERMEDIA","2","0","90"),new("CERCA","3","0","90")];
   await service.SaveLensAsync(unknown,saved.Id,corrected,saved.Version);
   Check(saved.OriginalInputJson==original&&original.Contains("ilegible")&&await db.OrderLenses.CountAsync(x=>x.LensOrderId==unknown)==2,"Corregir conserva original sin crear lentes extra");
   Check(await db.PrescriptionSections.AnyAsync(x=>x.OrderLensId==saved.Id&&x.Section=="CERCA"&&x.Sphere=="3"),"Corrección actualiza secciones conservadas");
   var actual=await Create("UTILIZADO");await service.SaveGlassesAsync(actual,new(){ProductId=white.Id,IncludeOI=false,OD=new(){LejosSphere="2",LejosCylinder="-1.25",LejosAxis="100"}});
   var lens=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==actual);
   Check(!(await service.InspectAsync(actual)).CanFinish,"Solicitado sin stock requiere excepción");
   await service.SaveSelectionAsync(actual,lens.Id,new(white.Id,OverrideGraduation:true,Sphere100:100,Cylinder100:0,Axis:100),"Prueba de graduación utilizada");
   Check(await service.FinishAsync(actual)==1,"Una sola lente indicada produce una baja");
   var used=await db.Movements.SingleAsync(x=>x.OrderLensId==lens.Id);
   Check(used.ActualSphere100==100&&used.ActualCylinder100==0&&used.RequestedSnapshotJson.Contains("-1.25"),"Descuenta graduación utilizada y conserva solicitada");
   var ambiguous=await Create("AMBIGUA");await service.SaveGlassesAsync(ambiguous,new(){ProductId=white.Id,IncludeOI=false,OD=Recipe()});
   Check(!(await service.InspectAsync(ambiguous)).CanFinish,"Varias graduaciones en visión simple exigen indicar lente utilizada");
   var secondary=await Create("SECUNDARIA");await service.SaveGlassesAsync(secondary,new(){ProductId=multi.Id,IncludeOI=false,OD=new(){LejosSphere="1",Add="2",Base="6.25",CercaSphere="ilegible"}});
   Check(!(await service.InspectAsync(secondary)).CanFinish&&await db.OrderLenses.AnyAsync(x=>x.LensOrderId==secondary&&x.InterpretationNote.Contains("CERCA ESF")),"Datos ilegibles en otra distancia también quedan en revisión");
  }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();foreach(var suffix in new[]{"","-wal","-shm"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
 }
}
