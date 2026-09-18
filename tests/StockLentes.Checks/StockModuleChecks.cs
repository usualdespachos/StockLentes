using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;

static class StockModuleChecks {
 public static async Task RunAsync(){
  var path=Path.Combine(Path.GetTempPath(),"stock-module-"+Guid.NewGuid()+".db");
  var options=new DbContextOptionsBuilder<StockDbContext>().UseSqlite("Data Source="+path+";Pooling=False").Options;
  void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("OK "+label);}
  try{
   await using var db=new StockDbContext(options);await db.Database.MigrateAsync();await DemoData.SeedAsync(db);
   var product=new LensProduct{Code="100",Name="Orgánico Blanco",LensFamilyId=await db.Products.Where(x=>x.Code=="DEMO-100").Select(x=>x.LensFamilyId).SingleAsync()};db.Products.Add(product);await db.SaveChangesAsync();
   var inventory=new InventoryService(db);
   await inventory.SaveAsync(null,product.Id,-1,-0.25m,null,null,8,"Prueba aislada inicial",Guid.Empty,Guid.NewGuid().ToString());
   var stock=await db.Stock.SingleAsync(x=>x.LensProductId==product.Id);
   Check(stock.QuantityHalfPairs==16,"100: nueva combinación guarda 8 pares");
   var entryKey=Guid.NewGuid().ToString();var version=stock.Version;
   await inventory.SaveAsync(stock.Id,product.Id,-1,-0.25m,null,null,5,"Mercadería recibida",version,entryKey,true);
   var movement=await db.Movements.SingleAsync(x=>x.IdempotencyKey==entryKey);
   Check(stock.QuantityHalfPairs==26&&movement.Kind=="ENTRADA"&&movement.StockBeforeHalfPairs==16&&movement.QuantityHalfPairs==10&&movement.StockAfterHalfPairs==26,"ENTRADA suma 8 + 5 = 13 y conserva historial");
   await inventory.SaveAsync(stock.Id,product.Id,-1,-0.25m,null,null,5,"Reintento",version,entryKey,true);
   Check(await db.Movements.CountAsync(x=>x.IdempotencyKey==entryKey)==1&&stock.QuantityHalfPairs==26,"Reintento de ENTRADA no vuelve a sumar");
   bool stale=false;try{await inventory.SaveAsync(stock.Id,product.Id,-1,-0.25m,null,null,5,"Obsoleto",version,Guid.NewGuid().ToString(),true);}catch(InvalidOperationException){stale=true;}
   Check(stale&&stock.QuantityHalfPairs==26,"Entrada con versión obsoleta no sobrescribe stock");
   var adjustment=Guid.NewGuid().ToString();await inventory.SaveAsync(stock.Id,product.Id,-1,-0.25m,null,null,10,"Conteo manual",stock.Version,adjustment);
   movement=await db.Movements.SingleAsync(x=>x.IdempotencyKey==adjustment);
   Check(movement.Kind=="AJUSTE"&&movement.QuantityHalfPairs==-6&&movement.StockBeforeHalfPairs==26&&movement.StockAfterHalfPairs==20,"AJUSTE MANUAL registra 13 → 10, variación -3");
   foreach(var amount in new[]{0m,-1m,0.25m}){bool blocked=false;try{await inventory.SaveAsync(stock.Id,product.Id,-1,-0.25m,null,null,amount,"Inválida",stock.Version,Guid.NewGuid().ToString(),true);}catch(InvalidOperationException){blocked=true;}Check(blocked&&stock.QuantityHalfPairs==20,"Entrada inválida bloqueada: "+amount);}
   var orders=new OrderService(db);var order=await orders.CreateAsync("100-PRUEBA",1,"Temporal",DateOnly.FromDateTime(DateTime.Today));
   await orders.SaveGlassesAsync(order,new(){ProductId=product.Id,OD=new(){LejosSphere="-1",LejosCylinder="-0.50"},OI=new(){LejosSphere="-1",LejosCylinder="-0.50"}});
   foreach(var lens in await db.OrderLenses.Where(x=>x.LensOrderId==order).ToListAsync())await orders.SaveSelectionAsync(order,lens.Id,new(product.Id,OverrideGraduation:true,Sphere100:-100,Cylinder100:-25),"Graduación utilizada");
   Check(await orders.FinishAsync(order)==2,"100: TERMINAR crea dos bajas automáticas");
   var used=await db.Movements.Where(x=>x.OrderLens!.LensOrderId==order).OrderBy(x=>x.Id).ToListAsync();
   Check(used[0].StockBeforeHalfPairs==20&&used[0].StockAfterHalfPairs==19&&used[1].StockBeforeHalfPairs==19&&used[1].StockAfterHalfPairs==18&&used.All(x=>x.QuantityHalfPairs==-1),"100: 10 → 9,5 → 9 pares, una baja por ojo");
   Check(used.All(x=>x.ActualCylinder100==-25&&MovementDisplay.Original(x)?.Cylinder=="-0.50"),"Movimientos reconstruye solicitado -0,50 y utilizado -0,25");
   Check(await orders.FinishAsync(order)==0&&await db.Movements.CountAsync(x=>x.OrderLens!.LensOrderId==order)==2,"100: reintento de pedido no duplica bajas");
   Check(MovementDisplay.Kind("AJUSTE")=="AJUSTE MANUAL"&&MovementDisplay.Kind("BAJA")=="BAJA POR PEDIDO","Tipos de movimiento legibles sin alterar historial");
  }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();foreach(var suffix in new[]{"","-wal","-shm"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
 }
}
