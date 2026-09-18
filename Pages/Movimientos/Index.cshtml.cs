using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Pages.Movimientos;
public class IndexModel(StockDbContext db):PageModel{
 public List<StockMovement> Rows {get;set;}=[];
 [BindProperty(SupportsGet=true)] public string? Search {get;set;}
 [BindProperty(SupportsGet=true)] public string? Kind {get;set;}
 public async Task OnGetAsync(){
  var query=db.Movements.AsNoTracking().Include(x=>x.OrderLens).ThenInclude(x=>x!.Order).ThenInclude(x=>x.Store).AsQueryable();
  if(!string.IsNullOrWhiteSpace(Search)){var term=Search.Trim().ToLower();query=query.Where(x=>x.ActualProductCode.ToLower().Contains(term)||x.ActualProductName.ToLower().Contains(term)||(x.OrderLens!=null&&x.OrderLens.Order.ExternalNumber.ToLower().Contains(term)));}
  if(!string.IsNullOrWhiteSpace(Kind))query=query.Where(x=>x.Kind==Kind);
  Rows=await query.OrderByDescending(x=>x.Id).Take(100).ToListAsync();
 }
}
