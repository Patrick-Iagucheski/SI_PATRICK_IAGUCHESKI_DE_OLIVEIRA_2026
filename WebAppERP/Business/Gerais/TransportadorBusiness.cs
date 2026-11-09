using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio da transportadora. Usada pela tela de Transportadores, pelo
// componente de cadastro de transportadora e pela pesquisa de transportadora (Compra).
public class TransportadorBusiness
{
    private readonly AppDbContext _db;

    public TransportadorBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // Transportadoras para listas. Por padrao so as ativas.
    public async Task<List<Transportador>> Listar(bool somenteAtivos = true) =>
        await _db.Transportadores
            .Include(t => t.Cidade).ThenInclude(c => c!.Estado)
            .Where(t => !somenteAtivos || t.FlAtivo)
            .OrderBy(t => t.DsRazaoSocial)
            .ToListAsync();

    public async Task<Transportador?> GetTransportador(int id) =>
        await _db.Transportadores.FindAsync(id);

    // Pesquisa da tela de Transportadores: por codigo, razao social, nome fantasia
    // ou CPF/CNPJ/documento. Sem "inativos" marcado traz so as ativas; marcado, so as inativas.
    public async Task<List<Transportador>> Buscar(int? id, string? razao, string? fantasia, string? documento, bool somenteInativos)
    {
        var query = _db.Transportadores
            .Include(t => t.Cidade).ThenInclude(c => c!.Estado)
            .Where(t => t.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(t => t.IdTransportador == id.Value);

        if (!string.IsNullOrWhiteSpace(razao))
        {
            query = query.Where(Module.MontarFiltroTexto<Transportador>(razao, t => t.DsRazaoSocial));
        }

        if (!string.IsNullOrWhiteSpace(fantasia))
        {
            query = query.Where(Module.MontarFiltroTexto<Transportador>(fantasia, t => t.DsNomeFantasia));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var dig = Module.SomenteNumeros(documento) ?? "";
            if (dig != "")
                query = query.Where(t =>
                    (t.NrCpf != null && t.NrCpf.Contains(dig)) ||
                    (t.NrCNPJ != null && t.NrCNPJ.Contains(dig)) ||
                    (t.NrDocumento != null && t.NrDocumento.Contains(dig)));
            else
            {
                query = query.Where(Module.MontarFiltroTexto<Transportador>(documento, t => t.NrDocumento));
            }
        }

        return await query.OrderBy(t => t.DsRazaoSocial).Take(100).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdTransportador = 0) ou altera a transportadora e devolve o registro gravado.
    public async Task<Transportador> Salvar(Transportador transportador)
    {
        Normalizar(transportador);
        await Validar(transportador);

        Transportador registro;
        if (transportador.IdTransportador == 0)
        {
            registro = new Transportador();
            _db.Transportadores.Add(registro);
        }
        else
        {
            registro = await _db.Transportadores.FindAsync(transportador.IdTransportador)
                ?? throw new ValidacaoException("Transportadora não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
        }

        registro.TpPessoa = transportador.TpPessoa;
        registro.DsRazaoSocial = transportador.DsRazaoSocial;
        registro.DsNomeFantasia = transportador.DsNomeFantasia;
        registro.NrCpf = transportador.NrCpf;
        registro.NrCNPJ = transportador.NrCNPJ;
        registro.NrDocumento = transportador.NrDocumento;
        registro.NrInscEstadual = transportador.NrInscEstadual;
        registro.NrInscMunicipal = transportador.NrInscMunicipal;
        registro.NrRG = transportador.NrRG;
        registro.DtNascimento = transportador.DtNascimento;
        registro.DsEmail = transportador.DsEmail;
        registro.DsObservacao = transportador.DsObservacao;
        registro.FlAtivo = transportador.FlAtivo;
        registro.NrCEP = transportador.NrCEP;
        registro.DsEndereco = transportador.DsEndereco;
        registro.NrNumero = transportador.NrNumero;
        registro.DsComplemento = transportador.DsComplemento;
        registro.DsBairro = transportador.DsBairro;
        registro.IdCidade = transportador.IdCidade;
        registro.NrTelefoneFixo = transportador.NrTelefoneFixo;
        registro.NrCelular = transportador.NrCelular;
        registro.DsSite = transportador.DsSite;

        await _db.SaveChangesAsync();
        return registro;
    }

    // Tipo de pessoa valido, mascaras removidas, so os campos do tipo escolhido
    // e cada campo no tamanho da coluna.
    private static void Normalizar(Transportador t)
    {
        // F = Fisica, J = Juridica, E = Estrangeiro, O = Outro
        var tipo = (t.TpPessoa ?? "J").Trim().ToUpper();
        if (tipo is not ("F" or "J" or "E" or "O")) tipo = "J";
        t.TpPessoa = tipo;

        t.NrCpf = Module.SomenteNumeros(t.NrCpf);
        t.NrCNPJ = Module.SomenteNumeros(t.NrCNPJ);
        t.NrCEP = Module.SomenteNumeros(t.NrCEP);
        t.NrTelefoneFixo = Module.SomenteNumeros(t.NrTelefoneFixo);
        t.NrCelular = Module.SomenteNumeros(t.NrCelular);
        t.NrRG = Module.SomenteNumeros(t.NrRG);

        // PJ (J)  -> CNPJ, Nome Fantasia, Inscricao Estadual, Inscricao Municipal
        // PF (F)  -> CPF, RG, Data de Nascimento
        // E / O   -> Documento, Inscricao Estadual, Data de Nascimento
        switch (tipo)
        {
            case "F":
                t.NrCNPJ = null; t.NrDocumento = null; t.NrInscEstadual = null; t.NrInscMunicipal = null;
                t.DsNomeFantasia = null;
                break;
            case "J":
                t.NrCpf = null; t.NrDocumento = null; t.NrRG = null; t.DtNascimento = null;
                break;
            default:
                t.NrCpf = null; t.NrCNPJ = null; t.NrRG = null; t.NrInscMunicipal = null;
                t.DsNomeFantasia = null;
                break;
        }

        // Tamanhos das colunas de tbGerTransportadores
        t.DsRazaoSocial = Module.Truncar(t.DsRazaoSocial?.Trim(), 100) ?? string.Empty;
        t.DsNomeFantasia = Module.Truncar(t.DsNomeFantasia, 150);
        t.NrCpf = Module.Truncar(t.NrCpf, 11);
        t.NrCNPJ = Module.Truncar(t.NrCNPJ, 14);
        t.NrDocumento = Module.Truncar(t.NrDocumento, 30);
        t.NrInscEstadual = Module.Truncar(t.NrInscEstadual, 13);
        t.NrInscMunicipal = Module.Truncar(t.NrInscMunicipal, 15);
        t.NrRG = Module.Truncar(t.NrRG, 9);
        t.DsEmail = Module.Truncar(t.DsEmail, 150);
        t.DsObservacao = Module.Truncar(t.DsObservacao, 500);
        t.NrCEP = Module.Truncar(t.NrCEP, 8);
        t.DsEndereco = Module.Truncar(t.DsEndereco, 100);
        t.NrNumero = Module.Truncar(t.NrNumero, 10);
        t.DsComplemento = Module.Truncar(t.DsComplemento, 100);
        t.DsBairro = Module.Truncar(t.DsBairro, 100);
        t.NrTelefoneFixo = Module.Truncar(t.NrTelefoneFixo, 11);
        t.NrCelular = Module.Truncar(t.NrCelular, 11);
        t.DsSite = Module.Truncar(t.DsSite?.Trim(), 150);
    }

    private async Task Validar(Transportador t)
    {
        if (string.IsNullOrWhiteSpace(t.DsRazaoSocial))
            throw new ValidacaoException("Informe a Razão Social / Nome da transportadora.");

        // Documento do tipo de pessoa obrigatorio: CPF na fisica, CNPJ na juridica.
        if (t.TpPessoa == "F")
        {
            if (string.IsNullOrEmpty(t.NrCpf))
                throw new ValidacaoException("Informe o CPF da transportadora.");
            if (!Module.CpfValido(t.NrCpf))
                throw new ValidacaoException("CPF inválido. Verifique os números informados.");
        }

        if (t.TpPessoa == "J")
        {
            if (string.IsNullOrEmpty(t.NrCNPJ))
                throw new ValidacaoException("Informe o CNPJ da transportadora.");
            if (!Module.CnpjValido(t.NrCNPJ))
                throw new ValidacaoException("CNPJ inválido. Verifique os números informados.");
        }

        // RG opcional; quando informado (Pessoa Fisica), com 9 digitos.
        if (t.TpPessoa == "F" && !string.IsNullOrEmpty(t.NrRG) && t.NrRG.Length != 9)
            throw new ValidacaoException("RG inválido. Informe 9 dígitos.");

        // Inscricoes para Pessoa Juridica (validadas apenas quando preenchidas)
        if (t.TpPessoa == "J")
        {
            if (!string.IsNullOrEmpty(t.NrInscEstadual) && (t.NrInscEstadual.Length < 8 || !t.NrInscEstadual.All(char.IsDigit)))
                throw new ValidacaoException("Inscrição Estadual deve ter de 8 a 13 dígitos.");
            if (!string.IsNullOrEmpty(t.NrInscMunicipal) && t.NrInscMunicipal.Length < 7)
                throw new ValidacaoException("Inscrição Municipal deve ter de 7 a 15 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(t.DsEmail) && !new EmailAddressAttribute().IsValid(t.DsEmail))
            throw new ValidacaoException("E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).");

        if (t.IdCidade.HasValue && !await _db.Cidades.AnyAsync(c => c.IdCidade == t.IdCidade))
            throw new ValidacaoException("Cidade inválida.");

        // Mesmo CPF/CNPJ/documento ja cadastrado em outra transportadora.
        var documento = t.TpPessoa switch
        {
            "F" => t.NrCpf,
            "J" => t.NrCNPJ,
            _ => t.NrDocumento
        };
        if (!string.IsNullOrEmpty(documento))
        {
            var duplicado = await _db.Transportadores
                .Where(x => x.IdTransportador != t.IdTransportador
                         && (x.NrCpf == documento || x.NrCNPJ == documento || x.NrDocumento == documento))
                .Select(x => x.DsRazaoSocial)
                .FirstOrDefaultAsync();
            if (duplicado != null)
                throw new ValidacaoException($"Já existe uma transportadora cadastrada com este documento: {duplicado}.");
        }
    }

    public async Task Excluir(int id)
    {
        var transportador = await _db.Transportadores.FindAsync(id);
        if (transportador == null)
            return;

        // Filhas: tbCmpCompras.idTransportador e tbFisNFe.idTransportador. A nota
        // fiscal nao tem DbSet, por isso esse caso cai no catch de DbUpdateException.
        const string dica = "Inative a transportadora em vez de excluí-la.";
        if (await _db.Compras.AnyAsync(c => c.IdTransportador == id))
            throw new ValidacaoException("Não é possível excluir: existe compra com esta transportadora.", dica);

        try
        {
            _db.Transportadores.Remove(transportador);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existe nota fiscal vinculada a esta transportadora.", dica);
        }
    }
}
