using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Pedidos;

public class CorregirLenteModel(StockDbContext db, OrderService orders):PageModel {
 public LensOrder Order {get;set;}=null!;
 public List<LensProduct> Products {get;set;}=[];
 [BindProperty] public LensEntry Input {get;set;}=new();
 [BindProperty] public Guid Version {get;set;}
 [BindProperty(SupportsGet=true)] public int LensId {get;set;}
 private async Task<OrderLens?> Load(int id){
  var lens=await db.OrderLenses.Include(x=>x.Order).SingleOrDefaultAsync(x=>x.Id==LensId&&x.LensOrderId==id);
  if(lens==null||lens.Order.Status!="PENDIENTE"||await db.Movements.AnyAsync(x=>x.OrderLensId==LensId))return null;
  Order=lens.Order;Products=await db.Products.Where(x=>x.IsActive).OrderBy(x=>x.Code).ToListAsync();return lens;
 }
 public async Task<IActionResult> OnGetAsync(int id){
  var lens=await Load(id);if(lens==null)return RedirectToPage("Detalle",new{id});
  Input=LensEntry.Read(lens);Version=lens.Version;return Page();
 }
 public async Task<IActionResult> OnPostAsync(int id){
  var lens=await Load(id);if(lens==null)return RedirectToPage("Detalle",new{id});
  if(!ModelState.IsValid)return Page();
  if(Input.Sections is {Count:>0}){
   var primary=Input.Sections.FirstOrDefault(x=>!string.IsNullOrWhiteSpace(x.Sphere)||!string.IsNullOrWhiteSpace(x.Cylinder)||!string.IsNullOrWhiteSpace(x.Axis))??Input.Sections[0];
   Input.Sphere=primary.Sphere;Input.Cylinder=primary.Cylinder;Input.Axis=primary.Axis;
  }
  try{await orders.SaveLensAsync(id,LensId,Input,Version);TempData["Success"]="Corrección guardada. La receta original permanece intacta. No se descontó stock.";return RedirectToPage("Detalle",new{id});}
  catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}
  catch(DbUpdateException){ModelState.AddModelError("","La lente cambió o repite un ojo del mismo anteojo. Revisá la identificación antes de guardar.");}
  return Page();
 }
}
