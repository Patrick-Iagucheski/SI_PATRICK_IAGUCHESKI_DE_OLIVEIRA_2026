using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do servico. Usada pela tela de Servicos, pela pesquisa de
// servico da Compra e pelas listas de servicos (Agendamentos).
public class ServicoBusiness
{
    private readonly AppDbContext _db;

    public ServicoBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // A tela lista todos; as listas para escolher, so os ativos.
    public async Task<List<Servico>> Listar(bool somenteAtivos = false) =>
        await _db.Servicos
            .Where(s => !somenteAtivos || s.FlAtivo)
            .OrderBy(s => s.DsServico)
            .ToListAsync();

    // Pesquisa rapida (pesquisa de servico da Compra): so ativos, por codigo ou nome.
    public async Task<List<Servico>> Pesquisar(int? id, string? termo)
    {
        var query = _db.Servicos.Where(s => s.FlAtivo);

        if (id.HasValue)
            query = query.Where(s => s.IdServico == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(s => s.DsServico.Contains(t));
        }

        return await query.OrderBy(s => s.DsServico).Take(50).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdServico = 0) ou altera o servico e devolve o registro gravado.
    public async Task<Servico> Salvar(Servico servico)
    {
        var nome = servico.DsServico?.Trim() ?? string.Empty;
        var descricao = string.IsNullOrWhiteSpace(servico.DsDescricao) ? null : servico.DsDescricao.Trim();
        var categoria = string.IsNullOrWhiteSpace(servico.DsCategoria) ? null : servico.DsCategoria.Trim();

        if (nome.Length == 0)
            throw new ValidacaoException("Informe o nome do serviço.");
        if (nome.Length > 200)
            throw new ValidacaoException("O nome do serviço deve ter no máximo 200 caracteres.");
        if (descricao?.Length > 500)
            throw new ValidacaoException("A descrição do serviço deve ter no máximo 500 caracteres.");
        if (categoria?.Length > 100)
            throw new ValidacaoException("A categoria deve ter no máximo 100 caracteres.");
        if (servico.VlServico < 0)
            throw new ValidacaoException("O valor do serviço não pode ser negativo.");
        if (servico.NrDuracaoMin is <= 0 or > 1440)
            throw new ValidacaoException("Duração inválida.", "Informe a duração em minutos (de 1 a 1440).");

        // Mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetido = await _db.Servicos
            .Where(s => s.IdServico != servico.IdServico
                     && EF.Functions.Collate(s.DsServico, Module.CollationSemAcento) == nome)
            .Select(s => s.DsServico)
            .FirstOrDefaultAsync();
        if (repetido != null)
            throw new ValidacaoException($"Já existe o serviço \"{repetido}\".");

        Servico registro;
        if (servico.IdServico == 0)
        {
            registro = new Servico { DtCriacao = DateTime.Now };
            _db.Servicos.Add(registro);
        }
        else
        {
            registro = await _db.Servicos.FindAsync(servico.IdServico)
                ?? throw new ValidacaoException("Serviço não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
        }

        registro.DsServico = nome;
        registro.DsDescricao = descricao;
        registro.VlServico = servico.VlServico;
        registro.NrDuracaoMin = servico.NrDuracaoMin;
        registro.DsCategoria = categoria;
        registro.FlAtivo = servico.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico == null)
            return;

        const string dica = "Inative o serviço em vez de excluí-lo.";

        // Filhas: agendamentos, itens de venda e itens de compra (idServico).
        if (await _db.Agendamentos.AnyAsync(a => a.IdServico == id))
            throw new ValidacaoException("Não é possível excluir: existe agendamento com este serviço.", dica);
        if (await _db.VendaItens.AnyAsync(i => i.IdServico == id))
            throw new ValidacaoException("Não é possível excluir: este serviço consta em uma venda.", dica);
        if (await _db.CompraItens.AnyAsync(i => i.IdServico == id))
            throw new ValidacaoException("Não é possível excluir: este serviço consta em uma compra.", dica);

        try
        {
            _db.Servicos.Remove(servico);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este serviço.", dica);
        }
    }
}
