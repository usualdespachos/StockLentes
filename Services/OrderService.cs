using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;

namespace StockLentes.Services;

public record LensSelection(int ProductId, int? Base100 = null, int? StockId = null,
    bool OverrideGraduation=false,int? Sphere100=null,int? Cylinder100=null,int? Add100=null,int? Axis=null);
public record LensPreview(OrderLens Lens, LensProduct Product, StockBalance? Stock,
    int? SuggestedBase100, int? SelectedBase100, string State, string Message, bool Manual) {
    public bool CanConfirm => State is "DISPONIBLE" or "SIN_CONTROL";
    public LensSelection? Selection {get;init;}
}
public record OrderReview(List<LensPreview> Ready, List<string> Issues) {
    public bool CanFinish => Issues.Count == 0;
}
public class OrderReviewException(List<string> issues):InvalidOperationException($"No se puede terminar. {issues.Count} incidencia(s) requieren revisión. {string.Join("; ",issues)}") {
    public List<string> Issues {get;}=issues;
}

public partial class OrderService(StockDbContext db) {
    public Task<int> CreateAsync(string? number, int? storeId, string? patient, DateOnly date) =>
        SaveHeaderAsync(null,number,storeId,patient,date==default?null:date.ToString("yyyy-MM-dd"),null);

    public async Task<int> SaveHeaderAsync(int? id,string? number,int? storeId,string? patient,string? date,Guid? version){
        await using var tx=await db.Database.BeginTransactionAsync();
        var order=id.HasValue?await db.Orders.FindAsync(id.Value)??throw new InvalidOperationException("Pedido inexistente."):new LensOrder();
        if(order.Status!="PENDIENTE")throw new InvalidOperationException("El pedido está definitivamente cerrado.");
        if(id.HasValue && order.Version!=version)throw new InvalidOperationException("El pedido cambió. Volvé a abrirlo.");
        var raw=JsonSerializer.Serialize(new{number,storeId,patient,date});
        if(string.IsNullOrEmpty(order.OriginalHeaderJson))order.OriginalHeaderJson=id.HasValue?JsonSerializer.Serialize(new{order.ExternalNumber,order.OpticalStoreId,order.PatientName,order.OrderDate}):raw;
        var issues=new List<string>();
        order.ExternalNumber=string.IsNullOrWhiteSpace(number)?(id.HasValue?order.ExternalNumber:"PEND-"+Guid.NewGuid().ToString("N")[..10]):number.Trim();
        if(string.IsNullOrWhiteSpace(number)||order.ExternalNumber.StartsWith("PEND-"))issues.Add("Número de orden pendiente");
        order.PatientName=patient?.Trim()??"";if(string.IsNullOrWhiteSpace(patient))issues.Add("Paciente pendiente");
        order.OpticalStoreId=await db.OpticalStores.AnyAsync(x=>x.Id==storeId)?storeId:null;
        if(order.OpticalStoreId==null)issues.Add("Óptica pendiente");
        if(DateOnly.TryParse(date,out var parsed))order.OrderDate=parsed;else issues.Add("Fecha pendiente (se muestra fecha de ingreso)");
        order.HeaderReviewReason=string.Join("; ",issues);order.Version=Guid.NewGuid();
        if(!id.HasValue)db.Orders.Add(order);
        await db.SaveChangesAsync();await tx.CommitAsync();return order.Id;
    }

    public async Task AddLensAsync(int orderId, int productId, string eye, string section, int pair,
        decimal? sphere, decimal? cylinder, int? axis, decimal? add, decimal? basis) {
        var product=await db.Products.FindAsync(productId);
        await SaveLensAsync(orderId,null,new LensEntry{ProductId=product?.Id,Code=product?.Code,Name=product?.Name,
            Eye=eye,Section=section,Pair=pair.ToString(),Sphere=sphere?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Cylinder=cylinder?.ToString(System.Globalization.CultureInfo.InvariantCulture),Axis=axis?.ToString(),
            Add=add?.ToString(System.Globalization.CultureInfo.InvariantCulture),Base=basis?.ToString(System.Globalization.CultureInfo.InvariantCulture)},null);
    }

