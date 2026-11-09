using Microsoft.EntityFrameworkCore;
using WebAppERP.Business.Financeiro;
using WebAppERP.Data;
using WebAppERP.DTOs.Compras;

namespace WebAppERP.Business.Compras;

// Regras de negocio da compra. Toda a gravacao definitiva acontece dentro
// de uma unica transacao: compra + itens + parcelas + contas a pagar +
// estoque. Se qualquer etapa falhar, nada e gravado (rollback).
// Regra nao atendida lanca ValidacaoException; a tela mostra a mensagem.
public class CompraBusiness
{
    private readonly AppDbContext _db;
    private readonly CondicaoPagamentoBusiness _condicao;
    private readonly FormaPagamentoBusiness _forma;

    public CompraBusiness(AppDbContext db, CondicaoPagamentoBusiness condicao, FormaPagamentoBusiness forma)
    {
        _db = db;
        _condicao = condicao;
        _forma = forma;
    }

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
    // Devolve a razao social do fornecedor.
    public async Task<string> ValidarChaveNova(CompraChaveDTO chave)
    {
        if (chave.NrModelo <= 0)
            throw new ValidacaoException("Informe o modelo da nota.");
        ValidarNumeroSerie(chave.NrNota, chave.NrSerie);
        if (chave.IdFornecedor <= 0)
            throw new ValidacaoException("Informe o fornecedor.");

        var fornecedor = await _db.Fornecedores
            .Where(f => f.IdFornecedor == chave.IdFornecedor)
            .Select(f => new { f.DsRazaoSocial, f.FlAtivo })
            .FirstOrDefaultAsync();

        if (fornecedor == null)
            throw new ValidacaoException("Fornecedor informado não existe.");
        if (!fornecedor.FlAtivo)
            throw new ValidacaoException("Fornecedor informado está inativo.");

        var existente = await GetSituacao(chave);
        if (existente != null)
            throw new ValidacaoException($"Já existe uma compra ({existente}) com modelo {chave.NrModelo}, série {chave.NrSerie}, "
                                       + $"número {chave.NrNota} para este fornecedor.");

        return fornecedor.DsRazaoSocial ?? string.Empty;
    }

    // Condicoes e formas de pagamento ativas para os combos do bloco Pagamento.
    public async Task<CompraCombosDTO> GetCombos() => new()
    {
        Condicoes = await _condicao.Listar(somenteAtivas: true),
        Formas = await _forma.Listar(somenteAtivas: true)
    };

    // ============================================================
    // Gravacao
    // ============================================================

