using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Pages;
public class IndexModel(StockDbContext db):PageModel{
 public int Pending {get;set;}
 public int Finished {get;set;}
 public int Low {get;set;}
 public int Products {get;set;}
 public List<StockBalance> Attention {get;set;}=[];
 public List<StockMovement> Recent {get;set;}=[];
 public async Task OnGetAsync(){
 Pending=await db.Orders.CountAsync(x=>x.Status=="PENDIENTE");Finished=await db.Orders.CountAsync(x=>x.Status=="TERMINADO");Products=await db.Products.CountAsync(x=>x.IsActive);
 Low=await db.Stock.CountAsync(x=>x.QuantityHalfPairs<=x.Product.LowStockHalfPairs);
 Attention=await db.Stock.Include(x=>x.Product).Where(x=>x.QuantityHalfPairs<=x.Product.LowStockHalfPairs).OrderBy(x=>x.QuantityHalfPairs).Take(5).ToListAsync();
 Recent=await db.Movements.OrderByDescending(x=>x.Id).Take(4).ToListAsync();
 }
}
