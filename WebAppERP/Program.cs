using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Business.Compras;
using WebAppERP.Business.Estoque;
using WebAppERP.Business.Financeiro;
using WebAppERP.Business.Gerais;
using WebAppERP.Business.Operacional;
using WebAppERP.Business.Principal;
using WebAppERP.Business.Seguranca;
using WebAppERP.Business.Vendas;
using WebAppERP.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Servicos do sistema (injecao de dependencia).
builder.Services.AddRazorPages();

// Chaves da protecao de dados (token de seguranca dos formularios, mensagens
// entre telas): gravadas numa pasta (appsettings "ProtecaoDados:PastaChaves").
// Sem isso o IIS cria chaves novas a cada reinicio (publicacao, reciclagem do
// pool) e toda tela que estava aberta passa a falhar ao salvar.
var pastaChaves = builder.Configuration["ProtecaoDados:PastaChaves"];
if (!string.IsNullOrWhiteSpace(pastaChaves))
{
    var protecao = builder.Services.AddDataProtection()
        .SetApplicationName("WebAppERP")
        .PersistKeysToFileSystem(new DirectoryInfo(pastaChaves));
    // No Windows as chaves ficam cifradas para a maquina (IIS e Visual Studio leem).
    if (OperatingSystem.IsWindows())
        protecao.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

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
// Conexao com o banco: servidor selecionado em Servers/servers.json.
// As opcoes sao montadas a cada requisicao, entao editar o arquivo ja troca o banco.
builder.Services.AddSingleton<ConexaoBanco>();
builder.Services.AddSingleton<ArquivoService>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<ConexaoBanco>().GetStringConexao()));

// Regras de negocio por modulo (Business/<Modulo>)
builder.Services.AddScoped<VendaBusiness>();
builder.Services.AddScoped<CompraBusiness>();
builder.Services.AddScoped<CargoBusiness>();
builder.Services.AddScoped<PaisBusiness>();
builder.Services.AddScoped<EstadoBusiness>();
builder.Services.AddScoped<CidadeBusiness>();
builder.Services.AddScoped<ClienteBusiness>();
builder.Services.AddScoped<FornecedorBusiness>();
builder.Services.AddScoped<FuncionarioBusiness>();
builder.Services.AddScoped<ServicoBusiness>();
builder.Services.AddScoped<CategoriaServicoBusiness>();
builder.Services.AddScoped<TransportadorBusiness>();
builder.Services.AddScoped<AgendamentoBusiness>();
builder.Services.AddScoped<VeiculoBusiness>();
builder.Services.AddScoped<EmpresaBusiness>();
builder.Services.AddScoped<PrincipalBusiness>();
builder.Services.AddScoped<MarcaBusiness>();
builder.Services.AddScoped<CategoriaBusiness>();
builder.Services.AddScoped<UnidadeMedidaBusiness>();
builder.Services.AddScoped<ProdutoBusiness>();
builder.Services.AddScoped<MovimentacaoEstoqueBusiness>();
builder.Services.AddScoped<FormaPagamentoBusiness>();
builder.Services.AddScoped<CondicaoPagamentoBusiness>();
builder.Services.AddScoped<ContaAPagarBusiness>();
builder.Services.AddScoped<ContaAReceberBusiness>();

// Nome e logo da empresa do layout (lidos do banco, em cache)
builder.Services.AddMemoryCache();
builder.Services.AddScoped<EmpresaService>();

// Usuario gravado como quem incluiu / alterou os registros (AppDbContext).
builder.Services.AddScoped<UsuarioAtual>();

var app = builder.Build();

// Mostra no console qual banco esta em uso (ajuda a conferir o servers.json)
var servidor = app.Services.GetRequiredService<ConexaoBanco>().Servidor;
app.Logger.LogInformation(servidor == null
    ? "Banco: DefaultConnection do appsettings.json (Servers/servers.json não encontrado)"
    : $"Banco: {servidor.name} -> {servidor.host} / {servidor.database} (Servers/servers.json)");

// Numeros, moeda e datas sempre no formato brasileiro (R$ 1.675,44), qualquer
// que seja o idioma do Windows ou do IIS onde o sistema rodar.
var ptBR = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBR;
CultureInfo.DefaultThreadCurrentUICulture = ptBR;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBR),
    SupportedCultures = [ptBR],
    SupportedUICultures = [ptBR]
});

// Pipeline das requisicoes.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

// Arquivos enviados pelas telas (imagens de produtos, logo): pasta unica fora
// do site (ArquivoService.PastaFisica), servida no endereco /uploads.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(app.Services.GetRequiredService<ArquivoService>().PastaFisica),
    RequestPath = "/" + ArquivoService.Raiz
});
app.UseStaticFiles();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
