using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
namespace StockLentes.Pages.Productos;
public class FamiliaModel(StockDbContext db):PageModel{
 [BindProperty,Required,MaxLength(100)] public string Name {get;set;}="";
 [BindProperty] public string Sector {get;set;}="OPTICA";
 [BindProperty] public bool Sphere {get;set;}
 [BindProperty] public bool Cylinder {get;set;}
 [BindProperty] public bool Add {get;set;}
 [BindProperty] public bool Base {get;set;}
 [BindProperty] public bool ManualBase {get;set;}
 public async Task<IActionResult> OnPostAsync(){
  if(Sector!="OPTICA"&&Sector!="LABORATORIO")ModelState.AddModelError("","Sector inválido.");
  if(ManualBase&&!Base)ModelState.AddModelError("","La selección manual de base requiere incluir BASE en la regla.");
  if(await db.LensFamilies.AnyAsync(x=>x.Name==Name.Trim()))ModelState.AddModelError("","Ya existe una familia con ese nombre.");
  if(!ModelState.IsValid)return Page();
  var fields=new List<string>();if(Sphere)fields.Add("ESF");if(Cylinder)fields.Add("CIL");if(Add)fields.Add("ADD");if(Base)fields.Add("BASE");
  db.LensFamilies.Add(new LensFamily{Name=Name.Trim(),Sector=Sector,Rule=new StockRule{Name=fields.Count==0?"Existencia única":string.Join(" + ",fields),UsesSphere=Sphere,UsesCylinder=Cylinder,UsesAdd=Add,UsesBase=Base,ManualBase=ManualBase}});
  await db.SaveChangesAsync();TempData["Success"]="Familia creada.";return RedirectToPage("Index");
 }
}
