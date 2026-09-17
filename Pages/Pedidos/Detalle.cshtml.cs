using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;
namespace StockLentes.Pages.Pedidos;
public class DetalleModel(StockDbContext db, OrderService orders):PageModel {
    public LensOrder Order {get;set;}=null!;
    public List<LensProduct> Products {get;set;}=[];
    public List<StockBalance> Balances {get;set;}=[];
    public Dictionary<int,StockMovement> Confirmed {get;set;}=[];
    public Dictionary<int,LensPreview> Previews {get;set;}=[];
    public OrderReview Review {get;set;}=new([],[]);
    [BindProperty] public SelectionInput Input {get;set;}=new();
    public class SelectionInput {
        public int LensId {get;set;}
        public int ProductId {get;set;}
        public int? Base100 {get;set;}
        public int? StockId {get;set;}
        public int? ExpectedStockId {get;set;}
        public Guid? ExpectedVersion {get;set;}
        public string? Reason {get;set;}
    }
    private async Task<IActionResult> Load(int id,bool useSelection=false){
        var order=await db.Orders.AsNoTracking().Include(x=>x.Store).Include(x=>x.Lenses).SingleOrDefaultAsync(x=>x.Id==id);
        if(order==null)return NotFound();Order=order;
        Products=await db.Products.AsNoTracking().Include(x=>x.Family).ThenInclude(x=>x.Rule).Where(x=>x.IsActive).OrderBy(x=>x.Code).ToListAsync();
        Balances=await db.Stock.AsNoTracking().OrderBy(x=>x.CombinationKey).ToListAsync();
        Confirmed=await db.Movements.AsNoTracking().Where(x=>x.OrderLens!.LensOrderId==id).ToDictionaryAsync(x=>x.OrderLensId!.Value);
        foreach(var lens in Order.Lenses.Where(x=>!Confirmed.ContainsKey(x.Id))){
            var selection=useSelection&&lens.Id==Input.LensId?new LensSelection(Input.ProductId,Input.Base100,Input.StockId):await orders.DefaultSelectionAsync(lens);
            try{Previews[lens.Id]=await orders.PreviewAsync(lens.Id,selection);}
            catch(InvalidOperationException e){ModelState.AddModelError("",$"{lens.Eye} par {lens.PairNumber}: {e.Message}");}
        }
        Review=await orders.InspectAsync(id);
        return Page();
    }
    public Task<IActionResult> OnGetAsync(int id)=>Load(id);
    public Task<IActionResult> OnPostPreviewAsync(int id)=>Load(id,ModelState.IsValid);
    public async Task<IActionResult> OnPostSaveSelectionAsync(int id){
        if(ModelState.IsValid)try{
            await orders.SaveSelectionAsync(id,Input.LensId,new(Input.ProductId,Input.Base100,Input.StockId),Input.Reason);
            TempData["Success"]="Selección guardada sin descontar. Podés volver al listado y terminar el pedido.";
            return RedirectToPage(new{id});
        }catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateException){ModelState.AddModelError("","La lente cambió. Revisá la selección antes de volver a guardar.");}
        db.ChangeTracker.Clear();return await Load(id,true);
    }
    public async Task<IActionResult> OnPostConfirmAsync(int id){
        if(ModelState.IsValid)try{
            bool saved=await orders.ConfirmAsync(id,Input.LensId,new(Input.ProductId,Input.Base100,Input.StockId),Input.ExpectedStockId,Input.ExpectedVersion,Input.Reason);
            TempData["Success"]=saved?"Lente confirmada. Movimiento registrado.":"Esta lente ya estaba confirmada. No se descontó nuevamente.";
            return RedirectToPage(new{id});
        }catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateException){ModelState.AddModelError("","No se registró otra baja: hubo un cambio simultáneo. Revisá el estado actualizado.");}
        catch(Microsoft.Data.Sqlite.SqliteException e) when(e.SqliteErrorCode is 5 or 6){ModelState.AddModelError("","Otra operación está actualizando el stock. Revisá nuevamente antes de confirmar.");}
        // Discard rolled-back tracked values before rendering the current balances.
        db.ChangeTracker.Clear();
        return await Load(id,true);
    }
    public static string Combination(StockBalance s)=>string.Join(" · ",new[]{s.Sphere100.HasValue?"ESF "+LensValues.Grade(s.Sphere100):null,s.Cylinder100.HasValue?"CIL "+LensValues.Grade(s.Cylinder100):null,s.Add100.HasValue?"ADD "+LensValues.Grade(s.Add100):null,s.Base100.HasValue?"BASE "+LensValues.Grade(s.Base100):null}.Where(x=>x!=null));
}
