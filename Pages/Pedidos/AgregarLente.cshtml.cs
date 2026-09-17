using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Models;
using StockLentes.Services;

namespace StockLentes.Pages.Pedidos;

public class AgregarLenteModel(StockDbContext db, OrderService orders) : PageModel
{
    public LensOrder Order { get; set; } = null!;
    public List<LensProduct> Products { get; set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int? ProductId { get; set; }

        public EyeInput OD { get; set; } = new();
        public EyeInput OI { get; set; } = new();
    }

    public class EyeInput
    {
        // LEJOS
        public string? LejosSphere { get; set; }
        public string? LejosCylinder { get; set; }
        public string? LejosAxis { get; set; }
        public string? Add { get; set; }

        // INTERMEDIA
        public string? IntermediaSphere { get; set; }
        public string? IntermediaCylinder { get; set; }
        public string? IntermediaAxis { get; set; }

        // CERCA
        public string? CercaSphere { get; set; }
        public string? CercaCylinder { get; set; }
        public string? CercaAxis { get; set; }

        // BASE, solo si viene indicada
        public string? Base { get; set; }
    }

    private async Task<IActionResult> LoadAsync(int id)
    {
        var order = await db.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        Order = order;

        if (order.Status != "PENDIENTE")
            return RedirectToPage("Detalle", new { id });

        Products = await db.Products
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(int id, string action)
    {
        var loaded = await LoadAsync(id);

        if (loaded is not PageResult)
            return loaded;

        try
        {
            if (!Input.ProductId.HasValue)
            {
                ModelState.AddModelError(
                    "Input.ProductId",
                    "Seleccioná el tipo de lente."
                );

                return Page();
            }

            var product = await db.Products
                .SingleOrDefaultAsync(x =>
                    x.Id == Input.ProductId.Value &&
                    x.IsActive);

            if (product == null)
            {
                ModelState.AddModelError(
                    "Input.ProductId",
                    "El tipo de lente seleccionado no está disponible."
                );

                return Page();
            }

            var pairNumber = await NextPairNumberAsync(id);

            var saved = 0;

            saved += await SaveEyeAsync(
                id,
                product,
                "OD",
                pairNumber,
                Input.OD
            );

            saved += await SaveEyeAsync(
                id,
                product,
                "OI",
                pairNumber,
                Input.OI
            );

            if (saved == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Ingresá al menos una graduación para guardar el anteojo."
                );

                return Page();
            }

            TempData["Success"] =
                "Anteojo guardado correctamente. No se descontó stock.";

            if (action == "another")
            {
                return RedirectToPage("AgregarLente", new { id });
            }

            return RedirectToPage("Index");
        }
        catch (InvalidOperationException e)
        {
            ModelState.AddModelError("", e.Message);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Hubo un cambio al guardar. Revisá los datos y volvé a intentarlo."
            );
        }

        return Page();
    }

    private async Task<int> NextPairNumberAsync(int orderId)
    {
        var current = await db.OrderLenses
            .Where(x => x.LensOrderId == orderId)
            .Select(x => (int?)x.PairNumber)
            .MaxAsync();

        return (current ?? 0) + 1;
    }

    private async Task<int> SaveEyeAsync(
        int orderId,
        LensProduct product,
        string eye,
        int pairNumber,
        EyeInput input)
    {
        var saved = 0;

        if (HasData(
            input.LejosSphere,
            input.LejosCylinder,
            input.LejosAxis,
            input.Add))
        {
            await SaveSectionAsync(
                orderId,
                product,
                eye,
                "LEJOS",
                pairNumber,
                input.LejosSphere,
                input.LejosCylinder,
                input.LejosAxis,
                input.Add,
                input.Base
            );

            saved++;
        }

        if (HasData(
            input.IntermediaSphere,
            input.IntermediaCylinder,
            input.IntermediaAxis))
        {
            await SaveSectionAsync(
                orderId,
                product,
                eye,
                "INTERMEDIA",
                pairNumber,
                input.IntermediaSphere,
                input.IntermediaCylinder,
                input.IntermediaAxis,
                null,
                input.Base
            );

            saved++;
        }

        if (HasData(
            input.CercaSphere,
            input.CercaCylinder,
            input.CercaAxis))
        {
            await SaveSectionAsync(
                orderId,
                product,
                eye,
                "CERCA",
                pairNumber,
                input.CercaSphere,
                input.CercaCylinder,
                input.CercaAxis,
                null,
                input.Base
            );

            saved++;
        }

        return saved;
    }

    private async Task SaveSectionAsync(
        int orderId,
        LensProduct product,
        string eye,
        string section,
        int pairNumber,
        string? sphere,
        string? cylinder,
        string? axis,
        string? add,
        string? basis)
    {
        await orders.SaveLensAsync(
            orderId,
            null,
            new LensEntry
            {
                ProductId = product.Id,
                Code = product.Code,
                Name = product.Name,

                Eye = eye,
                Section = section,
                Pair = pairNumber.ToString(),

                Sphere = sphere,
                Cylinder = cylinder,
                Axis = axis,
                Add = add,
                Base = basis
            },
            null
        );
    }

    private static bool HasData(params string?[] values)
    {
        return values.Any(x => !string.IsNullOrWhiteSpace(x));
    }
}