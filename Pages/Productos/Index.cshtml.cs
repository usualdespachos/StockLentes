using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Pages.Productos;
public class IndexModel(StockDbContext db):PageModel {
 public List<LensProduct> Products {get;set;}=[];
 public List<LensFamily> Families {get;set;}=[];
 [BindProperty(SupportsGet=true)] public string? Search {get;set;}
 public async Task OnGetAsync(){
  var q=db.Products.Include(x=>x.Family).ThenInclude(x=>x.Rule).AsNoTracking();
  if(!string.IsNullOrWhiteSpace(Search))q=q.Where(x=>x.Code.Contains(Search)||x.Name.Contains(Search));
  Products=await q.OrderBy(x=>x.Code).ToListAsync();Families=await db.LensFamilies.Include(x=>x.Rule).ToListAsync();
 }
}
