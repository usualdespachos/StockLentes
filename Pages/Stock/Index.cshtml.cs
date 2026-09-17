using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Pages.Stock;
public class IndexModel(StockDbContext db):PageModel{
 public List<StockBalance> Rows {get;set;}=[];
 [BindProperty(SupportsGet=true)] public string? Search {get;set;}
 public async Task OnGetAsync(){
  var q=db.Stock.Include(x=>x.Product).ThenInclude(x=>x.Family).AsNoTracking();
  if(!string.IsNullOrWhiteSpace(Search))q=q.Where(x=>x.Product.Name.Contains(Search)||x.Product.Code.Contains(Search));
  Rows=await q.OrderBy(x=>x.Product.Code).ThenBy(x=>x.Id).ToListAsync();
 }
}
