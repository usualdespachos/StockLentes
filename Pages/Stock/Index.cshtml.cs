using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Stock;
public class IndexModel(StockDbContext db):PageModel{
 public static readonly Dictionary<string,string> OpticalCodes=new(){["100"]="Orgánico Blanco",["101"]="Orgánico Blanco AR",["139"]="Blue Cut + AR",["1810"]="Orgánico Antiage"};
 public List<StockBalance> Rows {get;set;}=[];
 public LensProduct? SelectedProduct {get;set;}
 public bool MatrixCompatible=>SelectedProduct is {TracksStock:true} p&&p.Family.Sector=="OPTICA"&&p.Family.Rule.UsesSphere&&p.Family.Rule.UsesCylinder&&!p.Family.Rule.UsesAdd&&!p.Family.Rule.UsesBase;
 [BindProperty(SupportsGet=true), Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string View {get;set;}="OPTICA";
 [BindProperty(SupportsGet=true), Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string Code {get;set;}="100";
 [BindProperty(SupportsGet=true)] public string? Search {get;set;}
 [BindProperty(SupportsGet=true)] public string? Sphere {get;set;}
 [BindProperty(SupportsGet=true)] public string? Cylinder {get;set;}
 public async Task OnGetAsync(){
  if(View is not ("OPTICA" or "LABORATORIO" or "OTROS"))View="OPTICA";
  if(string.IsNullOrEmpty(Code)||!OpticalCodes.ContainsKey(Code))Code="100";
  var q=db.Stock.Include(x=>x.Product).ThenInclude(x=>x.Family).ThenInclude(x=>x.Rule).AsNoTracking();
  if(View=="OPTICA"){
   SelectedProduct=await db.Products.AsNoTracking().Include(x=>x.Family).ThenInclude(x=>x.Rule).SingleOrDefaultAsync(x=>x.Code==Code);
   q=q.Where(x=>x.Product.Code==Code&&x.Product.Family.Sector=="OPTICA");
   var issues=new List<string>();var s=LensEntry.Grade(Sphere,"ESF",issues);var c=LensEntry.Grade(Cylinder,"CIL",issues);
   foreach(var issue in issues)ModelState.AddModelError("",issue);if(issues.Count>0)return;
   if(s.HasValue)q=q.Where(x=>x.Sphere100==s);if(c.HasValue)q=q.Where(x=>x.Cylinder100==c);
  }else if(View=="LABORATORIO")q=q.Where(x=>x.Product.Family.Sector=="LABORATORIO");
  else {var codes=OpticalCodes.Keys.ToArray();q=q.Where(x=>x.Product.Family.Sector!="LABORATORIO"&&!codes.Contains(x.Product.Code));}
  if(!string.IsNullOrWhiteSpace(Search)){var term=Search.Trim().ToLower();q=q.Where(x=>x.Product.Name.ToLower().Contains(term)||x.Product.Code.ToLower().Contains(term));}
  Rows=await q.OrderBy(x=>x.Product.Code).ThenBy(x=>x.Sphere100).ThenBy(x=>x.Cylinder100).ThenBy(x=>x.Add100).ThenBy(x=>x.Base100).ToListAsync();
 }
}