    public async Task SaveLensAsync(int orderId,int? lensId,LensEntry input,Guid? expectedVersion){
        await using var tx = await db.Database.BeginTransactionAsync();
        var order = await db.Orders.FindAsync(orderId) ?? throw new InvalidOperationException("Pedido inexistente.");
        if(order.Status!="PENDIENTE")throw new InvalidOperationException("El pedido está definitivamente cerrado.");
        var lens=lensId.HasValue?await db.OrderLenses.SingleOrDefaultAsync(x=>x.Id==lensId&&x.LensOrderId==orderId)??throw new InvalidOperationException("Lente inexistente."):new OrderLens{LensOrderId=orderId};
        if(lensId.HasValue && (lens.Version!=expectedVersion||await db.Movements.AnyAsync(x=>x.OrderLensId==lens.Id)))
            throw new InvalidOperationException("La lente cambió o ya tiene una baja confirmada. Su historial no se puede sobrescribir.");
        if(string.IsNullOrEmpty(lens.OriginalInputJson))lens.OriginalInputJson=JsonSerializer.Serialize(lensId.HasValue?LensEntry.Read(lens):input);
        lens.RawInputJson=JsonSerializer.Serialize(input);
        // Preserve raw input before resolving. Interpretation errors below are business notes, never exceptions.
        lens.InterpretationNote="Interpretación pendiente";
        lens.SelectedProductId=null;lens.SelectedBase100=null;lens.SelectedStockId=null;lens.SelectionReason="";
        lens.UsesActualGraduation=false;lens.UsedSphere100=null;lens.UsedCylinder100=null;lens.UsedAdd100=null;lens.UsedAxis=null;
        if(input.Sections!=null){
            if(lensId.HasValue)await db.Entry(lens).Collection(x=>x.PrescriptionSections).LoadAsync();
            foreach(var s in input.Sections){
                var row=lens.PrescriptionSections.SingleOrDefault(x=>x.Section==s.Section);
                if(row==null){row=new PrescriptionSection{Section=s.Section};lens.PrescriptionSections.Add(row);}
                row.Sphere=s.Sphere;row.Cylinder=s.Cylinder;row.Axis=s.Axis;
            }
        }
        lens.Version=Guid.NewGuid();order.Version=Guid.NewGuid();
        if(!lensId.HasValue)db.OrderLenses.Add(lens);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await InterpretAsync(lens,input);
    }

    private async Task InterpretAsync(OrderLens lens,LensEntry input){
        var issues=new List<string>();
        var code=LensValues.NormalizeCode(input.Code??"");
        var product=code!=""?await db.Products.SingleOrDefaultAsync(x=>x.Code==code&&x.IsActive):await db.Products.SingleOrDefaultAsync(x=>x.Id==input.ProductId&&x.IsActive);
        lens.RequestedProductId=product?.Id;
        lens.OriginalProductCode=product?.Code??input.Code??"";lens.OriginalProductName=product?.Name??input.Name??"";
        lens.Eye=input.Eye is "OD" or "OI"?input.Eye:"";if(lens.Eye=="")issues.Add("Ojo pendiente");
        lens.PairType=input.Section is "LEJOS" or "CERCA" or "INTERMEDIA" or "ANTEOJO"?input.Section:"";if(lens.PairType=="")issues.Add("Sección pendiente");
        lens.PairNumber=int.TryParse(input.Pair,out var pair)&&pair>=1&&pair<=99?pair:0;if(lens.PairNumber==0)issues.Add("Número de par pendiente");
        lens.Sphere100=LensEntry.Grade(input.Sphere,"ESF",issues);lens.Cylinder100=LensEntry.Grade(input.Cylinder,"CIL",issues);
        lens.Add100=LensEntry.Grade(input.Add,"ADD",issues);lens.Base100=LensEntry.Grade(input.Base,"BASE",issues);
        lens.Axis=null;if(!string.IsNullOrWhiteSpace(input.Axis)){if(int.TryParse(input.Axis,out var axis)&&axis>=0&&axis<=180)lens.Axis=axis;else issues.Add("EJE no interpretable");}
        foreach(var section in input.Sections??[]){
            LensEntry.Grade(section.Sphere,section.Section+" ESF",issues);
            LensEntry.Grade(section.Cylinder,section.Section+" CIL",issues);
            if(!string.IsNullOrWhiteSpace(section.Axis)&&(!int.TryParse(section.Axis,out var sectionAxis)||sectionAxis is <0 or >180))issues.Add(section.Section+" EJE no interpretable");
        }
        lens.InterpretationNote=string.Join("; ",issues);
        lens.RequiresUsedPrescription=DifferentDistanceGrades(input);
        await db.SaveChangesAsync();
    }

