using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Pedidos;
public class CrearModel(StockDbContext db,OrderService orders):PageModel {
    public List<OpticalStore> Stores {get;set;}=[];
    [BindProperty] public InputModel Input {get;set;}=new();
    public int? OrderId {get;set;}
    public class InputModel {
        public string? Number {get;set;}
        public int? StoreId {get;set;}
        public string? Patient {get;set;}
        public string? Date {get;set;}=DateTime.Today.ToString("yyyy-MM-dd");
        public Guid? Version {get;set;}
    }
    public async Task<IActionResult> OnGetAsync(int? id){
        OrderId=id;Stores=await db.OpticalStores.OrderBy(x=>x.Name).ToListAsync();
        if(id.HasValue){var o=await db.Orders.FindAsync(id.Value);if(o==null)return NotFound();if(o.Status=="TERMINADO")return RedirectToPage("Detalle",new{id});Input=new(){Number=o.ExternalNumber.StartsWith("PEND-")?"":o.ExternalNumber,StoreId=o.OpticalStoreId,Patient=o.PatientName,Date=o.OrderDate.ToString("yyyy-MM-dd"),Version=o.Version};}
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int? id){
        OrderId=id;
        if(ModelState.IsValid)try {
            var savedId=await orders.SaveHeaderAsync(id,Input.Number,Input.StoreId,Input.Patient,Input.Date,Input.Version);
            return RedirectToPage(id.HasValue?"Detalle":"AgregarLente",new{id=savedId});
        }catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateException){ModelState.AddModelError("","No se guardó: revisá si esta óptica ya tiene ese número de orden.");}
        Stores=await db.OpticalStores.OrderBy(x=>x.Name).ToListAsync();return Page();
    }
}