    // Grava a compra e devolve a chave gravada. TpStatus RASCUNHO nao gera
    // Contas a Pagar nem estoque; FINALIZADA grava tudo de forma atomica
    // (compra + itens + parcelas + contas a pagar + estoque).
    public async Task<CompraChaveDTO> Salvar(CompraDTO input)
    {
        var chave = input.GetChave();
        var finalizar = input.TpStatus == Compra.FINALIZADA;

        // ---------- 1. Validacao do cabecalho ----------
        await ValidarCabecalho(input);

        // A chave identifica a compra: alterar qualquer parte dela seria
        // criar outra compra. Exige cancelar e lancar uma nova.
        if (input.ChaveOriginal is { Preenchida: true })
        {
            var orig = input.ChaveOriginal;
            if (orig.NrNota != chave.NrNota || orig.NrSerie != chave.NrSerie
                || orig.NrModelo != chave.NrModelo || orig.IdFornecedor != chave.IdFornecedor)
                throw new ValidacaoException("Modelo, série, número e fornecedor identificam a compra e não podem ser alterados.",
                                             "Cancele esta compra e lance uma nova.");
        }

        // Qualquer ValidacaoException daqui em diante descarta a transacao
        // (rollback no dispose): nada do que foi ajustado e gravado.
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
            throw new ValidacaoException("Esta compra foi excluída em outra tela.", "Recarregue a tela.");

        // Mesma chave, mas outra compra: a que a tela abriu foi excluida e a
        // nota foi lancada de novo em outra tela. A tela antiga nao grava por cima.
        if (existente != null && input.VersaoOriginal != 0 && existente.DtCriacao.Ticks != input.VersaoOriginal)
            throw new ValidacaoException("Esta compra foi excluída e lançada de novo em outra tela.",
                                         "Recarregue a tela para abrir a compra atual.");

        if (existente != null && input.ChaveOriginal is not { Preenchida: true })
            throw new ValidacaoException($"Já existe uma compra com modelo {chave.NrModelo}, série {chave.NrSerie}, "
                                       + $"número {chave.NrNota} para este fornecedor.");

        // ---------- 3. So rascunho pode ser alterado ----------
        // Finalizada ou cancelada fica so para consulta. Para corrigir uma
        // finalizada, cancela-se (estorna estoque e titulos) e lanca-se de novo.
        if (existente is { TpStatus: Compra.CANCELADA })
            throw new ValidacaoException("Compra cancelada não pode ser alterada.");
        if (existente is { TpStatus: Compra.FINALIZADA })
            throw new ValidacaoException("Compra finalizada não pode ser alterada.",
                                         "Para corrigir, cancele a compra e lance uma nova.");

        // ---------- 4. Itens (recalculados no servidor) ----------
        var (novosItens, totalProdutos) = MontarItens(input, chave);

        var custoAdicional = input.VlFrete + input.VlSeguro + input.VlOutrasDespesas;
        // Total da compra (valor da nota, regra da NF-e: produtos - desconto +
        // frete + seguro + outros) = total dos produtos ja com o desconto +
        // custo adicional. Ex.: 279,72 + 71,51 + 13,11 = 364,34. Bate com o que
        // entra no estoque (soma de quantidade x custo unitario).
        var totalCompra = totalProdutos + custoAdicional;
        RatearCustoAdicional(novosItens, custoAdicional);

        // ---------- 5. Parcelas ----------
        var novasParcelas = MontarParcelas(input, chave);

        // ---------- 6. Regras exclusivas da finalizacao ----------
        if (finalizar)
        {
            if (novosItens.Count == 0)
                throw new ValidacaoException("Adicione ao menos um produto/serviço para finalizar a compra.");

            if (novasParcelas.Count == 0)
                throw new ValidacaoException("Informe ao menos uma parcela de pagamento para finalizar a compra.");

            var somaParcelas = novasParcelas.Sum(p => p.VlParcela);
            if (Math.Abs(somaParcelas - totalCompra) > ToleranciaParcelas)
                throw new ValidacaoException($"A soma das parcelas (R$ {somaParcelas:N2}) é diferente do total da compra "
                                           + $"(R$ {totalCompra:N2}).", "Ajuste os pagamentos.");

            // Entrada no estoque dos produtos, recalculando o custo medio.
            await AjustarEstoque(novosItens, +1);
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
            };
            _db.Compras.Add(compra);
        }
        else
        {
            compra = existente;
            _db.CompraItens.RemoveRange(existente.Itens);
            _db.CompraPagamentos.RemoveRange(existente.Pagamentos);

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
                throw new ValidacaoException($"Já existe uma compra com modelo {chave.NrModelo}, série {chave.NrSerie}, "
                                           + $"número {chave.NrNota} para este fornecedor.");

            throw new ValidacaoException("Não foi possível gravar a compra. Nenhuma alteração foi aplicada.",
                                         "Verifique os dados informados e tente novamente.");
        }