    private async Task<int?> SuggestedBaseAsync(OrderLens lens) {
        if (lens.Base100.HasValue) return lens.Base100;
        var product = await db.Products.Include(x => x.Family).ThenInclude(x => x.Rule)
            .SingleOrDefaultAsync(x => x.Id == lens.RequestedProductId || (lens.RequestedProductId==null && x.Code==LensValues.NormalizeCode(lens.OriginalProductCode)));
        if (product == null || !product.Family.Rule.UsesBase || product.Family.Rule.ManualBase) return null;
        var candidates = await MatchingAsync(product, lens);
        var bases = candidates.Select(x => x.Base100).Distinct().ToList();
        return bases.Count == 1 ? bases[0] : null;
    }

    private Task<List<StockBalance>> MatchingAsync(LensProduct product, OrderLens lens,LensSelection? selection=null) {
        var rule = product.Family.Rule;
        var sphere=selection?.OverrideGraduation==true?selection.Sphere100:lens.Sphere100;
        var cylinder=selection?.OverrideGraduation==true?selection.Cylinder100:lens.Cylinder100;
        var add=selection?.OverrideGraduation==true?selection.Add100:lens.Add100;
        // Only the family's configured dimensions participate. Missing values never become zero.
        return db.Stock.Where(x => x.LensProductId == product.Id &&
            (!rule.UsesSphere || (sphere != null && x.Sphere100 == sphere)) &&
            (!rule.UsesCylinder || (cylinder != null && x.Cylinder100 == cylinder)) &&
            (!rule.UsesAdd || (add != null && x.Add100 == add))).ToListAsync();
    }

