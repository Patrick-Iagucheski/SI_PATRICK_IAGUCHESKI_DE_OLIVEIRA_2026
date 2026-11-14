using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.DTOs.Compras;
using WebAppERP.DTOs.Estoque;

namespace WebAppERP.Business.Estoque;

// Regras da movimentacao de estoque. O estoque so entra pela compra e so sai
// pela venda (nao ha lancamento manual): as duas passam por Registrar, o saldo
// muda e o movimento fica gravado, para o Balancete e a tela de Movimentacao
// (so consulta) mostrarem de onde veio cada entrada e saida.
public class MovimentacaoEstoqueBusiness
{
    private readonly AppDbContext _db;

    public MovimentacaoEstoqueBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Registro (chamado pela compra e pela venda, dentro da transacao de quem
    // chama; quem chama grava)
    // ============================================================

    // quantidade: positiva entra, negativa sai. estorno: desfaz uma compra ou
    // venda. venda/compra: a origem do movimento (uma das duas ou nenhuma).
    // data: quando o movimento aconteceu (padrao: agora). A entrada da compra usa
    // a chegada da nota: lancada depois, o saldo dos movimentos seguintes a ela
    // e ajustado, para o saldo de cada movimento seguir a ordem das datas.
    // O custo medio do produto ja deve estar recalculado por quem chama.
    public async Task Registrar(Produto produto, decimal quantidade, string origem, bool estorno, decimal? custoUnitario,
                                Venda? venda = null, CompraChaveDTO? compra = null, DateTime? data = null)
    {
        if (quantidade == 0) return;

        var agora = DateTime.Now;
        var quando = data is DateTime d && d < agora ? d : agora;

        produto.QtSaldo = (produto.QtSaldo ?? 0) + quantidade;
        var saldo = produto.QtSaldo.Value;

        if (quando < agora)
        {
            // Saldo naquela data: movimentos gravados e os ainda nao gravados desta transacao.
            var gravados = await _db.Movimentacoes
                .Where(m => m.IdProduto == produto.IdProduto && m.DtMovimento <= quando)
                .SumAsync(m => m.TpMovimento == MovimentacaoEstoque.ENTRADA ? m.QtMovimento : -m.QtMovimento);
            var pendentes = _db.ChangeTracker.Entries<MovimentacaoEstoque>()
                .Where(e => e.State == EntityState.Added && e.Entity.IdProduto == produto.IdProduto && e.Entity.DtMovimento <= quando)
                .Sum(e => e.Entity.QtComSinal);
            saldo = gravados + pendentes + quantidade;

            await _db.Movimentacoes
                .Where(m => m.IdProduto == produto.IdProduto && m.DtMovimento > quando)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.QtSaldo, m => m.QtSaldo + quantidade));
        }

