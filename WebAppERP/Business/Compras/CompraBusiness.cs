using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.DTOs.Compras;

namespace WebAppERP.Business.Compras;

// Regras de negocio da compra. Toda a gravacao definitiva acontece dentro
// de uma unica transacao: compra + itens + parcelas + contas a pagar +
// estoque. Se qualquer etapa falhar, nada e gravado (rollback).
public class CompraBusiness
{
    private readonly AppDbContext _db;

    public CompraBusiness(AppDbContext db) => _db = db;

    // Tolerancia de arredondamento na conferencia das parcelas (1 centavo).
    private const decimal ToleranciaParcelas = 0.01m;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Compra>> Listar() =>
        await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Transportador)
            .OrderByDescending(c => c.DtEmissao)
            .ThenByDescending(c => c.NrNota)
            .ToListAsync();

    public async Task<Compra?> GetCompraCompleta(CompraChaveDTO chave) =>
        await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Transportador)
            .Include(c => c.CondicaoPagamento)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .Include(c => c.Itens).ThenInclude(i => i.Servico)
            .Include(c => c.Pagamentos).ThenInclude(p => p.FormaPagamento)
            .Include(c => c.Pagamentos).ThenInclude(p => p.CondicaoPagamento)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

    // Titulos que esta compra gerou no Contas a Pagar.
    public async Task<List<ContaAPagar>> GetContasGeradas(CompraChaveDTO chave) =>
        await _db.ContasAPagar
            .Include(c => c.FormaPagamento)
            .Where(c => c.NrNota == chave.NrNota
                     && c.NrSerie == chave.NrSerie
                     && c.NrModelo == chave.NrModelo
                     && c.IdFornecedor == chave.IdFornecedor)
            .OrderBy(c => c.NrParcela)
            .ToListAsync();

    // Situacao atual da compra (RASCUNHO, FINALIZADA, CANCELADA); null se a
    // chave ainda nao foi gravada.
    public async Task<string?> GetSituacao(CompraChaveDTO chave) =>
        await _db.Compras
            .Where(c => c.NrNota == chave.NrNota
                     && c.NrSerie == chave.NrSerie
                     && c.NrModelo == chave.NrModelo
                     && c.IdFornecedor == chave.IdFornecedor)
            .Select(c => c.TpStatus)
            .FirstOrDefaultAsync();

    // Validacao da chave de uma compra NOVA, feita pela tela antes de liberar
    // os demais campos: chave completa, fornecedor ativo e chave ainda nao usada.
    public async Task<(bool ok, string? erro, string? nomeFornecedor)> ValidarChaveNova(CompraChaveDTO chave)
    {
        if (chave.NrModelo <= 0)
            return (false, "Informe o modelo da nota.", null);
        if (chave.NrSerie < 0)
            return (false, "Informe a série da nota.", null);
        if (chave.NrNota <= 0)
            return (false, "Informe o número da nota.", null);
        if (chave.IdFornecedor <= 0)
            return (false, "Informe o fornecedor.", null);

        var fornecedor = await _db.Fornecedores
            .Where(f => f.IdFornecedor == chave.IdFornecedor)
            .Select(f => new { f.DsRazaoSocial, f.FlAtivo })
            .FirstOrDefaultAsync();

        if (fornecedor == null)
            return (false, "Fornecedor informado não existe.", null);
        if (!fornecedor.FlAtivo)
            return (false, "Fornecedor informado está inativo.", null);

        var existente = await _db.Compras
            .Where(c => c.NrNota == chave.NrNota
                     && c.NrSerie == chave.NrSerie
                     && c.NrModelo == chave.NrModelo
                     && c.IdFornecedor == chave.IdFornecedor)
            .Select(c => c.TpStatus)
            .FirstOrDefaultAsync();

        if (existente != null)
            return (false, $"Já existe uma compra ({existente}) com modelo {chave.NrModelo}, série {chave.NrSerie}, "
                         + $"número {chave.NrNota} para este fornecedor.", fornecedor.DsRazaoSocial);

        return (true, null, fornecedor.DsRazaoSocial);
    }

    public async Task<CompraCombosDTO> GetCombos() => new()
    {
        Fornecedores = await _db.Fornecedores.Where(f => f.FlAtivo).OrderBy(f => f.DsRazaoSocial).ToListAsync(),
        Transportadores = await _db.Transportadores.OrderBy(t => t.DsRazaoSocial).ToListAsync(),
        Produtos = await _db.Produtos.Where(p => p.FlAtivo).OrderBy(p => p.DsProduto).ToListAsync(),
        Servicos = await _db.Servicos.Where(s => s.FlAtivo).OrderBy(s => s.DsServico).ToListAsync(),
        Condicoes = await _db.CondicoesPagamento.Where(c => c.FlAtivo).OrderBy(c => c.DsCondicao).ToListAsync(),
        Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync()
    };

    // ============================================================
    // Gravacao
    // ============================================================

    // Grava a compra. TpStatus RASCUNHO nao gera Contas a Pagar nem estoque;
    // FINALIZADA grava tudo de forma atomica (compra + itens + parcelas +
    // contas a pagar + estoque).
    public async Task<(bool ok, string? erro, CompraChaveDTO chave)> Salvar(CompraDTO input)
    {
        var chave = input.GetChave();
        var finalizar = input.TpStatus == Compra.FINALIZADA;

        // ---------- 1. Validacao do cabecalho ----------
        var (cabecalhoOk, erroCabecalho) = await ValidarCabecalho(input);
        if (!cabecalhoOk) return (false, erroCabecalho, chave);

        // A chave identifica a compra: alterar qualquer parte dela seria
        // criar outra compra. Exige cancelar e lancar uma nova.
        if (input.ChaveOriginal is { Preenchida: true })
        {
            var orig = input.ChaveOriginal;
            if (orig.NrNota != chave.NrNota || orig.NrSerie != chave.NrSerie
                || orig.NrModelo != chave.NrModelo || orig.IdFornecedor != chave.IdFornecedor)
                return (false, "Modelo, série, número e fornecedor identificam a compra e não podem ser alterados. "
                             + "Cancele esta compra e lance uma nova.", orig);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        // ---------- 2. Situacao atual ----------
        var existente = await _db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Pagamentos)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

        // Inclusao sobre chave ja existente: a PK barraria, mas a mensagem
        // amigavel vem daqui. O catch de DbUpdateException cobre a corrida.
        if (existente == null && input.ChaveOriginal is { Preenchida: true })
            return (false, "Compra não encontrada para edição.", chave);

        if (existente != null && input.ChaveOriginal is not { Preenchida: true })
            return (false, $"Já existe uma compra com modelo {chave.NrModelo}, série {chave.NrSerie}, "
                         + $"número {chave.NrNota} para este fornecedor.", chave);

        if (existente is { TpStatus: Compra.CANCELADA })
            return (false, "Compra cancelada não pode ser editada.", chave);

        // ---------- 3. Estorno do que a versao anterior gerou ----------
        var contasAnteriores = await GetContasGeradas(chave);

        if (contasAnteriores.Any(c => c.DsStatus == ContaAPagar.PAGO || c.VlPago > 0))
            return (false, "Esta compra possui título já pago/baixado no Contas a Pagar. "
                         + "Estorne o pagamento antes de alterar a compra.", chave);

        if (existente is { TpStatus: Compra.FINALIZADA })
        {
            // Devolve o estoque que a versao anterior deu entrada.
            var (estornoOk, erroEstorno) = await AjustarEstoque(existente.Itens, -1, validar: true);
            if (!estornoOk) return (false, erroEstorno, chave);

            // Os titulos serao regerados a partir das novas parcelas.
            if (contasAnteriores.Count > 0)
                _db.ContasAPagar.RemoveRange(contasAnteriores);
        }

        // ---------- 4. Itens (recalculados no servidor) ----------
        var (itensOk, erroItens, novosItens, totalProdutos) = MontarItens(input, chave);
        if (!itensOk) return (false, erroItens, chave);

        var custoAdicional = input.VlFrete + input.VlSeguro + input.VlOutrasDespesas;
        var totalCompra = totalProdutos + custoAdicional;
        RatearCustoAdicional(novosItens, custoAdicional);

        // ---------- 5. Parcelas ----------
        var (parcelasOk, erroParcelas, novasParcelas) = MontarParcelas(input, chave);
        if (!parcelasOk) return (false, erroParcelas, chave);

        // ---------- 6. Regras exclusivas da finalizacao ----------
        if (finalizar)
        {
            if (novosItens.Count == 0)
                return (false, "Adicione ao menos um produto/serviço para finalizar a compra.", chave);

            if (novasParcelas.Count == 0)
                return (false, "Informe ao menos uma parcela de pagamento para finalizar a compra.", chave);

            var somaParcelas = novasParcelas.Sum(p => p.VlParcela);
            if (Math.Abs(somaParcelas - totalCompra) > ToleranciaParcelas)
                return (false, $"A soma das parcelas (R$ {somaParcelas:N2}) é diferente do total da compra "
                             + $"(R$ {totalCompra:N2}). Ajuste os pagamentos.", chave);

            // Entrada no estoque dos produtos, recalculando o custo medio.
            var (estoqueOk, erroEstoque) = await AjustarEstoque(novosItens, +1);
            if (!estoqueOk) return (false, erroEstoque, chave);
        }

        // ---------- 7. Persistencia do cabecalho ----------
        Compra compra;
        if (existente == null)
        {
            compra = new Compra
            {
                NrNota = chave.NrNota,
                NrSerie = chave.NrSerie,
                NrModelo = chave.NrModelo,
                IdFornecedor = chave.IdFornecedor,
                DtCriacao = DateTime.Now
            };
            _db.Compras.Add(compra);
        }
        else
        {
            compra = existente;
            _db.CompraItens.RemoveRange(existente.Itens);
            _db.CompraPagamentos.RemoveRange(existente.Pagamentos);
            compra.DtEdicao = DateTime.Now;

            // Descarrega as exclusoes ANTES de inserir as novas linhas. As PKs de
            // item e parcela sao deterministicas (nrItem/nrParcela 1..n), entao
            // apagar e inserir no mesmo SaveChanges poderia violar a PK. Continua
            // tudo dentro da mesma transacao: um erro adiante ainda faz rollback.
            await _db.SaveChangesAsync();
        }

        compra.DtEmissao = input.DtEmissao!.Value;
        compra.DtChegada = input.DtChegada;
        compra.IdTransportador = input.IdTransportador;
        compra.IdCondicaoPagamento = input.IdCondicaoPagamento;
        compra.VlFrete = input.VlFrete;
        compra.VlSeguro = input.VlSeguro;
        compra.VlOutrasDespesas = input.VlOutrasDespesas;
        compra.VlTotalProdutos = totalProdutos;
        compra.VlTotalCompra = totalCompra;
        compra.TpStatus = input.TpStatus;
        compra.DsObservacao = Module.Truncar(input.DsObservacao?.Trim(), 500);
        compra.Itens = novosItens;
        compra.Pagamentos = novasParcelas;

        // ---------- 8. Gravacao e contas a pagar, tudo na mesma transacao ----------
        try
        {
            // Compra + itens + parcelas primeiro. O titulo do Contas a Pagar
            // referencia a parcela por FK composta que o EF nao enxerga (as
            // colunas de origem sao anulaveis e nao formam navegacao), entao
            // ele nao saberia ordenar os INSERTs sozinho. Sao dois
            // SaveChanges dentro da MESMA transacao: um erro no segundo ainda
            // desfaz o primeiro no rollback.
            await _db.SaveChangesAsync();

            if (finalizar)
            {
                _db.ContasAPagar.AddRange(GerarContasAPagar(compra, novasParcelas));
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();

            // Violacao da PK composta: outra sessao gravou a mesma
            // Modelo + Serie + Numero + Fornecedor no meio do caminho.
            if (ex.InnerException?.Message.Contains("PK_tbCmpCompras") == true)
                return (false, $"Já existe uma compra com modelo {chave.NrModelo}, série {chave.NrSerie}, "
                             + $"número {chave.NrNota} para este fornecedor.", chave);

            return (false, "Não foi possível gravar a compra. Nenhuma alteração foi aplicada. "
                         + "Verifique os dados informados e tente novamente.", chave);
        }

        return (true, null, chave);
    }

    // ============================================================
    // Cancelamento e exclusao
    // ============================================================

    // Cancela a compra estornando o estoque e cancelando os titulos gerados.
    public async Task<(bool ok, string? erro)> Cancelar(CompraChaveDTO chave)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

        if (compra == null) return (false, "Compra não encontrada.");
        if (compra.TpStatus == Compra.CANCELADA) return (true, null);

        var contas = await GetContasGeradas(chave);
        if (contas.Any(c => c.DsStatus == ContaAPagar.PAGO || c.VlPago > 0))
            return (false, "Não é possível cancelar: existe título já pago/baixado no Contas a Pagar. "
                         + "Estorne o pagamento primeiro.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (compra.TpStatus == Compra.FINALIZADA)
        {
            // Devolve o estoque que a compra deu entrada.
            var (ok, erro) = await AjustarEstoque(compra.Itens, -1, validar: true);
            if (!ok) return (false, erro);

            // Titulos sao cancelados (nao excluidos) para preservar o historico.
            foreach (var conta in contas)
            {
                conta.DsStatus = ContaAPagar.CANCELADO;
                conta.FlAtivo = false;
                conta.DtEdicao = DateTime.Now;
            }
        }

        compra.TpStatus = Compra.CANCELADA;
        compra.DtEdicao = DateTime.Now;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    // Exclusao definitiva. So permitida enquanto a compra nao gerou financeiro.
    public async Task<(bool ok, string? erro)> Excluir(CompraChaveDTO chave)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Pagamentos)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

        if (compra == null) return (false, "Compra não encontrada.");

        if (compra.TpStatus == Compra.FINALIZADA)
            return (false, "Compra finalizada não pode ser excluída. Utilize o cancelamento.");

        var contas = await GetContasGeradas(chave);
        if (contas.Count > 0)
            return (false, "Não é possível excluir: existem títulos no Contas a Pagar vinculados a esta compra.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.CompraItens.RemoveRange(compra.Itens);
        _db.CompraPagamentos.RemoveRange(compra.Pagamentos);
        _db.Compras.Remove(compra);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    // ============================================================
    // Apoio
    // ============================================================

    private async Task<(bool ok, string? erro)> ValidarCabecalho(CompraDTO input)
    {
        if (input.NrModelo <= 0)
            return (false, "Informe o modelo da nota.");
        if (input.NrSerie < 0)
            return (false, "Informe a série da nota.");
        if (input.NrNota <= 0)
            return (false, "Informe o número da nota.");
        if (input.IdFornecedor <= 0)
            return (false, "Selecione o fornecedor.");
        if (input.DtEmissao == null)
            return (false, "Informe a data de emissão.");

        if (input.DtEmissao > DateOnly.FromDateTime(DateTime.Today))
            return (false, "A data de emissão não pode ser posterior à data atual.");

        if (input.DtChegada != null && input.DtChegada < input.DtEmissao)
            return (false, "A data de chegada não pode ser anterior à data de emissão.");

        // So se informa a chegada depois que a mercadoria chegou.
        if (input.DtChegada > DateOnly.FromDateTime(DateTime.Today))
            return (false, "A data de chegada não pode ser posterior à data atual.");

        if (input.VlFrete < 0 || input.VlSeguro < 0 || input.VlOutrasDespesas < 0)
            return (false, "Frete, seguro e outros gastos não podem ser negativos.");

        var fornecedor = await _db.Fornecedores.AsNoTracking()
            .Where(f => f.IdFornecedor == input.IdFornecedor)
            .Select(f => new { f.IdCondicaoPagamento })
            .FirstOrDefaultAsync();
        if (fornecedor == null)
            return (false, "Fornecedor informado não existe.");

        // Fornecedor com condicao de pagamento no cadastro: a compra usa sempre a
        // dele (a tela trava o campo; aqui vale mesmo que o POST venha diferente).
        if (fornecedor.IdCondicaoPagamento is > 0)
        {
            input.IdCondicaoPagamento = fornecedor.IdCondicaoPagamento;
            foreach (var p in input.Pagamentos)
                p.IdCondicaoPagamento = fornecedor.IdCondicaoPagamento;
        }

        if (input.IdTransportador is > 0
            && !await _db.Transportadores.AnyAsync(t => t.IdTransportador == input.IdTransportador))
            return (false, "Transportadora informada não existe.");

        if (input.IdCondicaoPagamento is > 0
            && !await _db.CondicoesPagamento.AnyAsync(c => c.IdCondicaoPagamento == input.IdCondicaoPagamento))
            return (false, "Condição de pagamento informada não existe.");

        if (input.TpStatus != Compra.RASCUNHO && input.TpStatus != Compra.FINALIZADA)
            return (false, "Situação da compra inválida.");

        return (true, null);
    }

    // Recalcula os itens no servidor. O total enviado pela tela e ignorado.
    private (bool ok, string? erro, List<CompraItem> itens, decimal total) MontarItens(
        CompraDTO input, CompraChaveDTO chave)
    {
        var itens = new List<CompraItem>();
        decimal total = 0;
        var seq = 0;

        foreach (var dto in input.Itens)
        {
            seq++;

            var produto = dto.TpItem == "PRODUTO";
            if (produto && dto.IdProduto is not > 0)
                return (false, $"Item {seq}: selecione o produto.", [], 0);
            if (!produto && dto.IdServico is not > 0)
                return (false, $"Item {seq}: selecione o serviço.", [], 0);

            if (dto.QtItem <= 0)
                return (false, $"Item {seq}: a quantidade deve ser maior que zero.", [], 0);
            if (dto.VlUnitario < 0)
                return (false, $"Item {seq}: o preço unitário não pode ser negativo.", [], 0);
            if (dto.VlDesconto < 0)
                return (false, $"Item {seq}: o desconto não pode ser negativo.", [], 0);

            // Total do item = Quantidade x Preco Unitario - Desconto.
            var bruto = Math.Round(dto.QtItem * dto.VlUnitario, 2, MidpointRounding.AwayFromZero);
            var totalItem = bruto - dto.VlDesconto;

            if (totalItem < 0)
                return (false, $"Item {seq}: o desconto (R$ {dto.VlDesconto:N2}) é maior que o valor do item "
                             + $"(R$ {bruto:N2}) e deixaria o total negativo.", [], 0);

            total += totalItem;

            itens.Add(new CompraItem
            {
                NrNota = chave.NrNota,
                NrSerie = chave.NrSerie,
                NrModelo = chave.NrModelo,
                IdFornecedor = chave.IdFornecedor,
                NrItem = seq,
                TpItem = produto ? "PRODUTO" : "SERVICO",
                IdProduto = produto ? dto.IdProduto : null,
                IdServico = produto ? null : dto.IdServico,
                IdClassificacaoConta = dto.IdClassificacaoConta,
                SgUnidade = Module.Truncar(dto.SgUnidade, 10),
                QtItem = dto.QtItem,
                VlUnitario = dto.VlUnitario,
                VlDesconto = dto.VlDesconto,
                VlTotal = totalItem
            });
        }

        return (true, null, itens, total);
    }

    // Rateia o custo adicional (frete + seguro + outros) entre os PRODUTOS,
    // proporcional ao total de cada item. O ultimo produto absorve a
    // diferenca de arredondamento, para a soma fechar com o custo adicional.
    // Mesma conta feita pela tela em calcRateio().
    private static void RatearCustoAdicional(List<CompraItem> itens, decimal custoAdicional)
    {
        var produtos = itens.Where(i => i.TpItem == "PRODUTO").ToList();
        var baseRateio = produtos.Sum(i => i.VlTotal);
        decimal acumulado = 0;

        for (var n = 0; n < produtos.Count; n++)
        {
            var item = produtos[n];

            // Sem valor para ponderar (itens zerados): divide em partes iguais.
            var proporcao = baseRateio > 0 ? item.VlTotal / baseRateio : 1m / produtos.Count;

            var rateio = n == produtos.Count - 1
                ? custoAdicional - acumulado
                : Math.Round(custoAdicional * proporcao, 2, MidpointRounding.AwayFromZero);

            acumulado += rateio;
            item.VlRateio = rateio;
        }

        foreach (var item in itens)
        {
            if (item.TpItem != "PRODUTO") item.VlRateio = 0;
            // 2 casas decimais, igual a tela: 5 arredonda para cima, abaixo de 5 para baixo.
            item.VlCustoUnitario = Math.Round((item.VlTotal + item.VlRateio) / item.QtItem, 2,
                                              MidpointRounding.AwayFromZero);
        }
    }

    private (bool ok, string? erro, List<CompraPagamento> parcelas) MontarParcelas(
        CompraDTO input, CompraChaveDTO chave)
    {
        var parcelas = new List<CompraPagamento>();
        var seq = 0;

        foreach (var dto in input.Pagamentos)
        {
            seq++;

            if (dto.IdFormaPagamento <= 0)
                return (false, $"Parcela {seq}: selecione a forma de pagamento.", []);
            if (dto.VlParcela <= 0)
                return (false, $"Parcela {seq}: o valor deve ser maior que zero.", []);
            if (dto.DtVencimento == null)
                return (false, $"Parcela {seq}: informe a data de vencimento.", []);

            parcelas.Add(new CompraPagamento
            {
                NrNota = chave.NrNota,
                NrSerie = chave.NrSerie,
                NrModelo = chave.NrModelo,
                IdFornecedor = chave.IdFornecedor,
                NrParcela = seq,
                IdCondicaoPagamento = dto.IdCondicaoPagamento is > 0 ? dto.IdCondicaoPagamento : null,
                IdFormaPagamento = dto.IdFormaPagamento,
                VlParcela = dto.VlParcela,
                DtVencimento = dto.DtVencimento.Value
            });
        }

        return (true, null, parcelas);
    }

    // Um titulo no Contas a Pagar por parcela da compra, mantendo o vinculo
    // com a compra de origem (nrNota + nrSerie + nrModelo + idFornecedor + nrParcela).
    private static List<ContaAPagar> GerarContasAPagar(Compra compra, List<CompraPagamento> parcelas)
    {
        var contas = new List<ContaAPagar>();

        foreach (var parcela in parcelas)
        {
            var descricao = $"Compra NF {compra.NrModelo}/{compra.NrSerie}/{compra.NrNota} - "
                          + $"Parcela {parcela.NrParcela}/{parcelas.Count}";

            contas.Add(new ContaAPagar
            {
                DsConta = Module.Truncar(descricao, 100)!,
                NrDocumento = Module.Truncar(compra.NrNota.ToString(), 20),
                IdFornecedor = compra.IdFornecedor,
                IdFormaPagamento = parcela.IdFormaPagamento,
                DtEmissao = compra.DtEmissao,
                DtVencimento = parcela.DtVencimento,
                VlValor = parcela.VlParcela,
                VlPago = 0,
                DsStatus = ContaAPagar.PENDENTE,
                FlAtivo = true,
                DsObservacao = Module.Truncar($"Gerado automaticamente pela compra "
                                     + $"{compra.NrModelo}/{compra.NrSerie}/{compra.NrNota}.", 500),
                DtCriacao = DateTime.Now,

                // Vinculo com a compra de origem.
                NrNota = compra.NrNota,
                NrSerie = compra.NrSerie,
                NrModelo = compra.NrModelo,
                NrParcela = parcela.NrParcela
            });
        }

        return contas;
    }

    // Ajusta o saldo dos produtos e recalcula o custo medio. sinal = +1
    // (entrada pela compra) ou -1 (estorno). validar=true impede que o estorno
    // deixe saldo negativo. Mesmo mecanismo de saldo usado por VendaBusiness,
    // com o sinal invertido.
    private async Task<(bool ok, string? erro)> AjustarEstoque(
        IEnumerable<CompraItem> itens, int sinal, bool validar = false)
    {
        foreach (var item in itens.Where(i => i.TpItem == "PRODUTO" && i.IdProduto != null))
        {
            var prod = await _db.Produtos.FindAsync(item.IdProduto);
            if (prod == null) continue;

            var atual = prod.QtSaldo ?? 0;
            var novo = atual + sinal * item.QtItem;

            if (validar && novo < 0)
                return (false, $"Não é possível estornar a entrada de '{prod.DsProduto}': o saldo ficaria negativo "
                             + $"(atual: {atual:N4}, estorno: {item.QtItem:N4}). "
                             + "Provavelmente o produto já foi vendido/consumido.");

            // O custo medio usa o saldo ANTERIOR ao movimento, por isso e
            // calculado antes de gravar o novo saldo.
            prod.VlCusto = CalcularCustoMedio(atual, prod.VlCusto ?? 0, sinal * item.QtItem, item.VlCustoUnitario)
                           ?? prod.VlCusto;
            prod.QtSaldo = novo;
        }

        return (true, null);
    }

    // Custo medio ponderado, conforme definido em aula:
    //   custoMedio = (saldo * custo + qtde * custoUnit) / (saldo + qtde)
    // custoUnit e o VlCustoUnitario do item, que ja sai com o desconto
    // (vlTotal = bruto - desconto) e com o rateio de frete/seguro/outros.
    // No estorno qtde vem negativa, o que desfaz a media daquela entrada.
    // Retorna null quando a conta nao se aplica (saldo final zerado ou custo
    // negativo); nesse caso o custo atual do produto e mantido.
    private static decimal? CalcularCustoMedio(decimal saldo, decimal custo, decimal qtde, decimal custoUnit)
    {
        // Saldo negativo nao tem custo a ponderar: entra na media como zero.
        if (saldo < 0) saldo = 0;

        var saldoFinal = saldo + qtde;
        if (saldoFinal <= 0) return null;

        var custoMedio = (saldo * custo + qtde * custoUnit) / saldoFinal;
        if (custoMedio < 0) return null;

        return Math.Round(custoMedio, 2, MidpointRounding.AwayFromZero);
    }
}
