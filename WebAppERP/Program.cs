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
// Conexao no padrao do SoftTur: servidor selecionado em Servers/servers.json.
// As opcoes sao montadas a cada requisicao, entao editar o arquivo ja troca o banco.
builder.Services.AddSingleton<ConexaoBanco>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<ConexaoBanco>().StringConexao()));

builder.Services.AddScoped<IVendaRepository, VendaRepository>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();

// Nome e logo da empresa do layout (lidos do banco, em cache)
builder.Services.AddMemoryCache();
builder.Services.AddScoped<EmpresaService>();

var app = builder.Build();

// Mostra no console qual banco esta em uso (ajuda a conferir o servers.json)
var servidor = app.Services.GetRequiredService<ConexaoBanco>().Servidor;
app.Logger.LogInformation(servidor == null
    ? "Banco: DefaultConnection do appsettings.json (Servers/servers.json nao encontrado)"
    : $"Banco: {servidor.name} -> {servidor.host} / {servidor.database} (Servers/servers.json)");

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