    public async Task<LensPreview> PreviewAsync(int lensId, LensSelection selection) {
        var lens = await db.OrderLenses.Include(x => x.Order).ThenInclude(x => x.Store)
            .SingleOrDefaultAsync(x => x.Id == lensId) ?? throw new InvalidOperationException("Lente inexistente.");
        if(!string.IsNullOrEmpty(lens.InterpretationNote)&&(!selection.OverrideGraduation||lens.InterpretationNote=="Interpretación pendiente"))throw new InvalidOperationException(lens.InterpretationNote+". Usá Completar/corregir o indicá los datos utilizados.");
        if(lens.Eye is not ("OD" or "OI")||lens.PairNumber<1||string.IsNullOrWhiteSpace(lens.PairType))throw new InvalidOperationException("Completá ojo, sección y par.");
        var product = await db.Products.Include(x => x.Family).ThenInclude(x => x.Rule)
            .SingleOrDefaultAsync(x => x.Id == selection.ProductId && x.IsActive)
            ?? throw new InvalidOperationException("Código desconocido o producto pendiente. Seleccioná un producto activo.");
        var suggested = await SuggestedBaseAsync(lens);
        var rule = product.Family.Rule;
        ValidateSelection(selection);
        if(product.TracksStock&&lens.RequiresUsedPrescription&&(rule.UsesSphere||rule.UsesCylinder)&&!selection.OverrideGraduation&&!selection.StockId.HasValue)
            return new(lens,product,null,suggested,selection.Base100,"MANUAL","La receta contiene distintas graduaciones por distancia. Indicá la graduación realmente utilizada para esta lente física.",true);
        int? basis = rule.UsesBase ? selection.Base100 ?? (product.Id == lens.RequestedProductId ? suggested : null) : null;
        var manual = product.Id != lens.RequestedProductId || selection.StockId.HasValue || selection.OverrideGraduation || (rule.UsesBase && basis != suggested);
        if (!product.TracksStock)
            return new(lens, product, null, suggested, basis, "SIN_CONTROL", "Baja válida sin modificar existencias.", manual){Selection=selection};

        StockBalance? stock;
        if (selection.StockId.HasValue) {
            stock = await db.Stock.SingleOrDefaultAsync(x => x.Id == selection.StockId && x.LensProductId == product.Id);
            if (stock == null) throw new InvalidOperationException("La combinación manual no pertenece al producto seleccionado.");
            basis = stock.Base100;
        } else {
            var candidates = await MatchingAsync(product, lens,selection);
            if (rule.UsesBase) {
                if (basis == null && !rule.ManualBase && candidates.Select(x => x.Base100).Distinct().Count() == 1)
                    basis = candidates[0].Base100;
                if (basis == null)
                    return new(lens, product, null, suggested, null, "MANUAL", "REQUIERE SELECCIÓN MANUAL: indicá la base realmente utilizada.", true);
                candidates = candidates.Where(x => x.Base100 == basis).ToList();
            }
            stock = candidates.Count == 1 ? candidates[0] : null;
        }
        if (stock == null)
            return new(lens, product, null, suggested, basis, "MANUAL", "REQUIERE SELECCIÓN MANUAL: revisá producto, base y graduación realmente utilizados.", true);
        manual |= rule.UsesBase && stock.Base100 != suggested;
        return new(lens, product, stock, suggested, basis, stock.QuantityHalfPairs < 1 ? "SIN_STOCK" : "DISPONIBLE",
            stock.QuantityHalfPairs < 1 ? "SIN STOCK: indicá la lente realmente utilizada o reponé existencias." : "Stock encontrado. Se descontarán 0,5 pares al confirmar.", manual){Selection=selection};
    }

    public async Task<LensSelection> DefaultSelectionAsync(OrderLens lens){
        if(lens.SelectedProductId.HasValue)return new(lens.SelectedProductId.Value,lens.SelectedBase100,lens.SelectedStockId,lens.UsesActualGraduation,lens.UsedSphere100,lens.UsedCylinder100,lens.UsedAdd100,lens.UsedAxis);
        var code=LensValues.NormalizeCode(lens.OriginalProductCode);
        var productId=lens.RequestedProductId??await db.Products.Where(x=>x.Code==code&&x.IsActive).Select(x=>(int?)x.Id).SingleOrDefaultAsync();
        return new(productId??0);
    }

    public async Task SaveSelectionAsync(int orderId,int lensId,LensSelection selection,string? reason){
        ValidateSelection(selection);
        await using var tx=await db.Database.BeginTransactionAsync();
        var lens=await db.OrderLenses.Include(x=>x.Order).SingleOrDefaultAsync(x=>x.Id==lensId&&x.LensOrderId==orderId)??throw new InvalidOperationException("Lente inexistente.");
        if(lens.Order.Status!="PENDIENTE"||await db.Movements.AnyAsync(x=>x.OrderLensId==lensId))throw new InvalidOperationException("Esta lente ya está confirmada o el pedido está cerrado.");
        if(selection.ProductId!=0&&!await db.Products.AnyAsync(x=>x.Id==selection.ProductId&&x.IsActive))throw new InvalidOperationException("Producto inexistente.");
        if(selection.StockId.HasValue&&!await db.Stock.AnyAsync(x=>x.Id==selection.StockId&&x.LensProductId==selection.ProductId))throw new InvalidOperationException("Combinación de otro producto.");
        lens.SelectedProductId=selection.ProductId==0?null:selection.ProductId;lens.SelectedBase100=selection.Base100;lens.SelectedStockId=selection.StockId;
        lens.SelectionReason=reason??"";lens.Version=Guid.NewGuid();lens.Order.Version=Guid.NewGuid();
        lens.UsesActualGraduation=selection.OverrideGraduation;lens.UsedSphere100=selection.Sphere100;lens.UsedCylinder100=selection.Cylinder100;lens.UsedAdd100=selection.Add100;lens.UsedAxis=selection.Axis;
        await db.SaveChangesAsync();await tx.CommitAsync();
    }

