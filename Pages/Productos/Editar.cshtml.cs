using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Productos;
public class EditarModel(StockDbContext db):PageModel{
 [BindProperty] public InputModel Input {get;set;}=new();
 public List<LensFamily> Families {get;set;}=[];
 public class InputModel{
  public int? Id {get;set;}
  [Required,MaxLength(60)] public string Code {get;set;}="";
  [Required,MaxLength(160)] public string Name {get;set;}="";
  public int FamilyId {get;set;}
  public bool TracksStock {get;set;}=true;
  public bool IsActive {get;set;}=true;
  [Range(0,100000)] public decimal LowStockPairs {get;set;}=1;
  public Guid Version {get;set;}
 }
 public async Task<IActionResult> OnGetAsync(int? id){
  await Load();
  if(id.HasValue){var p=await db.Products.FindAsync(id.Value);if(p==null)return NotFound();Input=new(){Id=p.Id,Code=p.Code,Name=p.Name,FamilyId=p.LensFamilyId,TracksStock=p.TracksStock,IsActive=p.IsActive,LowStockPairs=p.LowStockHalfPairs/2m,Version=p.Version};}
  return Page();
 }
 private async Task Load()=>Families=await db.LensFamilies.OrderBy(x=>x.Name).ToListAsync();
 public async Task<IActionResult> OnPostAsync(){
  await Load();if(!ModelState.IsValid)return Page();
  Input.Code=LensValues.NormalizeCode(Input.Code);
  if(Input.Code.Length==0)ModelState.AddModelError("Input.Code","El código no puede quedar vacío.");
  if(!Families.Any(x=>x.Id==Input.FamilyId))ModelState.AddModelError("Input.FamilyId","Seleccioná una familia.");
  if(await db.Products.AnyAsync(x=>x.Code==Input.Code&&x.Id!=Input.Id))ModelState.AddModelError("Input.Code","Ese código ya existe.");
  if(!ModelState.IsValid)return Page();
  try{
   var threshold=LensValues.Halves(Input.LowStockPairs);
   var p=Input.Id.HasValue?await db.Products.FindAsync(Input.Id.Value):new LensProduct();
   if(p==null)return NotFound();
   if(Input.Id.HasValue && p.Version!=Input.Version)throw new InvalidOperationException("El producto cambió. Volvé a abrirlo.");
   if(Input.Id.HasValue && (p.LensFamilyId!=Input.FamilyId||p.TracksStock!=Input.TracksStock) && (await db.Stock.AnyAsync(x=>x.LensProductId==p.Id)||await db.Movements.AnyAsync(x=>x.ActualProductId==p.Id)))throw new InvalidOperationException("Con existencias o historial, no se puede cambiar la familia ni el control de stock.");
   p.Code=Input.Code;p.Name=Input.Name.Trim();p.LensFamilyId=Input.FamilyId;p.TracksStock=Input.TracksStock;p.IsActive=Input.IsActive;p.LowStockHalfPairs=threshold;p.Version=Guid.NewGuid();
   if(!Input.Id.HasValue)db.Products.Add(p);
   await db.SaveChangesAsync();TempData["Success"]="Producto guardado.";return RedirectToPage("Index");
  }catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}catch(DbUpdateException){ModelState.AddModelError("","No se pudo guardar: el producto cambió o el código ya está en uso.");}
  return Page();
 }
}
