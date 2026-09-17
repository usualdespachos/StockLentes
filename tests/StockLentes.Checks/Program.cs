using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
if(args.Length==2 && args[0]=="--audit-demo") { await OrderChecks.AuditDemoAsync(args[1]); return; }
var path=Path.Combine(Path.GetTempPath(),"stocklentes-check-"+Guid.NewGuid()+".db");
var options=new DbContextOptionsBuilder<StockDbContext>().UseSqlite("Data Source="+path+";Pooling=False").Options;
void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("OK "+message);}
try{
 await using(var db=new StockDbContext(options)){await db.Database.MigrateAsync();await DemoData.SeedAsync(db);await DemoData.SeedAsync(db);Check(await db.Products.CountAsync()==3,"Semilla idempotente");}
 int productId,stockId;Guid original;
 await using(var db=new StockDbContext(options)){var s=await db.Stock.Include(x=>x.Product).SingleAsync(x=>x.Product.Code=="DEMO-100"&&x.Sphere100==100);productId=s.LensProductId;stockId=s.Id;original=s.Version;}
 var key=Guid.NewGuid().ToString();
 await using(var db=new StockDbContext(options)){await new InventoryService(db).SaveAsync(stockId,productId,1,0,null,null,4.5m,"Prueba",original,key);}
 await using(var db=new StockDbContext(options)){await new InventoryService(db).SaveAsync(stockId,productId,1,0,null,null,4.5m,"Prueba",original,key);Check(await db.Movements.CountAsync(x=>x.IdempotencyKey==key)==1,"Reenvío no duplica movimiento");Check((await db.Stock.FindAsync(stockId))!.QuantityHalfPairs==9,"Media unidad exacta");}
 foreach(var invalid in new[]{-1m,1.25m}){
  await using var db=new StockDbContext(options);var count=await db.Movements.CountAsync();var s=await db.Stock.FindAsync(stockId);bool blocked=false;
  try{await new InventoryService(db).SaveAsync(stockId,productId,1,0,null,null,invalid,"Inválida",s!.Version,Guid.NewGuid().ToString());}catch(InvalidOperationException){blocked=true;}
  Check(blocked&&await db.Movements.CountAsync()==count,"Cantidad inválida no deja movimiento");
 }
 await using(var db=new StockDbContext(options)){
  bool blocked=false;try{await new InventoryService(db).SaveAsync(stockId,productId,1,0,null,null,3,"Obsoleto",original,Guid.NewGuid().ToString());}catch(InvalidOperationException){blocked=true;}Check(blocked,"Formulario obsoleto bloqueado");
 }
 await using(var db=new StockDbContext(options)){
  var p=await db.Products.SingleAsync(x=>!x.TracksStock);bool blocked=false;
  try{await new InventoryService(db).SaveAsync(null,p.Id,1,0,null,null,1,"Sin control",Guid.Empty,Guid.NewGuid().ToString());}catch(InvalidOperationException){blocked=true;}Check(blocked,"Sin control no crea existencias");
 }
 // Fuerza una falla de persistencia al final de la transacción: ni stock ni historial sobreviven.
 await using(var db=new StockDbContext(options)){
  var count=await db.Movements.CountAsync();db.Products.Add(new LensProduct{Code="DEMO-100",Name="Duplicado",LensFamilyId=1});
  var s=await db.Stock.FindAsync(stockId);bool failed=false;
  try{await new InventoryService(db).SaveAsync(stockId,productId,1,0,null,null,2,"Rollback",s!.Version,Guid.NewGuid().ToString());}catch(DbUpdateException){failed=true;}Check(failed,"Fallo de persistencia detectado");
 }
 await using(var db=new StockDbContext(options)){Check((await db.Stock.FindAsync(stockId))!.QuantityHalfPairs==9,"Rollback conserva saldo");Check(!await db.Movements.AnyAsync(x=>x.Reason=="Rollback"),"Rollback no deja historial parcial");}
 Check(LensValues.NormalizeCode(" 10020* ")=="10020","Normalización de código");
 Check(LensValues.Key(new StockRule{UsesAdd=true},null,null,200,null)=="ADD=200","Bifocal sin BASE");
 await OrderChecks.RunAsync();
 await AtomicOrderChecks.RunAsync();
 Console.WriteLine("Todas las comprobaciones pasaron.");
}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();foreach(var suffix in new[]{"","-wal","-shm"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