        _db.Movimentacoes.Add(new MovimentacaoEstoque
        {
            IdProduto = produto.IdProduto,
            DtMovimento = quando,
            TpMovimento = quantidade > 0 ? MovimentacaoEstoque.ENTRADA : MovimentacaoEstoque.SAIDA,
            DsOrigem = origem,
            QtMovimento = Math.Abs(quantidade),
            QtSaldo = saldo,
            VlCustoUnitario = custoUnitario,
            VlCustoMedio = produto.VlCusto,
            FlEstorno = estorno,
            Venda = venda,
            NrNota = compra?.NrNota,
            NrSerie = compra?.NrSerie,
            NrModelo = compra?.NrModelo,
            IdFornecedor = compra?.IdFornecedor
        });
    }

    // ============================================================
    // Consultas
    // ============================================================

    // Movimentos do periodo, o mais recente primeiro (ate 500).
    public async Task<List<MovimentacaoEstoque>> Buscar(DateOnly inicio, DateOnly fim, int? idProduto, string? origem, string? tipo)
    {
        ValidarPeriodo(inicio, fim);

        var de = inicio.ToDateTime(TimeOnly.MinValue);
        var ate = fim.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var query = _db.Movimentacoes
            .Include(m => m.Produto).ThenInclude(p => p!.UnidadeMedida)
            .Where(m => m.DtMovimento >= de && m.DtMovimento < ate);
        if (idProduto.HasValue)
            query = query.Where(m => m.IdProduto == idProduto.Value);
        if (!string.IsNullOrWhiteSpace(origem))
            query = query.Where(m => m.DsOrigem == origem.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(m => m.TpMovimento == tipo.Trim().ToUpper());

        return await query.OrderByDescending(m => m.DtMovimento).ThenByDescending(m => m.IdMovimentacao)
                          .Take(500).ToListAsync();
    }

    // Periodo das consultas (Movimentacao e Balancete): datas entre 2000 e 2100.
    // Data absurda (ex.: 31/12/9999) derrubava a tela ao somar o dia final.
    public static readonly DateOnly DataMinima = new(2000, 1, 1);
    public static readonly DateOnly DataMaxima = new(2100, 12, 31);

    private static void ValidarPeriodo(DateOnly inicio, DateOnly fim)
    {
        if (inicio < DataMinima || inicio > DataMaxima || fim < DataMinima || fim > DataMaxima)
            throw new ValidacaoException("Data inválida.", $"Informe datas entre {DataMinima:dd/MM/yyyy} e {DataMaxima:dd/MM/yyyy}.");
        if (fim < inicio)
            throw new ValidacaoException("A data final não pode ser anterior à data inicial.");
    }

    // Custo medio ponderado, conforme definido em aula (entrada da compra):
    //   custoMedio = (saldo * custo + qtde * custoUnit) / (saldo + qtde)
    // Na compra, custoUnit e o VlCustoUnitario do item, que ja sai com o
    // desconto e com o rateio de frete/seguro/outros. No estorno qtde vem
    // negativa, o que desfaz a media daquela entrada.
    // Retorna null quando a conta nao se aplica (saldo final zerado ou custo
    // negativo); nesse caso o custo atual do produto e mantido.
    public static decimal? CalcularCustoMedio(decimal saldo, decimal custo, decimal qtde, decimal custoUnit)
    {
        // Saldo negativo nao tem custo a ponderar: entra na media como zero.
        if (saldo < 0) saldo = 0;

        var saldoFinal = saldo + qtde;
        if (saldoFinal <= 0) return null;

        var custoMedio = (saldo * custo + qtde * custoUnit) / saldoFinal;
        if (custoMedio < 0) return null;

        // 6 casas, igual ao custo unitario do item da compra.
        return Math.Round(custoMedio, 6, MidpointRounding.AwayFromZero);
    }

    // Produto para movimentar, com a linha travada ate o fim da transacao de quem
    // chama: duas gravacoes do mesmo produto ao mesmo tempo (venda e compra, por
    // exemplo) esperam uma pela outra, e saldo e custo sempre partem do valor
    // gravado mais recente.
    public async Task<Produto?> ProdutoTravado(int idProduto)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM dbo.tbEstProdutos WITH (UPDLOCK, ROWLOCK) WHERE idProduto = {idProduto}");
        var produto = await _db.Produtos.FindAsync(idProduto);
        // Ja carregado antes da trava (e sem alteracao nesta gravacao): le de novo.
        if (produto != null && _db.Entry(produto).State == EntityState.Unchanged)
            await _db.Entry(produto).ReloadAsync();
        return produto;
    }

    public Task<bool> ProdutoTemMovimento(int idProduto) =>
        _db.Movimentacoes.AnyAsync(m => m.IdProduto == idProduto);

    public Task<bool> CompraTemMovimento(CompraChaveDTO chave) =>
        _db.Movimentacoes.AnyAsync(m => m.NrNota == chave.NrNota && m.NrSerie == chave.NrSerie
                                     && m.NrModelo == chave.NrModelo && m.IdFornecedor == chave.IdFornecedor);

    // ============================================================
    // Balancete
    // ============================================================

    // Por produto: saldo antes do periodo, compras e vendas no periodo e saldo
    // final. Lista os produtos ativos e os inativos que tiveram
    // movimento ate o fim do periodo.
    //   saldo: NEGATIVO, ZERADO, POSITIVO ou vazio (todos), sobre o saldo final.
    //   somenteComMovimento: so quem movimentou dentro do periodo.
    public async Task<List<BalanceteEstoqueDTO>> GetBalancete(DateOnly inicio, DateOnly fim, int? idProduto,
                                                              string? saldo, bool somenteComMovimento)
    {
        ValidarPeriodo(inicio, fim);

        var de = inicio.ToDateTime(TimeOnly.MinValue);
        var ate = fim.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var movimentos = _db.Movimentacoes.Where(m => m.DtMovimento < ate);
        if (idProduto.HasValue)
            movimentos = movimentos.Where(m => m.IdProduto == idProduto.Value);

        // Cada coluna soma as quantidades com sinal; vendas viram positivas
        // (saida), e o estorno entra com o sinal contrario.
        var somas = await movimentos
            .GroupBy(m => m.IdProduto)
            .Select(g => new
            {
                IdProduto = g.Key,
                Inicial = g.Sum(m => m.DtMovimento < de
                    ? (m.TpMovimento == MovimentacaoEstoque.ENTRADA ? m.QtMovimento : -m.QtMovimento) : 0m),
                Compras = g.Sum(m => m.DtMovimento >= de && m.DsOrigem == MovimentacaoEstoque.COMPRA
                    ? (m.TpMovimento == MovimentacaoEstoque.ENTRADA ? m.QtMovimento : -m.QtMovimento) : 0m),
                Vendas = g.Sum(m => m.DtMovimento >= de && m.DsOrigem == MovimentacaoEstoque.VENDA
                    ? (m.TpMovimento == MovimentacaoEstoque.SAIDA ? m.QtMovimento : -m.QtMovimento) : 0m),
                Movimentos = g.Count(m => m.DtMovimento >= de),
                // Custo medio no fim do periodo: o do ultimo movimento ate la.
                CustoMedio = g.OrderByDescending(m => m.DtMovimento).ThenByDescending(m => m.IdMovimentacao)
                              .Select(m => m.VlCustoMedio).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.IdProduto);

        var produtos = await _db.Produtos
            .Include(p => p.UnidadeMedida)
            .Where(p => idProduto == null || p.IdProduto == idProduto)
            .OrderBy(p => p.DsProduto)
            .ToListAsync();

        var linhas = new List<BalanceteEstoqueDTO>();
        foreach (var p in produtos)
        {
            somas.TryGetValue(p.IdProduto, out var s);
            if (!p.FlAtivo && s == null) continue;

            linhas.Add(new BalanceteEstoqueDTO
            {
                IdProduto = p.IdProduto,
                DsProduto = p.DsProduto,
                SgUnidade = p.UnidadeMedida?.SgUnidade,
                FlAtivo = p.FlAtivo,
                QtSaldoInicial = s?.Inicial ?? 0,
                QtCompras = s?.Compras ?? 0,
                QtVendas = s?.Vendas ?? 0,
                QtMovimentos = s?.Movimentos ?? 0,
                VlCustoMedio = s?.CustoMedio ?? p.VlCusto ?? 0
            });
        }

        IEnumerable<BalanceteEstoqueDTO> resultado = linhas;
        if (somenteComMovimento)
            resultado = resultado.Where(l => l.QtMovimentos > 0);

        resultado = (saldo ?? "").Trim().ToUpper() switch
        {
            BalanceteEstoqueDTO.SALDO_NEGATIVO => resultado.Where(l => l.QtSaldoFinal < 0),
            BalanceteEstoqueDTO.SALDO_ZERADO => resultado.Where(l => l.QtSaldoFinal == 0),
            BalanceteEstoqueDTO.SALDO_POSITIVO => resultado.Where(l => l.QtSaldoFinal > 0),
            _ => resultado
        };

        return resultado.ToList();
    }
}
