using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do transportador. Usada pela tela de Transportadores e pela
// pesquisa de transportador da Compra.
public class TransportadorBusiness
{
    private readonly AppDbContext _db;

    public TransportadorBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Transportador>> Listar() =>
        await _db.Transportadores
            .Include(t => t.Cidade).ThenInclude(c => c!.Estado)
            .OrderBy(t => t.DsRazaoSocial)
            .ToListAsync();

    public async Task<Transportador?> GetTransportador(int id) =>
        await _db.Transportadores.FindAsync(id);

    // Pesquisa rapida (pesquisa de transportador da Compra): por codigo ou razao social.
    public async Task<List<Transportador>> Pesquisar(int? id, string? termo)
    {
        var query = _db.Transportadores.AsQueryable();

        if (id.HasValue)
            query = query.Where(t => t.IdTransportador == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(Module.MontarFiltroTexto<Transportador>(termo, x => x.DsRazaoSocial));
        }

        return await query.OrderBy(t => t.DsRazaoSocial).Take(50).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdTransportador = 0) ou altera o transportador e devolve o registro gravado.
    public async Task<Transportador> Salvar(Transportador transportador)
    {
        var razao = transportador.DsRazaoSocial?.Trim() ?? string.Empty;
        var documento = Module.SomenteNumeros(transportador.NrCpfCnpj);
        var endereco = string.IsNullOrWhiteSpace(transportador.DsEndereco) ? null : transportador.DsEndereco.Trim();
        var inscricao = string.IsNullOrWhiteSpace(transportador.NrInscEstadual) ? null : transportador.NrInscEstadual.Trim();

        if (razao.Length == 0)
            throw new ValidacaoException("Informe a razão social do transportador.");
        if (razao.Length > 200)
            throw new ValidacaoException("A razão social deve ter no máximo 200 caracteres.");
        if (endereco?.Length > 200)
            throw new ValidacaoException("O endereço deve ter no máximo 200 caracteres.");
        if (inscricao?.Length > 20)
            throw new ValidacaoException("A inscrição estadual deve ter no máximo 20 caracteres.");

        // CPF (11 digitos) ou CNPJ (14 digitos), obrigatorio
        if (string.IsNullOrEmpty(documento))
            throw new ValidacaoException("Informe o CPF/CNPJ do transportador.");

        var valido = documento.Length switch
        {
            11 => Module.CpfValido(documento),
            14 => Module.CnpjValido(documento),
            _ => false
        };
        if (!valido)
            throw new ValidacaoException("CPF/CNPJ inválido. Verifique os números informados.");

        var mesmoDocumento = await _db.Transportadores
            .Where(t => t.NrCpfCnpj == documento && t.IdTransportador != transportador.IdTransportador)
            .Select(t => t.DsRazaoSocial)
            .FirstOrDefaultAsync();
        if (mesmoDocumento != null)
            throw new ValidacaoException($"Já existe um transportador com este CPF/CNPJ: {mesmoDocumento}.");

        if (transportador.IdCidade.HasValue && !await _db.Cidades.AnyAsync(c => c.IdCidade == transportador.IdCidade))
            throw new ValidacaoException("Cidade inválida.");

        Transportador registro;
        if (transportador.IdTransportador == 0)
        {
            registro = new Transportador();
            _db.Transportadores.Add(registro);
        }
        else
        {
            registro = await _db.Transportadores.FindAsync(transportador.IdTransportador)
                ?? throw new ValidacaoException("Transportador não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.DsRazaoSocial = razao;
        registro.NrCpfCnpj = documento;
        registro.DsEndereco = endereco;
        registro.IdCidade = transportador.IdCidade;
        registro.NrInscEstadual = inscricao;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var transportador = await _db.Transportadores.FindAsync(id);
        if (transportador == null)
            return;

        // Filhas: tbCmpCompras.idTransportador e tbFisNFe.idTransportador. A nota
        // fiscal nao tem DbSet, por isso esse caso cai no catch de DbUpdateException.
        if (await _db.Compras.AnyAsync(c => c.IdTransportador == id))
            throw new ValidacaoException("Não é possível excluir: existe compra com este transportador.");

        try
        {
            _db.Transportadores.Remove(transportador);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existe nota fiscal vinculada a este transportador.");
        }
    }
}
