using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace WebAppERP.Infrastructure;

// Um servidor da lista em Servers/servers.json.
public class ServidorBanco
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? host { get; set; }
    public string? user { get; set; }
    public string? pwd { get; set; }
    public string? database { get; set; }
    public bool selected { get; set; }
    public string[]? databases { get; set; }
}

// Conexao com o banco: a lista de servidores fica em Servers/servers.json e
// vale o marcado com "selected" (ou o primeiro).
// O arquivo e relido quando muda, entao trocar de banco nao exige recompilar
// nem reiniciar o sistema. Sem o arquivo, vale o DefaultConnection do appsettings.
public class ConexaoBanco
{
    private readonly string _caminho;
    private readonly string? _padrao;
    private readonly object _trava = new();
    private DateTime _lidoEm;
    private ServidorBanco? _servidor;

    public ConexaoBanco(IWebHostEnvironment env, IConfiguration config)
    {
        _caminho = Path.Combine(env.ContentRootPath, "Servers", "servers.json");
        _padrao = config.GetConnectionString("DefaultConnection");
    }

    // Servidor selecionado no servers.json (null quando o arquivo nao existe)
    public ServidorBanco? Servidor
    {
        get
        {
            lock (_trava)
            {
                if (!File.Exists(_caminho)) { _servidor = null; return null; }

                var alterado = File.GetLastWriteTimeUtc(_caminho);
                if (alterado != _lidoEm)
                {
                    var servidores = JsonSerializer.Deserialize<ServidorBanco[]>(File.ReadAllText(_caminho)) ?? [];
                    _servidor = servidores.FirstOrDefault(s => s.selected) ?? servidores.FirstOrDefault();
                    _lidoEm = alterado;
                }
                return _servidor;
            }
        }
    }

    public string StringConexao()
    {
        var s = Servidor;
        if (s == null)
            return _padrao ?? throw new InvalidOperationException(
                "Conexão com o banco não configurada: crie Servers/servers.json ou o DefaultConnection no appsettings.json.");

        var cs = new SqlConnectionStringBuilder
        {
            DataSource = s.host,
            InitialCatalog = s.database,
            Encrypt = false,
            TrustServerCertificate = true
        };

        // Com usuario: login do SQL (ex.: sa).
        // Sem usuario: login do Windows de quem roda o sistema.
        if (string.IsNullOrWhiteSpace(s.user))
            cs.IntegratedSecurity = true;
        else
        {
            cs.UserID = s.user;
            cs.Password = s.pwd;
        }

        return cs.ConnectionString;
    }
}
