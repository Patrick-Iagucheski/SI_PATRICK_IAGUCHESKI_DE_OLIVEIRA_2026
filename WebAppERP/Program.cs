using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Infrastructure;
using WebAppERP.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Limite de upload (ex.: imagem do produto): ate 500 MB.
// Sem isso valem os padroes (Kestrel ~30 MB e multipart 128 MB), que barram
// arquivos maiores antes de chegarem no handler da pagina.
const long tamanhoMaxUpload = 500L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = tamanhoMaxUpload);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = tamanhoMaxUpload;
    options.ValueLengthLimit = int.MaxValue;
});

// Parsing invariante de numeros (decimal/double/float) para casar com o formato
// enviado por <input type="number"> (ponto decimal), sem alterar a exibicao pt-BR.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IVendaRepository, VendaRepository>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();

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
