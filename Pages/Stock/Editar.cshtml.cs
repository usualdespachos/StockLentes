using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Stock;
public class EditarModel(StockDbContext db,InventoryService inventory):PageModel{
 public List<LensProduct> Products {get;set;}=[];
 public StockRule? Rule {get;set;}
 public int? CurrentHalfPairs {get;set;}
 [BindProperty] public InputModel Input {get;set;}=new();
 public class InputModel{
  public int? Id {get;set;}
  public bool Entry {get;set;}
  public int ProductId {get;set;}
  public decimal? Sphere {get;set;}
  public decimal? Cylinder {get;set;}
  public decimal? Add {get;set;}
  public decimal? Base {get;set;}
  [Range(0,100000)] public decimal Pairs {get;set;}
  [Required,MaxLength(300)] public string Reason {get;set;}="";
  public Guid Version {get;set;}
  public string OperationKey {get;set;}=Guid.NewGuid().ToString();
 }
 private async Task Load(){
  Products=await db.Products.Include(x=>x.Family).ThenInclude(x=>x.Rule).Where(x=>x.TracksStock&&x.IsActive).OrderBy(x=>x.Name).ToListAsync();
  Rule=Products.FirstOrDefault(x=>x.Id==Input.ProductId)?.Family.Rule;
  if(Input.Id.HasValue)CurrentHalfPairs=await db.Stock.Where(x=>x.Id==Input.Id).Select(x=>(int?)x.QuantityHalfPairs).SingleOrDefaultAsync();
 }
 public async Task<IActionResult> OnGetAsync(int? id,int? productId,bool entry=false){
  if(id.HasValue){var s=await db.Stock.FindAsync(id.Value);if(s==null)return NotFound();Input=new(){Id=s.Id,ProductId=s.LensProductId,Sphere=s.Sphere100/100m,Cylinder=s.Cylinder100/100m,Add=s.Add100/100m,Base=s.Base100/100m,Pairs=s.QuantityHalfPairs/2m,Version=s.Version};}
  else Input.ProductId=productId??0;
  Input.Entry=entry&&id.HasValue;if(Input.Entry)Input.Pairs=0;
  await Load();return Page();
 }
 public async Task<IActionResult> OnPostAsync(){
  await Load();if(!ModelState.IsValid)return Page();
  try{await inventory.SaveAsync(Input.Id,Input.ProductId,Input.Sphere,Input.Cylinder,Input.Add,Input.Base,Input.Pairs,Input.Reason,Input.Version,Input.OperationKey,Input.Entry);TempData["Success"]=Input.Entry?"Mercadería ingresada y movimiento ENTRADA registrado.":"Stock guardado y movimiento registrado.";var product=Products.Single(x=>x.Id==Input.ProductId);return RedirectToPage("Index",new { View=product.Family.Sector=="LABORATORIO"?"LABORATORIO":IndexModel.OpticalCodes.ContainsKey(product.Code)?"OPTICA":"OTROS",Code=product.Code });}
  catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}
  catch(DbUpdateException){ModelState.AddModelError("","No se guardó el ajuste: hubo un cambio simultáneo o la combinación ya existe. Volvé a abrir el formulario.");}
  catch(Microsoft.Data.Sqlite.SqliteException e) when(e.SqliteErrorCode is 5 or 6){ModelState.AddModelError("","Otra operación está actualizando el stock. Volvé a abrir el formulario antes de intentar nuevamente.");}
  return Page();
 }
}
