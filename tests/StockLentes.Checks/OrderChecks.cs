using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;

static class OrderChecks {
 public static async Task AuditDemoAsync(string path){
  var connection=new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder{DataSource=path,Mode=Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,Pooling=false};
  await using var db=new StockDbContext(new DbContextOptionsBuilder<StockDbContext>().UseSqlite(connection.ToString()).Options);
  var orders=await db.Orders.AsNoTracking().Where(x=>x.ExternalNumber.StartsWith("DEMO-E2-")).Include(x=>x.Lenses).OrderBy(x=>x.Id).ToListAsync();
  var moves=await db.Movements.AsNoTracking().Include(x=>x.OrderLens).Where(x=>x.OrderLensId!=null).ToListAsync();
  foreach(var o in orders){
   var rows=moves.Where(x=>x.OrderLens!.LensOrderId==o.Id).ToList();
   Console.WriteLine($"{o.ExternalNumber}: {o.Status}, lentes {o.Lenses.Count}, bajas {rows.Count}");
   foreach(var m in rows)Console.WriteLine($"  {m.IdempotencyKey}: {m.ActualProductCode}, BASE {LensValues.Grade(m.SuggestedBase100)} → {LensValues.Grade(m.ActualBase100)}, saldo {m.StockBeforeHalfPairs/2m} → {m.StockAfterHalfPairs/2m}");
  }
  var stock=await db.Stock.AsNoTracking().Include(x=>x.Product).OrderBy(x=>x.Id).ToListAsync();
  foreach(var s in stock)Console.WriteLine($"STOCK {s.Product.Code} {s.CombinationKey}: {LensValues.Pairs(s.QuantityHalfPairs)}");
  if(orders.Count!=5||moves.Count!=5||moves.Select(x=>x.OrderLensId).Distinct().Count()!=5||orders.Count(x=>x.Status=="TERMINADO")!=4)
   throw new Exception("Revisar totales de la prueba de interfaz.");
  var zero=orders.Single(x=>x.ExternalNumber=="DEMO-E2-CERO");
  if(zero.Status!="PENDIENTE"||moves.Any(x=>x.OrderLens!.LensOrderId==zero.Id))throw new Exception("Stock cero no quedó bloqueado.");
  if(stock.Single(x=>x.Product.Code=="DEMO-100"&&x.Sphere100==100).QuantityHalfPairs!=7||
     stock.Single(x=>x.Product.Code=="DEMO-100"&&x.Sphere100==200).QuantityHalfPairs!=0||
     stock.Single(x=>x.Product.Code=="DEMO-1395"&&x.Base100==625).QuantityHalfPairs!=6||
     stock.Single(x=>x.Product.Code=="DEMO-1395"&&x.Base100==650).QuantityHalfPairs!=3)
     throw new Exception("Los saldos finales no corresponden a las pruebas.");
  Console.WriteLine("AUDITORIA DEMO OK: lectura directa de SQLite, sin escrituras.");
 }
 public static async Task RunAsync(){
  var path=Path.Combine(Path.GetTempPath(),"stocklentes-orders-"+Guid.NewGuid()+".db");
  var options=new DbContextOptionsBuilder<StockDbContext>().UseSqlite("Data Source="+path+";Pooling=False").Options;
  void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("OK "+message);}
  async Task<int> Order(string number,int product,decimal? esf,decimal? cil,decimal? add=null,decimal? basis=null,bool pair=false){
   await using var db=new StockDbContext(options);var service=new OrderService(db);
   var id=await service.CreateAsync(number,1,"Paciente DEMO",DateOnly.FromDateTime(DateTime.Today));
   await service.AddLensAsync(id,product,"OD","LEJOS",1,esf,cil,90,add,basis);
   if(pair)await service.AddLensAsync(id,product,"OI","LEJOS",1,esf,cil,90,add,basis);
   return id;
  }
  async Task Confirm(int order,string eye,LensSelection selection){
   await using var db=new StockDbContext(options);var service=new OrderService(db);
   int lens=await db.OrderLenses.Where(x=>x.LensOrderId==order&&x.Eye==eye).Select(x=>x.Id).SingleAsync();
   var p=await service.PreviewAsync(lens,selection);await service.ConfirmAsync(order,lens,selection,p.Stock?.Id,p.Stock?.Version,"Prueba DEMO");
  }
  try{
   await using(var db=new StockDbContext(options)){await db.Database.MigrateAsync();await DemoData.SeedAsync(db);}
   int white,multi,free,stockWhite,stockAlternative;
   await using(var db=new StockDbContext(options)){
    white=await db.Products.Where(x=>x.Code=="DEMO-100").Select(x=>x.Id).SingleAsync();
    multi=await db.Products.Where(x=>x.Code=="DEMO-1395").Select(x=>x.Id).SingleAsync();
    free=await db.Products.Where(x=>!x.TracksStock).Select(x=>x.Id).SingleAsync();
    stockWhite=await db.Stock.Where(x=>x.LensProductId==white&&x.Sphere100==100).Select(x=>x.Id).SingleAsync();
    await new InventoryService(db).SaveAsync(null,multi,null,null,2,6.5m,2,"DEMO alternativa",Guid.Empty,Guid.NewGuid().ToString());
    stockAlternative=await db.Stock.Where(x=>x.LensProductId==multi&&x.Base100==650).Select(x=>x.Id).SingleAsync();
   }
   var automatic=await Order("DEMO-AUTO",white,1,0,pair:true);
   await Confirm(automatic,"OD",new(white));
   await using(var db=new StockDbContext(options)){
    Check((await db.Stock.FindAsync(stockWhite))!.QuantityHalfPairs==9,"Pedido OD 5 → 4,5");
    Check((await db.Orders.FindAsync(automatic))!.Status=="PENDIENTE","OI pendiente mantiene pedido pendiente");
   }
   await Confirm(automatic,"OI",new(white));
   await using(var db=new StockDbContext(options)){
    Check((await db.Stock.FindAsync(stockWhite))!.QuantityHalfPairs==8,"Pedido OI 4,5 → 4");
    Check((await db.Orders.FindAsync(automatic))!.Status=="PENDIENTE","La baja individual mantiene el pedido abierto");
    await new OrderService(db).FinishAsync(automatic);
    Check((await db.Orders.FindAsync(automatic))!.Status=="TERMINADO","Cierre explícito finaliza pedido");
    Check(await db.Movements.CountAsync(x=>x.OrderLens!.LensOrderId==automatic)==2,"Dos bajas separadas");
   }
   var manual=await Order("DEMO-BASE",multi,null,null,2,6.25m);
   await Confirm(manual,"OD",new(multi,650));
   await using(var db=new StockDbContext(options)){
    var m=await db.Movements.SingleAsync(x=>x.OrderLens!.LensOrderId==manual);
    Check(m.SuggestedBase100==625&&m.ActualBase100==650&&m.ManualSelection,"Base sugerida y utilizada separadas");
    Check((await db.Stock.FindAsync(stockAlternative))!.QuantityHalfPairs==3,"Solo descuenta base seleccionada");
    Check((await db.OrderLenses.SingleAsync(x=>x.LensOrderId==manual)).Base100==625,"Pedido original intacto");
   }
   var outside=await Order("DEMO-FUERA",white,12,0);
   await using(var db=new StockDbContext(options)){
    var lens=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==outside);
    Check((await new OrderService(db).PreviewAsync(lens.Id,new(white))).State=="MANUAL","Fuera de rango requiere selección manual");
   }
   await Confirm(outside,"OD",new(white,StockId:stockWhite));
   await using(var db=new StockDbContext(options)){
    var m=await db.Movements.SingleAsync(x=>x.OrderLens!.LensOrderId==outside);
    Check(m.ActualSphere100==100&&m.ManualSelection&&m.RequestedSnapshotJson.Contains("1200"),"Graduación usada no sobrescribe solicitada");
   }
   var uncontrolled=await Order("DEMO-LIBRE",white,1,0);
   Dictionary<int,int> before;
   await using(var db=new StockDbContext(options))before=await db.Stock.ToDictionaryAsync(x=>x.Id,x=>x.QuantityHalfPairs);
   await Confirm(uncontrolled,"OD",new(free));
   await using(var db=new StockDbContext(options)){
    Check((await db.Stock.ToListAsync()).All(x=>before[x.Id]==x.QuantityHalfPairs),"Sin control no toca existencias");
    var m=await db.Movements.SingleAsync(x=>x.OrderLens!.LensOrderId==uncontrolled);
    Check(m.ActualProductId==free&&m.StockBalanceId==null&&m.ManualSelection,"Cambio de producto queda registrado");
   }
   var zero=await Order("DEMO-CERO",white,2,-1);
   await using(var db=new StockDbContext(options)){
    var lens=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==zero);var service=new OrderService(db);
    var p=await service.PreviewAsync(lens.Id,new(white));Check(p.State=="SIN_STOCK"&&!p.CanConfirm,"Cero stock bloqueado");
    bool blocked=false;try{await service.ConfirmAsync(zero,lens.Id,new(white),p.Stock?.Id,p.Stock?.Version,"");}catch(InvalidOperationException){blocked=true;}
    Check(blocked&&!await db.Movements.AnyAsync(x=>x.OrderLensId==lens.Id),"Backend rechaza baja sin stock");
   }
   await using(var db=new StockDbContext(options)){
    var lens=await db.OrderLenses.FirstAsync(x=>x.LensOrderId==automatic);
    var count=await db.Movements.CountAsync();
    Check(!await new OrderService(db).ConfirmAsync(automatic,lens.Id,new(free),null,null,"Reenvío"),"Duplicado bloqueado incluso cambiando producto");
    Check(await db.Movements.CountAsync()==count,"Duplicado no agrega movimientos");
   }
   // A late persistence failure must roll back the balance, movement and order status together.
   var rollback=await Order("DEMO-ROLLBACK",white,1,0);
   await using(var db=new StockDbContext(options)){
    var lens=await db.OrderLenses.SingleAsync(x=>x.LensOrderId==rollback);var service=new OrderService(db);
    var p=await service.PreviewAsync(lens.Id,new(white));
    db.Products.Add(new LensProduct{Code="DEMO-100",Name="Duplicate",LensFamilyId=1});
    bool failed=false;try{await service.ConfirmAsync(rollback,lens.Id,new(white),p.Stock?.Id,p.Stock?.Version,"");}catch(DbUpdateException){failed=true;}
    Check(failed,"Fallo técnico de baja detectado");
   }
   await using(var db=new StockDbContext(options)){
    Check((await db.Stock.FindAsync(stockWhite))!.QuantityHalfPairs==before[stockWhite],"Rollback de baja conserva stock");
    Check(!await db.Movements.AnyAsync(x=>x.OrderLens!.LensOrderId==rollback)&&(await db.Orders.FindAsync(rollback))!.Status=="PENDIENTE","Rollback no deja baja ni pedido terminado");
   }
  }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();foreach(var suffix in new[]{"","-wal","-shm"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
 }
}
