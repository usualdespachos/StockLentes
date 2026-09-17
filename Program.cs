using Microsoft.EntityFrameworkCore;
using StockLentes.Data;
using StockLentes.Services;
using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-AR");
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var connection = builder.Configuration.GetConnectionString("Stock")
    ?? $"Data Source={Path.Combine(dataDirectory, "stocklentes-dev.db")}";
builder.Services.AddDbContext<StockDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<OrderService>();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();
if (app.Environment.IsDevelopment()) {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    await db.Database.MigrateAsync();
    await DemoData.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
