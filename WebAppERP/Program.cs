using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Infrastructure;
using WebAppERP.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Parsing invariante de numeros (decimal/double/float) para casar com o formato
// enviado por <input type="number"> (ponto decimal), sem alterar a exibicao pt-BR.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IVendaRepository, VendaRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

// Serve arquivos enviados em tempo de execucao (ex: imagens de produtos em wwwroot/uploads)
app.UseStaticFiles();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
