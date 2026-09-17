using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Pedidos;
public class IndexModel(StockDbContext db,OrderService orders):PageModel {
    [BindProperty(SupportsGet=true)] public string? Search {get;set;}
    [BindProperty(SupportsGet=true)] public string? Status {get;set;}
    public List<LensOrder> Rows {get;set;}=[];
    public Dictionary<int,OrderReview> Reviews {get;set;}=[];
    public async Task OnGetAsync(){
        var q=db.Orders.AsNoTracking().Include(x=>x.Store).Include(x=>x.Lenses).AsQueryable();
        if(!string.IsNullOrWhiteSpace(Search))q=q.Where(x=>x.ExternalNumber.Contains(Search)||x.PatientName.Contains(Search));
        if(Status is "PENDIENTE" or "TERMINADO")q=q.Where(x=>x.Status==Status);
        Rows=await q.OrderBy(x=>x.Status=="TERMINADO").ThenByDescending(x=>x.Id).ToListAsync();
        foreach(var o in Rows.Where(x=>x.Status=="PENDIENTE"))Reviews[o.Id]=await orders.InspectAsync(o.Id);
    }
    public async Task<IActionResult> OnPostFinishAsync(int orderId){
        try{var count=await orders.FinishAsync(orderId);TempData["Success"]=count==0?"Pedido terminado; no se generaron bajas duplicadas.":$"Pedido terminado. {count} lente(s) registradas correctamente.";}
        catch(InvalidOperationException e){TempData["ReviewError"]=e.Message;}
        catch(DbUpdateException){TempData["ReviewError"]="El pedido o el stock cambiaron. No se guardó ninguna baja de este intento. Revisá y reintentá.";}
        catch(Microsoft.Data.Sqlite.SqliteException e) when(e.SqliteErrorCode is 5 or 6){TempData["ReviewError"]="Hay otra operación en curso. No se completó este intento; volvé a revisar el pedido.";}
        return RedirectToPage();
    }
}
