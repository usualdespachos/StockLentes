using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Pedidos;
public class AgregarLenteModel(StockDbContext db,OrderService orders):PageModel {
 public LensOrder Order {get;set;}=null!;
 public List<LensProduct> Products {get;set;}=[];
 [BindProperty] public GlassesEntry Input {get;set;}=new();
 private async Task<IActionResult> LoadAsync(int id){
  var order=await db.Orders.FindAsync(id);if(order==null)return NotFound();Order=order;
  if(order.Status!="PENDIENTE")return RedirectToPage("Detalle",new{id});
  Products=await db.Products.Where(x=>x.IsActive).OrderBy(x=>x.Code).ToListAsync();return Page();
 }
 public async Task<IActionResult> OnGetAsync(int id,int? lensId){
  if(lensId.HasValue)return RedirectToPage("CorregirLente",new{id,lensId});
  return await LoadAsync(id);
 }
 public async Task<IActionResult> OnPostAsync(int id,string? action){
  var loaded=await LoadAsync(id);if(loaded is not PageResult)return loaded;
  if(!ModelState.IsValid)return Page();
  try{
   var count=await orders.SaveGlassesAsync(id,Input);
   TempData["Success"]=$"Anteojo guardado: {count} lente(s) física(s). Receta conservada; los datos faltantes quedan para revisión. No se descontó stock.";
   return action=="another"?RedirectToPage("AgregarLente",new{id}):RedirectToPage("Index");
  }catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}
  catch(DbUpdateException){ModelState.AddModelError("","Hubo un cambio simultáneo. Revisá el pedido; si la receta ya se guardó, no vuelvas a cargarla con un formulario nuevo.");}
  return Page();
 }
}