    public async Task<OrderReview> InspectAsync(int orderId){
        var order=await db.Orders.Include(x=>x.Lenses).SingleOrDefaultAsync(x=>x.Id==orderId)??throw new InvalidOperationException("Pedido inexistente.");
        var issues=new List<string>();var ready=new List<LensPreview>();
        if(order.Status=="TERMINADO")return new(ready,issues);
        if(!string.IsNullOrEmpty(order.HeaderReviewReason))issues.Add(order.HeaderReviewReason);
        if(order.Lenses.Count==0)issues.Add("Faltan lentes por cargar");
        var repeated=order.Lenses.GroupBy(x=>new{x.Eye,x.PairNumber}).Where(x=>x.Count()>1).SelectMany(x=>x).Select(x=>x.Id).ToHashSet();
        var confirmed=await db.Movements.Where(x=>x.OrderLens!.LensOrderId==orderId).Select(x=>x.OrderLensId).ToListAsync();
        var remaining=new Dictionary<int,int>();
        foreach(var lens in order.Lenses.OrderBy(x=>x.Id)){
            if(confirmed.Contains(lens.Id))continue;
            var label=$"{(lens.Eye==""?"Ojo pendiente":lens.Eye)} / {lens.PairType} par {lens.PairNumber}";
            if(repeated.Contains(lens.Id)){issues.Add(label+": hay varios detalles para el mismo ojo/anteojo. Revisá si son distancias de una sola lente o anteojos distintos antes de descontar");continue;}
            try{
                var p=await PreviewAsync(lens.Id,await DefaultSelectionAsync(lens));
                if(!p.CanConfirm){issues.Add(label+": "+p.Message);continue;}
                if(p.Stock!=null){
                    var balance=remaining.GetValueOrDefault(p.Stock.Id,p.Stock.QuantityHalfPairs);
                    if(balance<1){issues.Add(label+": stock insuficiente para todas las lentes del pedido");continue;}
                    remaining[p.Stock.Id]=balance-1;
                }
                ready.Add(p);
            }catch(InvalidOperationException e){issues.Add(label+": "+e.Message);}
        }
        return new(ready,issues);
    }

    public async Task<int> FinishAsync(int orderId){
        var preflight=await InspectAsync(orderId);
        if(!preflight.CanFinish)throw new OrderReviewException(preflight.Issues);
        // Repeat validation within the transaction to prevent stale preflight balances.
        db.ChangeTracker.Clear();
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order=await db.Orders.FindAsync(orderId)??throw new InvalidOperationException("Pedido inexistente.");
        if(order.Status=="TERMINADO")return 0;
        var review=await InspectAsync(orderId);
        if(!review.CanFinish)throw new OrderReviewException(review.Issues);
        foreach(var preview in review.Ready)RecordMovement(preview,preview.Lens.SelectionReason);
        order.Status="TERMINADO";order.Version=Guid.NewGuid();
        await db.SaveChangesAsync();await tx.CommitAsync();return review.Ready.Count;
    }

