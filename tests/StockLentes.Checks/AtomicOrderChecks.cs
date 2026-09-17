using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Services;
using StockLentes.Models;

static class AtomicOrderChecks {
 public static async Task RunAsync(){
  var path=Path.Combine(Path.GetTempPath(),"stocklentes-atomic-"+Guid.NewGuid()+".db");
  var options=new DbContextOptionsBuilder<StockDbContext>().UseSqlite("Data Source="+path+";Pooling=False").Options;
  void Check(bool b,string label){if(!b)throw new Exception(label);Console.WriteLine("OK "+label);}
  async Task<int> Create(string number, params LensEntry[] lenses){await using var db=new StockDbContext(options);var s=new OrderService(db);var id=await s.CreateAsync(number,1,"DEMO",DateOnly.FromDateTime(DateTime.Today));foreach(var l in lenses)await s.SaveLensAsync(id,null,l,null);return id;}
  LensEntry White(string eye="OD")=>new(){Code="DEMO-100",Eye=eye,Sphere="1",Cylinder="0"};
  async Task<bool> Blocked(int id){await using var db=new StockDbContext(options);try{await new OrderService(db).FinishAsync(id);return false;}catch(OrderReviewException){return true;}}
  async Task<int> Balance(){await using var db=new StockDbContext(options);return await db.Stock.Where(x=>x.Product.Code=="DEMO-100"&&x.Sphere100==100).Select(x=>x.QuantityHalfPairs).SingleAsync();}
  try{
   await using(var db=new StockDbContext(options)){await db.Database.MigrateAsync();await DemoData.SeedAsync(db);}
   var a=await Create("A",White(),White("OI"));
   await using(var db=new StockDbContext(options)){Check(await new OrderService(db).FinishAsync(a)==2,"A: dos bajas atómicas");var m=await db.Movements.Where(x=>x.OrderLens!.LensOrderId==a).OrderBy(x=>x.Id).ToListAsync();Check(m[0].StockBeforeHalfPairs==10&&m[0].StockAfterHalfPairs==9&&m[1].StockBeforeHalfPairs==9&&m[1].StockAfterHalfPairs==8,"A: saldo secuencial compartido 5 → 4,5 → 4");}
   var b=await Create("B",White(),new(){Eye="OI",Code="CODIGO-DESCONOCIDO",Sphere="dato ilegible"});
   var initial=await Balance();Check(await Blocked(b),"B: una lente inválida bloquea cierre completo");
   await using(var db=new StockDbContext(options)){Check(!await db.Movements.AnyAsync(x=>x.OrderLens!.LensOrderId==b)&&await Balance()==initial,"B: ninguna baja parcial");var l=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==b&&x.Eye=="OI");Check(l.RawInputJson.Contains("dato ilegible")&&l.OriginalInputJson.Contains("CODIGO-DESCONOCIDO"),"C: conserva entrada incompleta e ilegible");await new OrderService(db).SaveLensAsync(b,l.Id,White("OI"),l.Version);}
   await using(var db=new StockDbContext(options)){Check(await new OrderService(db).FinishAsync(b)==2,"D: completar permite terminar");Check((await db.OrderLenses.SingleAsync(x=>x.LensOrderId==b&&x.Eye=="OI")).OriginalInputJson.Contains("CODIGO-DESCONOCIDO"),"D: conserva original tras corregir");}
   var e=await Create("E",new LensEntry{Code="DEMO-LIBRE"});initial=await Balance();
   await using(var db=new StockDbContext(options)){await new OrderService(db).FinishAsync(e);Check(await db.Movements.AnyAsync(x=>x.OrderLens!.LensOrderId==e&&x.StockBalanceId==null),"E: registra uso sin control");}Check(await Balance()==initial,"E: existencias intactas");
   var f=await Create("F",new LensEntry{Code="DEMO-100",Sphere="2",Cylinder="-1"});Check(await Blocked(f),"F: cero impide cierre");
   await using(var db=new StockDbContext(options)){var count=await db.Movements.CountAsync();Check(await new OrderService(db).FinishAsync(a)==0&&await db.Movements.CountAsync()==count,"G: repetir cierre no agrega bajas");}
   Check(LensValues.NormalizeCode("10020*")=="10020"&&LensValues.NormalizeCode("1012/3")=="1012/3"&&LensValues.NormalizeCode("A*B")=="A*B","H: normalización limitada a asteriscos finales");
   var append=await Create("APPEND",White());
   await using(var db=new StockDbContext(options)){var s=new OrderService(db);var l=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==append);var p=await s.PreviewAsync(l.Id,await s.DefaultSelectionAsync(l));await s.ConfirmAsync(append,l.Id,await s.DefaultSelectionAsync(l),p.Stock?.Id,p.Stock?.Version,"");await s.SaveLensAsync(append,null,White("OI"),null);Check(await s.FinishAsync(append)==1,"Agregar OI después de confirmar OD sin repetir OD");}
   // Force a failure on the second insert in a temporary DB; no stock or first movement may survive.
   var rollback=await Create("ROLLBACK",White(),White("OI"));initial=await Balance();
   await using(var db=new StockDbContext(options)){
    var second=await db.OrderLenses.Where(x=>x.LensOrderId==rollback).MaxAsync(x=>x.Id);
    var triggerSql="CREATE TRIGGER test_fail_second BEFORE INSERT ON Movements WHEN NEW.OrderLensId = "+second.ToString(System.Globalization.CultureInfo.InvariantCulture)+" BEGIN SELECT RAISE(ABORT, 'test rollback'); END;";
    await db.Database.ExecuteSqlRawAsync(triggerSql);
    bool failed=false;try{await new OrderService(db).FinishAsync(rollback);}catch(DbUpdateException){failed=true;}Check(failed,"Fallo técnico en segunda baja detectado");
   }
   await using(var db=new StockDbContext(options)){Check(await Balance()==initial&&!await db.Movements.AnyAsync(x=>x.OrderLens!.LensOrderId==rollback)&&(await db.Orders.FindAsync(rollback))!.Status=="PENDIENTE","Rollback revierte todo el pedido");await db.Database.ExecuteSqlRawAsync("DROP TRIGGER test_fail_second;");}
   // Two available-looking previews can jointly exhaust the same row; preflight sums both.
   await using(var db=new StockDbContext(options)){var s=await db.Stock.SingleAsync(x=>x.Product.Code=="DEMO-100"&&x.Sphere100==100);await new InventoryService(db).SaveAsync(s.Id,s.LensProductId,1,0,null,null,0.5m,"DEMO insuficiente",s.Version,Guid.NewGuid().ToString());}
   var joint=await Create("JOINT",White(),White("OI"));Check(await Blocked(joint)&&await Balance()==1,"Insuficiencia conjunta bloquea sin consumir primera lente");
   await using(var db=new StockDbContext(options)){var id=await new OrderService(db).SaveHeaderAsync(null,null,null,null,"fecha ilegible",null);Check(await db.Orders.AnyAsync(x=>x.Id==id)&&!(await new OrderService(db).InspectAsync(id)).CanFinish,"Cabecera incompleta se conserva y requiere revisión");}
  }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();foreach(var suffix in new[]{"","-wal","-shm"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
 }
}
