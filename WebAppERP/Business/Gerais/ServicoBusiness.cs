using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do servico. Usada pela tela de Servicos, pela pesquisa de
// servico (Compra, Venda e Agendamentos).
public class ServicoBusiness
{
    private readonly AppDbContext _db;
    private readonly CategoriaServicoBusiness _categoria;

    public ServicoBusiness(AppDbContext db, CategoriaServicoBusiness categoria)
    {
        _db = db;
        _categoria = categoria;
    }

    // ============================================================
    // Consultas
    // ============================================================

    // A tela lista todos; as listas para escolher, so os ativos.
    public async Task<List<Servico>> Listar(bool somenteAtivos = false) =>
        await _db.Servicos
            .Include(s => s.CategoriaServico)
            .Where(s => !somenteAtivos || s.FlAtivo)
            .OrderBy(s => s.DsServico)
            .ToListAsync();

    // Pesquisa (campo de servico): por codigo, nome ou categoria. Sem
    // "inativos" marcado traz so os ativos; marcado, so os inativos.
    public async Task<List<Servico>> Pesquisar(int? id, string? termo, bool somenteInativos = false)
    {
        var query = _db.Servicos.Include(s => s.CategoriaServico).Where(s => s.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(s => s.IdServico == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(Module.MontarFiltroTexto<Servico>(termo, s => s.DsServico, s => s.CategoriaServico!.NmCategoria));
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

        if (nome.Length == 0)
            throw new ValidacaoException("Informe o nome do serviço.");
        if (nome.Length > 200)
            throw new ValidacaoException("O nome do serviço deve ter no máximo 200 caracteres.");
        if (descricao?.Length > 500)
            throw new ValidacaoException("A descrição do serviço deve ter no máximo 500 caracteres.");
        // Categoria opcional; quando informada, do cadastro e ativa.
        if (servico.IdCategoriaServico is int idCategoria && !await _categoria.ExisteAtiva(idCategoria))
            throw new ValidacaoException("Categoria de serviço inválida.");
        if (servico.VlServico <= 0)
            throw new ValidacaoException("Informe o valor do serviço.", "O valor deve ser maior que zero.");
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
            registro = new Servico();
            _db.Servicos.Add(registro);
        }
        else
        {
            registro = await _db.Servicos.FindAsync(servico.IdServico)
                ?? throw new ValidacaoException("Serviço não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.DsServico = nome;
        registro.DsDescricao = descricao;
        registro.VlServico = servico.VlServico;
        registro.NrDuracaoMin = servico.NrDuracaoMin;
        registro.IdCategoriaServico = servico.IdCategoriaServico;
        registro.FlAtivo = servico.FlAtivo;

        await _db.SaveChangesAsync();
        await _db.Entry(registro).Reference(r => r.CategoriaServico).LoadAsync();
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
