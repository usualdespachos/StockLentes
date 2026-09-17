using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Pages.Movimientos;
public class IndexModel(StockDbContext db):PageModel{
 public List<StockMovement> Rows {get;set;}=[];
 public async Task OnGetAsync()=>Rows=await db.Movements.AsNoTracking().Include(x=>x.OrderLens).ThenInclude(x=>x!.Order).ThenInclude(x=>x.Store).OrderByDescending(x=>x.Id).Take(100).ToListAsync();
}