        return chave;
    }

    // ============================================================
    // Cancelamento e exclusao
    // ============================================================

    // Cancela a compra estornando o estoque e cancelando os titulos gerados, e
    // libera o numero da nota: a cancelada recebe zeros na frente (99001 ->
    // 099001) e o numero certo fica livre para lancar a nota de novo. Devolve a
    // mensagem para a tela.
    public async Task<string> Cancelar(CompraChaveDTO chave)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor)
            ?? throw new ValidacaoException("Compra não encontrada.");

        if (compra.TpStatus == Compra.CANCELADA)
            throw new ValidacaoException("Esta compra já está cancelada.");
        // Rascunho nao deu entrada no estoque nem gerou titulos: nao ha o que
        // cancelar. Sai pelo Excluir.
        if (compra.TpStatus == Compra.RASCUNHO)
            throw new ValidacaoException("Rascunho não pode ser cancelado.", "Para desistir do rascunho, use Excluir.");

        var contas = await GetContasGeradas(chave);
        if (contas.Any(c => c.DsStatus == ContaAPagar.PAGO || c.VlPago > 0))
            throw new ValidacaoException("Não é possível cancelar: existe título já pago/baixado no Contas a Pagar.",
                                         "Estorne o pagamento primeiro.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (compra.TpStatus == Compra.FINALIZADA)
        {
            // Devolve o estoque que a compra deu entrada.
            await AjustarEstoque(compra.Itens, -1, validar: true);

            // Titulos sao cancelados (nao excluidos) para preservar o historico.
            foreach (var conta in contas)
            {
                conta.DsStatus = ContaAPagar.CANCELADO;
            }
        }

        compra.TpStatus = Compra.CANCELADA;

        await _db.SaveChangesAsync();

        var novoNumero = await NumeroParaCancelada(chave);
        if (novoNumero != null)
            await TrocarNumeroDaCancelada(chave, novoNumero);

        await tx.CommitAsync();

        const string mensagem = "Compra cancelada. Estoque estornado e títulos cancelados.";
        return novoNumero != null
            ? $"{mensagem} O número {chave.NrNota} ficou livre: a compra cancelada passou a ser a Nº {novoNumero}."
            : $"{mensagem} O número {chave.NrNota} não pôde ser liberado: com mais um zero passaria de 9 dígitos.";
    }

    // Numero para a compra cancelada: o mesmo com zeros na frente, o primeiro
    // que ainda nao existe, ate 9 digitos. null se nao couber.
    private async Task<string?> NumeroParaCancelada(CompraChaveDTO chave)
    {
        for (var numero = "0" + chave.NrNota; numero.Length <= 9; numero = "0" + numero)
        {
            var tentativa = numero;
            if (!await _db.Compras.AnyAsync(c => c.NrNota == tentativa && c.NrSerie == chave.NrSerie
                                              && c.NrModelo == chave.NrModelo && c.IdFornecedor == chave.IdFornecedor))
                return tentativa;
        }
        return null;
    }

    // Troca o numero da compra cancelada. Itens, parcelas e titulos acompanham
    // pelas FKs com ON UPDATE CASCADE
    // (Database/Migracoes/1794071000000_Alter_Table_Compras_Cancelada_Libera_Numero.sql);
    // o texto dos titulos ("Compra NF 55/488/99001 ...") tambem passa ao numero novo.
    private async Task TrocarNumeroDaCancelada(CompraChaveDTO chave, string novoNumero)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.tbCmpCompras SET nrNota = {novoNumero}
            WHERE nrNota = {chave.NrNota} AND nrSerie = {chave.NrSerie}
              AND nrModelo = {chave.NrModelo} AND idFornecedor = {chave.IdFornecedor}");

        var antigo = $"{chave.NrModelo}/{chave.NrSerie}/{chave.NrNota}";
        var novo = $"{chave.NrModelo}/{chave.NrSerie}/{novoNumero}";
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.tbFinContasAPagar
            SET dsConta = REPLACE(dsConta, {"NF " + antigo + " "}, {"NF " + novo + " "}),
                dsObservacao = REPLACE(dsObservacao, {"COMPRA " + antigo + "."}, {"COMPRA " + novo + "."})
            WHERE nrNota = {novoNumero} AND nrSerie = {chave.NrSerie}
              AND nrModelo = {chave.NrModelo} AND idFornecedor = {chave.IdFornecedor}");
    }

    // Exclusao definitiva. So permitida enquanto a compra nao gerou financeiro.
    public async Task Excluir(CompraChaveDTO chave)
    {
        var compra = await _db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Pagamentos)
            .FirstOrDefaultAsync(c => c.NrNota == chave.NrNota
                                   && c.NrSerie == chave.NrSerie
                                   && c.NrModelo == chave.NrModelo
                                   && c.IdFornecedor == chave.IdFornecedor)
            ?? throw new ValidacaoException("Compra não encontrada.");

        if (compra.TpStatus == Compra.FINALIZADA)
            throw new ValidacaoException("Compra finalizada não pode ser excluída.", "Utilize o cancelamento.");

        var contas = await GetContasGeradas(chave);
        if (contas.Count > 0)
            throw new ValidacaoException("Não é possível excluir: existem títulos no Contas a Pagar vinculados a esta compra.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.CompraItens.RemoveRange(compra.Itens);
        _db.CompraPagamentos.RemoveRange(compra.Pagamentos);
        _db.Compras.Remove(compra);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ============================================================
    // Apoio
    // ============================================================

    private async Task ValidarCabecalho(CompraDTO input)
    {
        if (input.NrModelo <= 0)
            throw new ValidacaoException("Informe o modelo da nota.");
        ValidarNumeroSerie(input.NrNota, input.NrSerie);
        if (input.IdFornecedor <= 0)
            throw new ValidacaoException("Selecione o fornecedor.");
        if (input.DtEmissao == null)
            throw new ValidacaoException("Informe a data de emissão.");

        if (input.DtEmissao > DateOnly.FromDateTime(DateTime.Today))
            throw new ValidacaoException("A data de emissão não pode ser posterior à data atual.");

        if (input.DtChegada != null && input.DtChegada < input.DtEmissao)
            throw new ValidacaoException("A data de chegada não pode ser anterior à data de emissão.");

        // So se informa a chegada depois que a mercadoria chegou.
        if (input.DtChegada > DateOnly.FromDateTime(DateTime.Today))
            throw new ValidacaoException("A data de chegada não pode ser posterior à data atual.");

        if (input.VlFrete < 0 || input.VlSeguro < 0 || input.VlOutrasDespesas < 0)
            throw new ValidacaoException("Frete, seguro e outros gastos não podem ser negativos.");

        var fornecedor = await _db.Fornecedores.AsNoTracking()
            .Where(f => f.IdFornecedor == input.IdFornecedor)
            .Select(f => new { f.IdCondicaoPagamento })
            .FirstOrDefaultAsync()
            ?? throw new ValidacaoException("Fornecedor informado não existe.");

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
            throw new ValidacaoException("Transportadora informada não existe.");

        if (input.IdCondicaoPagamento is > 0
            && !await _db.CondicoesPagamento.AnyAsync(c => c.IdCondicaoPagamento == input.IdCondicaoPagamento))
            throw new ValidacaoException("Condição de pagamento informada não existe.");

        if (input.TpStatus != Compra.RASCUNHO && input.TpStatus != Compra.FINALIZADA)
            throw new ValidacaoException("Situação da compra inválida.");
    }

    // Numero da nota: 1 a 9 digitos (nao pode ser so zeros). Serie: 1 a 3 digitos
    // (pode ser 0). Chegam so com digitos e com os zeros a esquerda digitados
    // (CompraChaveDTO.Normalizar): o zero a esquerda faz parte da chave.
    private static void ValidarNumeroSerie(string nrNota, string nrSerie)
    {
        if (nrSerie.Length == 0)
            throw new ValidacaoException("Informe a série da nota.");
        if (nrSerie.Length > 3)
            throw new ValidacaoException("A série da nota deve ter no máximo 3 dígitos.");
        if (nrNota.Length == 0 || nrNota.All(c => c == '0'))
            throw new ValidacaoException("Informe o número da nota.");
        if (nrNota.Length > 9)
            throw new ValidacaoException("O número da nota deve ter no máximo 9 dígitos.");
    }

    // Total do item da compra. O desconto e POR UNIDADE (6 casas):
    //   total = quantidade x (preco unitario - desconto unitario), em 2 casas.
    // Ex.: 12 x (4,97 - 0,06) = 12 x 4,91 = 58,92.
    // A validacao comum com a venda fica em Module.ValidarItem.
    private static decimal CalcularTotalItem(int seq, int? idProduto, decimal quantidade, decimal vlUnitario, decimal vlDesconto)
    {
        Module.ValidarItem(seq, produto: true, idProduto, idServico: null, quantidade, vlUnitario, vlDesconto);
        if (vlDesconto != Math.Round(vlDesconto, 6))
            throw new ValidacaoException($"Item {seq}: o desconto aceita no máximo 6 casas decimais.");
        if (vlDesconto > vlUnitario)
            throw new ValidacaoException($"Item {seq}: o desconto por unidade (R$ {vlDesconto:N6}) é maior que o preço "
                                       + $"unitário (R$ {vlUnitario:N2}).");

        return Math.Round(quantidade * (vlUnitario - vlDesconto), 2, MidpointRounding.AwayFromZero);
    }

    // Recalcula os itens no servidor. O total enviado pela tela e ignorado.
    private static (List<CompraItem> itens, decimal total) MontarItens(CompraDTO input, CompraChaveDTO chave)
    {
        var itens = new List<CompraItem>();
        decimal total = 0;
        var seq = 0;

        foreach (var dto in input.Itens)
        {
            seq++;

            // A compra so tem produtos (entrada de estoque).
            if (dto.TpItem != CompraItem.PRODUTO)
                throw new ValidacaoException($"Item {seq}: a compra aceita apenas produtos.");
            // Quantidade com no maximo 2 casas decimais.
            if (dto.QtItem != Math.Round(dto.QtItem, 2))
                throw new ValidacaoException($"Item {seq}: a quantidade aceita no máximo 2 casas decimais.");

            var totalItem = CalcularTotalItem(seq, dto.IdProduto, dto.QtItem, dto.VlUnitario, dto.VlDesconto);
            total += totalItem;

            itens.Add(new CompraItem
            {
                NrNota = chave.NrNota,
                NrSerie = chave.NrSerie,
                NrModelo = chave.NrModelo,
                IdFornecedor = chave.IdFornecedor,
                NrItem = seq,
                TpItem = CompraItem.PRODUTO,
                IdProduto = dto.IdProduto,
                IdClassificacaoConta = dto.IdClassificacaoConta,
                SgUnidade = Module.Truncar(dto.SgUnidade, 10),
                QtItem = dto.QtItem,
                VlUnitario = dto.VlUnitario,
                VlDesconto = dto.VlDesconto,
                VlTotal = totalItem
            });
        }

        return (itens, total);
    }

    // Rateia o custo adicional (frete + seguro + outros) entre os itens (a
    // compra so tem produtos), proporcional ao total de cada item. O ultimo produto absorve a
    // diferenca de arredondamento, para a soma fechar com o custo adicional.
    // Rateio com 6 casas decimais.
    // Mesma conta feita pela tela em calcRateio().
    private static void RatearCustoAdicional(List<CompraItem> itens, decimal custoAdicional)
    {
        var baseRateio = itens.Sum(i => i.VlTotal);
        decimal acumulado = 0;

        for (var n = 0; n < itens.Count; n++)
        {
            var item = itens[n];

            // Sem valor para ponderar (itens zerados): divide em partes iguais.
            var proporcao = baseRateio > 0 ? item.VlTotal / baseRateio : 1m / itens.Count;

            var rateio = n == itens.Count - 1
                ? custoAdicional - acumulado
                : Math.Round(custoAdicional * proporcao, 6, MidpointRounding.AwayFromZero);

            acumulado += rateio;
            item.VlRateio = rateio;
        }

        foreach (var item in itens)
        {
            // Custo unitario = preco - desconto por unidade + rateio por unidade,
            // com 6 casas decimais, igual a tela (5 arredonda para cima).
            // Ex.: 4,97 - 0,06 + 0 = 4,910000.
            item.VlCustoUnitario = Math.Round(item.VlUnitario - item.VlDesconto + item.VlRateio / item.QtItem, 6,
                                              MidpointRounding.AwayFromZero);
        }
    }

    private static List<CompraPagamento> MontarParcelas(CompraDTO input, CompraChaveDTO chave)
    {
        var parcelas = new List<CompraPagamento>();
        var seq = 0;

        foreach (var dto in input.Pagamentos)
        {
            seq++;

            if (dto.IdFormaPagamento <= 0)
                throw new ValidacaoException($"Parcela {seq}: selecione a forma de pagamento.");
            if (dto.VlParcela <= 0)
                throw new ValidacaoException($"Parcela {seq}: o valor deve ser maior que zero.");
            if (dto.DtVencimento == null)
                throw new ValidacaoException($"Parcela {seq}: informe a data de vencimento.");

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

        return parcelas;
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
                DsObservacao = Module.Truncar($"Gerado automaticamente pela compra "
                                     + $"{compra.NrModelo}/{compra.NrSerie}/{compra.NrNota}.", 500),

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
    private async Task AjustarEstoque(IEnumerable<CompraItem> itens, int sinal, bool validar = false)
    {
        foreach (var item in itens.Where(i => i.TpItem == CompraItem.PRODUTO && i.IdProduto != null))
        {
            var prod = await _db.Produtos.FindAsync(item.IdProduto);
            if (prod == null) continue;

            var atual = prod.QtSaldo ?? 0;
            var novo = atual + sinal * item.QtItem;

            if (validar && novo < 0)
                throw new ValidacaoException($"Não é possível estornar a entrada de '{prod.DsProduto}': o saldo ficaria negativo "
                                           + $"(atual: {atual:N4}, estorno: {item.QtItem:N4}).",
                                             "Provavelmente o produto já foi vendido/consumido.");

            // O custo medio usa o saldo ANTERIOR ao movimento, por isso e
            // calculado antes de gravar o novo saldo.
            prod.VlCusto = CalcularCustoMedio(atual, prod.VlCusto ?? 0, sinal * item.QtItem, item.VlCustoUnitario)
                           ?? prod.VlCusto;
            prod.QtSaldo = novo;
        }
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

        // 6 casas, igual ao custo unitario do item (VlCustoUnitario).
        return Math.Round(custoMedio, 6, MidpointRounding.AwayFromZero);
    }
}
