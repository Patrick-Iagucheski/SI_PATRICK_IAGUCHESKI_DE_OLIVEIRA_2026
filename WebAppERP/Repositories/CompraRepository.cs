using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.DTOs;
using WebAppERP.Models;

namespace WebAppERP.Repositories;

// Regras de negocio da compra. Toda a gravacao definitiva acontece dentro
// de uma unica transacao: compra + itens + parcelas + contas a pagar +
// estoque. Se qualquer etapa falhar, nada e gravado (rollback).
public class CompraRepository : ICompraRepository
{
    private readonly AppDbContext _db;

    public CompraRepository(AppDbContext db) => _db = db;

    // Tolerancia de arredondamento na conferencia das parcelas (1 centavo).
    private const decimal ToleranciaParcelas = 0.01m;

    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<OpeCompra>> ListarAsync() =>
        await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Transportador)
            .OrderByDescending(c => c.DtEmissao)
            .ThenByDescending(c => c.NrNota)
            .ToListAsync();

    public async Task<OpeCompra?> ObterCompletaAsync(CompraChaveDto chave) =>
        await _db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Transportador)
            .Include(c => c.CondicaoPagamento)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .Include(c => c.Itens).ThenInclude(i => i.Servico)
            .Include(c => c.Itens).ThenInclude(i => i.ClassificacaoConta)
            .Include(c => c.Pagamentos).ThenInclude(p => p.FormaPagamento)
            .Include(c => c.Pagamentos).ThenInclude(p => p.CondicaoPagamento)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

    // Titulos que esta compra gerou no Contas a Pagar.
    public async Task<List<FinContaPagar>> ObterContasGeradasAsync(CompraChaveDto chave) =>
        await _db.ContasAPagar
            .Include(c => c.FormaPagamento)
            .Where(c => c.NrNota == chave.NrNota
                     && c.NrSerie == chave.NrSerie
                     && c.NrModelo == chave.NrModelo
                     && c.IdFornecedor == chave.IdFornecedor)
            .OrderBy(c => c.NrParcela)
            .ToListAsync();

    public async Task<CompraCombosDto> ObterCombosAsync() => new()
    {
        Fornecedores = await _db.Fornecedores.Where(f => f.FlAtivo).OrderBy(f => f.DsRazaoSocial).ToListAsync(),
        Transportadores = await _db.Transportadores.OrderBy(t => t.DsRazaoSocial).ToListAsync(),
        Produtos = await _db.Produtos.Where(p => p.FlAtivo).OrderBy(p => p.DsProduto).ToListAsync(),
        Servicos = await _db.Servicos.Where(s => s.FlAtivo).OrderBy(s => s.DsServico).ToListAsync(),
        Classificacoes = await _db.ClassificacoesConta.Where(c => c.FlAtivo).OrderBy(c => c.DsClassificacaoConta).ToListAsync(),
        Condicoes = await _db.CondicoesPagamento.Where(c => c.FlAtivo).OrderBy(c => c.DsCondicao).ToListAsync(),
        Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync()
    };

    // ============================================================
    // Gravacao
    // ============================================================

    public async Task<(bool ok, string? erro, CompraChaveDto chave)> SalvarAsync(CompraInputDto input)
    {
        var chave = input.Chave();
        var finalizar = input.TpStatus == "FINALIZADA";

        // ---------- 1. Validacao do cabecalho ----------
        var (cabecalhoOk, erroCabecalho) = await ValidarCabecalhoAsync(input);
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

        if (existente is { TpStatus: "CANCELADA" })
            return (false, "Compra cancelada não pode ser editada.", chave);

        // ---------- 3. Estorno do que a versao anterior gerou ----------
        var contasAnteriores = await ObterContasGeradasAsync(chave);

        if (contasAnteriores.Any(c => c.DsStatus == "PAGO" || c.VlPago > 0))
            return (false, "Esta compra possui título já pago/baixado no Contas a Pagar. "
                         + "Estorne o pagamento antes de alterar a compra.", chave);

        if (existente is { TpStatus: "FINALIZADA" })
        {
            // Devolve o estoque que a versao anterior deu entrada.
            var (estornoOk, erroEstorno) = await AjustarEstoqueAsync(existente.Itens, -1, validar: true);
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

            // Entrada no estoque dos produtos.
            var (estoqueOk, erroEstoque) = await AjustarEstoqueAsync(novosItens, +1);
            if (!estoqueOk) return (false, erroEstoque, chave);
        }

        // ---------- 7. Persistencia do cabecalho ----------
        OpeCompra compra;
        if (existente == null)
        {
            compra = new OpeCompra
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
        compra.DsObservacao = Truncar(input.DsObservacao?.Trim(), 500);
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
            if (ex.InnerException?.Message.Contains("PK_tbOpeCompras") == true)
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
    public async Task<(bool ok, string? erro)> CancelarAsync(CompraChaveDto chave)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

        if (compra == null) return (false, "Compra não encontrada.");
        if (compra.TpStatus == "CANCELADA") return (true, null);

        var contas = await ObterContasGeradasAsync(chave);
        if (contas.Any(c => c.DsStatus == "PAGO" || c.VlPago > 0))
            return (false, "Não é possível cancelar: existe título já pago/baixado no Contas a Pagar. "
                         + "Estorne o pagamento primeiro.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (compra.TpStatus == "FINALIZADA")
        {
            // Devolve o estoque que a compra deu entrada.
            var (ok, erro) = await AjustarEstoqueAsync(compra.Itens, -1, validar: true);
            if (!ok) return (false, erro);

            // Titulos sao cancelados (nao excluidos) para preservar o historico.
            foreach (var conta in contas)
            {
                conta.DsStatus = "CANCELADO";
                conta.FlAtivo = false;
                conta.DtEdicao = DateTime.Now;
            }
        }

        compra.TpStatus = "CANCELADA";
        compra.DtEdicao = DateTime.Now;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    // Exclusao definitiva. So permitida enquanto a compra nao gerou financeiro.
    public async Task<(bool ok, string? erro)> ExcluirAsync(CompraChaveDto chave)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Pagamentos)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor);

        if (compra == null) return (false, "Compra não encontrada.");

        if (compra.TpStatus == "FINALIZADA")
            return (false, "Compra finalizada não pode ser excluída. Utilize o cancelamento.");

        var contas = await ObterContasGeradasAsync(chave);
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

    private async Task<(bool ok, string? erro)> ValidarCabecalhoAsync(CompraInputDto input)
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

        if (input.DtChegada != null && input.DtChegada < input.DtEmissao)
            return (false, "A data de chegada não pode ser anterior à data de emissão.");

        if (input.VlFrete < 0 || input.VlSeguro < 0 || input.VlOutrasDespesas < 0)
            return (false, "Frete, seguro e outros gastos não podem ser negativos.");

        if (!await _db.Fornecedores.AnyAsync(f => f.IdFornecedor == input.IdFornecedor))
            return (false, "Fornecedor informado não existe.");

        if (input.IdTransportador is > 0
            && !await _db.Transportadores.AnyAsync(t => t.IdTransportador == input.IdTransportador))
            return (false, "Transportadora informada não existe.");

        if (input.IdCondicaoPagamento is > 0
            && !await _db.CondicoesPagamento.AnyAsync(c => c.IdCondicaoPagamento == input.IdCondicaoPagamento))
            return (false, "Condição de pagamento informada não existe.");

        if (input.TpStatus != "RASCUNHO" && input.TpStatus != "FINALIZADA")
            return (false, "Situação da compra inválida.");

        return (true, null);
    }

    // Recalcula os itens no servidor. O total enviado pela tela e ignorado.
    private (bool ok, string? erro, List<OpeCompraItem> itens, decimal total) MontarItens(
        CompraInputDto input, CompraChaveDto chave)
    {
        var itens = new List<OpeCompraItem>();
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

            itens.Add(new OpeCompraItem
            {
                NrNota = chave.NrNota,
                NrSerie = chave.NrSerie,
                NrModelo = chave.NrModelo,
                IdFornecedor = chave.IdFornecedor,
                NrItem = seq,
                TpItem = produto ? "PRODUTO" : "SERVICO",
                IdProduto = produto ? dto.IdProduto : null,
                IdServico = produto ? null : dto.IdServico,
                IdClassificacaoConta = dto.IdClassificacaoConta is > 0 ? dto.IdClassificacaoConta : null,
                SgUnidade = Truncar(dto.SgUnidade, 10),
                QtItem = dto.QtItem,
                VlUnitario = dto.VlUnitario,
                VlDesconto = dto.VlDesconto,
                VlTotal = totalItem
            });
        }

        return (true, null, itens, total);
    }

    private (bool ok, string? erro, List<OpeCompraPagamento> parcelas) MontarParcelas(
        CompraInputDto input, CompraChaveDto chave)
    {
        var parcelas = new List<OpeCompraPagamento>();
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

            parcelas.Add(new OpeCompraPagamento
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
    private static List<FinContaPagar> GerarContasAPagar(OpeCompra compra, List<OpeCompraPagamento> parcelas)
    {
        var contas = new List<FinContaPagar>();

        foreach (var parcela in parcelas)
        {
            var descricao = $"Compra NF {compra.NrModelo}/{compra.NrSerie}/{compra.NrNota} - "
                          + $"Parcela {parcela.NrParcela}/{parcelas.Count}";

            contas.Add(new FinContaPagar
            {
                DsConta = Truncar(descricao, 100)!,
                NrDocumento = Truncar(compra.NrNota.ToString(), 20),
                IdFornecedor = compra.IdFornecedor,
                IdFormaPagamento = parcela.IdFormaPagamento,
                DtEmissao = compra.DtEmissao,
                DtVencimento = parcela.DtVencimento,
                VlValor = parcela.VlParcela,
                VlPago = 0,
                DsStatus = "PENDENTE",
                FlAtivo = true,
                DsObservacao = Truncar($"Gerado automaticamente pela compra "
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

    // Ajusta o saldo dos produtos. sinal = +1 (entrada pela compra) ou
    // -1 (estorno). validar=true impede que o estorno deixe saldo negativo.
    // Mesmo mecanismo usado por VendaRepository, com o sinal invertido.
    private async Task<(bool ok, string? erro)> AjustarEstoqueAsync(
        IEnumerable<OpeCompraItem> itens, int sinal, bool validar = false)
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

            prod.QtSaldo = novo;
        }

        return (true, null);
    }
}