    public async Task<bool> ConfirmAsync(int orderId, int lensId, LensSelection selection, int? expectedStockId,
        Guid? expectedVersion, string? reason) {
        await using var tx = await db.Database.BeginTransactionAsync();
        var lens = await db.OrderLenses.Include(x => x.Order).ThenInclude(x => x.Store)
            .SingleOrDefaultAsync(x => x.Id == lensId && x.LensOrderId == orderId)
            ?? throw new InvalidOperationException("La lente no pertenece a este pedido.");
        // Stable identity is the physical order detail, even if a second request selects another product.
        if (await db.Movements.AnyAsync(x => x.OrderLensId == lensId)) return false;
        if (lens.Order.Status != "PENDIENTE") throw new InvalidOperationException("Este pedido ya está terminado.");
        if(await db.OrderLenses.AnyAsync(x=>x.LensOrderId==orderId&&x.Id!=lensId&&x.Eye==lens.Eye&&x.PairNumber==lens.PairNumber))
            throw new InvalidOperationException("Hay detalles repetidos para este ojo, sección y par. Corregilos antes de confirmar.");
        var preview = await PreviewAsync(lensId, selection);
        if (!preview.CanConfirm) throw new InvalidOperationException(preview.Message);
        var stock = preview.Stock;
        if (stock != null && (stock.Id != expectedStockId || stock.Version != expectedVersion))
            throw new InvalidOperationException("El stock cambió desde la vista previa. Revisá el saldo actualizado antes de confirmar.");
        if (stock == null && expectedStockId.HasValue)
            throw new InvalidOperationException("Cambió el control de stock. Revisá nuevamente la selección.");
        if ((reason?.Length ?? 0) > 300) throw new InvalidOperationException("El motivo admite hasta 300 caracteres.");
        RecordMovement(preview,reason);
        lens.Order.Version=Guid.NewGuid();
        await db.SaveChangesAsync();await tx.CommitAsync();return true;
    }

    private void RecordMovement(LensPreview preview,string? reason){
        var lens=preview.Lens;var stock=preview.Stock;
        if(stock!=null&&stock.QuantityHalfPairs<1)throw new InvalidOperationException("Stock insuficiente. No se guardó ninguna baja.");
        int? before = stock?.QuantityHalfPairs;
        if (stock != null) { stock.QuantityHalfPairs -= 1; stock.Version = Guid.NewGuid(); }
        db.Movements.Add(new StockMovement {
            OrderLensId = lens.Id, ActualProductId = preview.Product.Id, StockBalanceId = stock?.Id,
            IdempotencyKey = $"lente-{lens.Id}", Kind = stock == null ? "BAJA_SIN_STOCK_CONTROLADO" : "BAJA",
            QuantityHalfPairs = -1, StockBeforeHalfPairs = before, StockAfterHalfPairs = stock?.QuantityHalfPairs,
            ActualSphere100 = stock?.Sphere100 ?? (preview.Selection?.OverrideGraduation==true?preview.Selection.Sphere100:lens.Sphere100), ActualCylinder100 = stock?.Cylinder100 ?? (preview.Selection?.OverrideGraduation==true?preview.Selection.Cylinder100:lens.Cylinder100),
            ActualAxis = preview.Selection?.OverrideGraduation==true?preview.Selection.Axis:lens.Axis, ActualAdd100 = stock?.Add100 ?? (preview.Selection?.OverrideGraduation==true?preview.Selection.Add100:lens.Add100),
            SuggestedBase100 = preview.SuggestedBase100, ActualBase100 = preview.SelectedBase100,
            ManualSelection = preview.Manual, Reason = reason?.Trim() ?? "",
            ActualProductCode = preview.Product.Code, ActualProductName = preview.Product.Name,
            RequestedSnapshotJson = JsonSerializer.Serialize(new { OrderId = lens.Order.Id, lens.Order.ExternalNumber,
                lens.Order.PatientName, OpticalStore = lens.Order.Store?.Name, lens.Order.OrderDate,
                OriginalHeader = lens.Order.OriginalHeaderJson, OriginalInput = lens.OriginalInputJson, CorrectedInput = lens.RawInputJson,
                lens.Id, lens.Eye, lens.PairType, lens.PairNumber, lens.OriginalProductCode, lens.OriginalProductName,
                lens.Sphere100, lens.Cylinder100, lens.Axis, lens.Add100, lens.Base100, SuggestedProductId = lens.RequestedProductId,
                SuggestedBase100 = preview.SuggestedBase100 })
        });
    }
    private static void ValidateSelection(LensSelection selection){
        foreach(var value in new[]{selection.Sphere100,selection.Cylinder100,selection.Add100,selection.Base100})
            if(value is <-10000 or >10000)throw new InvalidOperationException("La graduación utilizada debe estar entre -100 y 100.");
        if(selection.Axis is <0 or >180)throw new InvalidOperationException("El eje utilizado debe estar entre 0 y 180.");
    }
}
