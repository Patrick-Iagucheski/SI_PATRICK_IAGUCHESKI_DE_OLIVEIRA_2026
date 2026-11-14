using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.DTOs.Compras;
using WebAppERP.Business.Compras;
using WebAppERP.Business.Estoque;
using WebAppERP.Business.Financeiro;
using WebAppERP.Business.Gerais;
using WebAppERP.ViewModels;

namespace WebAppERP.Pages.Compras;

public class CompraModel : PaginaBase
{
    private readonly CompraBusiness _business;
    private readonly FornecedorBusiness _fornecedor;
    private readonly CondicaoPagamentoBusiness _condicao;
    private readonly TransportadorBusiness _transportador;

    public CompraModel(CompraBusiness business, FornecedorBusiness fornecedor, CondicaoPagamentoBusiness condicao,
        TransportadorBusiness transportador)
    {
        _business = business;
        _fornecedor = fornecedor;
        _condicao = condicao;
        _transportador = transportador;
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public CompraEditorViewModel Vm { get; set; } = new();
    public string? Erro { get; set; }

    // Chave ja validada: na edicao sempre; numa inclusao, depois que a tela
    // confirmou que a chave esta livre. Enquanto for false, so a chave e editavel.
    public bool ChaveValidada { get; set; }

    // Condicao de pagamento do cadastro do fornecedor: quando existe, a tela
    // trava o campo Condicao de Pagamento com ela.
    public int? CondicaoFornecedor { get; set; }

    // ===== Cabecalho: chave composta =====
    [BindProperty] public string NrNota { get; set; } = string.Empty;
    [BindProperty] public string NrSerie { get; set; } = string.Empty;
    [BindProperty] public int NrModelo { get; set; } = 55;
    [BindProperty] public int IdFornecedor { get; set; }

    // Chave com que a compra foi carregada (vazia numa inclusao). Serve para
    // o repositorio detectar tentativa de alterar a propria chave.
    [BindProperty] public string OrigNrNota { get; set; } = string.Empty;
    [BindProperty] public string OrigNrSerie { get; set; } = string.Empty;
    [BindProperty] public int OrigNrModelo { get; set; }
    [BindProperty] public int OrigIdFornecedor { get; set; }

    // Versao da compra carregada (DtCriacao em ticks): ver CompraDTO.VersaoOriginal.
    [BindProperty] public long OrigVersao { get; set; }

    // ===== Cabecalho: demais campos =====
    [BindProperty] public DateOnly? DtEmissao { get; set; }
    [BindProperty] public DateOnly? DtChegada { get; set; }
    [BindProperty] public int? IdTransportador { get; set; }
    [BindProperty] public int? IdCondicaoPagamento { get; set; }
    [BindProperty] public decimal VlFrete { get; set; }
    [BindProperty] public decimal VlSeguro { get; set; }
    [BindProperty] public decimal VlOutrasDespesas { get; set; }
    [BindProperty] public string TpStatus { get; set; } = Compra.RASCUNHO;
    [BindProperty] public string? DsObservacao { get; set; }

    // Itens e parcelas trafegam serializados em JSON, como na tela de Venda.
    [BindProperty] public string ItensJson { get; set; } = "[]";
    [BindProperty] public string PagamentosJson { get; set; } = "[]";

    // ============================================================
    // Carregamento da tela
    // ============================================================

    public async Task<IActionResult> OnGet(string? nrNota, string? nrSerie, int? nrModelo, int? idFornecedor)
    {
        Vm.Combos = await _business.GetCombos();

        var chaveInformada = !string.IsNullOrWhiteSpace(nrNota) && nrModelo is > 0 && idFornecedor is > 0;
        if (!chaveInformada)
        {
            // Nova compra: valores iniciais da tela.
            DtEmissao = DateOnly.FromDateTime(DateTime.Today);
            Vm.Compra = new Compra { NrModelo = 55, DtEmissao = DtEmissao.Value };
            NrModelo = 55;
            return Page();
        }

        var chave = new CompraChaveDTO
        {
            NrNota = nrNota!,
            NrSerie = nrSerie ?? string.Empty,
            NrModelo = nrModelo!.Value,
            IdFornecedor = idFornecedor!.Value
        };

        var compra = await _business.GetCompraCompleta(chave);
        if (compra == null)
        {
            TempData["Erro"] = "Compra não encontrada.";
            return RedirectToPage("Compras");
        }

        Vm.Compra = compra;
        Vm.Itens = compra.Itens.OrderBy(i => i.NrItem).ToList();
        Vm.Pagamentos = compra.Pagamentos.OrderBy(p => p.NrParcela).ToList();
        Vm.ContasGeradas = await _business.GetContasGeradas(chave);
        Vm.Edicao = true;
        ChaveValidada = true;

        // Preenche o cabecalho para os asp-for exibirem os valores atuais.
        NrNota = OrigNrNota = compra.NrNota;
        NrSerie = OrigNrSerie = compra.NrSerie;
        NrModelo = OrigNrModelo = compra.NrModelo;
        IdFornecedor = OrigIdFornecedor = compra.IdFornecedor;
        OrigVersao = compra.DtCriacao.Ticks;
        DtEmissao = compra.DtEmissao;
        DtChegada = compra.DtChegada;
        IdTransportador = compra.IdTransportador;
        IdCondicaoPagamento = compra.IdCondicaoPagamento;
        CondicaoFornecedor = await GetCondicaoFornecedor(compra.IdFornecedor);
        VlFrete = compra.VlFrete;
        VlSeguro = compra.VlSeguro;
        VlOutrasDespesas = compra.VlOutrasDespesas;
        TpStatus = compra.TpStatus;
        DsObservacao = compra.DsObservacao;

        return Page();
    }

    // ============================================================
    // Gravacao (Salvar Rascunho / Salvar Compra)
    // ============================================================

    // Dados da tela (campos do form + itens e parcelas em JSON) para o repositorio.
    private CompraDTO MontarDados() => new()
    {
        NrNota = NrNota,
        NrSerie = NrSerie,
        NrModelo = NrModelo,
        IdFornecedor = IdFornecedor,
        ChaveOriginal = new CompraChaveDTO
        {
            NrNota = OrigNrNota,
            NrSerie = OrigNrSerie,
            NrModelo = OrigNrModelo,
            IdFornecedor = OrigIdFornecedor
        },
        VersaoOriginal = OrigVersao,
        DtEmissao = DtEmissao,
        DtChegada = DtChegada,
        IdTransportador = IdTransportador,
        IdCondicaoPagamento = IdCondicaoPagamento,
        VlFrete = VlFrete,
        VlSeguro = VlSeguro,
        VlOutrasDespesas = VlOutrasDespesas,
        TpStatus = TpStatus,
        DsObservacao = DsObservacao,
        Itens = JsonSerializer.Deserialize<List<CompraItemDTO>>(ItensJson ?? "[]", JsonOpts) ?? [],
        Pagamentos = JsonSerializer.Deserialize<List<CompraPagamentoDTO>>(PagamentosJson ?? "[]", JsonOpts) ?? []
    };

    public async Task<IActionResult> OnPost()
    {
        var input = MontarDados();
        try
        {
            var chave = await _business.Salvar(input);

            TempData["Sucesso"] = TpStatus == Compra.FINALIZADA
                ? "Compra finalizada. Estoque atualizado e títulos gerados no Contas a Pagar."
                : "Rascunho da compra salvo com sucesso.";

            return RedirectToPage("Compra", new
            {
                nrNota = chave.NrNota,
                nrSerie = chave.NrSerie,
                nrModelo = chave.NrModelo,
                idFornecedor = chave.IdFornecedor
            });
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            Erro = mensagem;
        }

        // Em caso de erro, recarrega os combos e reexibe a tela com os dados, os
        // itens e as parcelas digitados. A situacao e a do banco: a gravacao
        // falhou, entao a compra continua como estava (em geral, rascunho).
        Vm.Combos = await _business.GetCombos();
        Vm.Edicao = !string.IsNullOrEmpty(OrigNrNota);
        (Vm.Itens, Vm.Pagamentos) = await _business.GetDigitados(input);
        var situacao = await _business.GetSituacao(Vm.Edicao ? input.ChaveOriginal! : input.GetChave()) ?? Compra.RASCUNHO;

        // Numa inclusao, a chave so volta travada se ainda estiver livre.
        ChaveValidada = Vm.Edicao || await ChaveLivre(input.GetChave());
        if (ChaveValidada) CondicaoFornecedor = await GetCondicaoFornecedor(IdFornecedor);
        Vm.Compra = new Compra
        {
            NrNota = NrNota,
            NrSerie = NrSerie,
            NrModelo = NrModelo,
            IdFornecedor = IdFornecedor,
            DtEmissao = DtEmissao ?? DateOnly.FromDateTime(DateTime.Today),
            DtChegada = DtChegada,
            IdTransportador = IdTransportador,
            IdCondicaoPagamento = IdCondicaoPagamento,
            VlFrete = VlFrete,
            VlSeguro = VlSeguro,
            VlOutrasDespesas = VlOutrasDespesas,
            DsObservacao = DsObservacao,
            TpStatus = situacao,
            Fornecedor = await _fornecedor.GetFornecedor(IdFornecedor),
            Transportador = IdTransportador is > 0 ? await _transportador.GetTransportador(IdTransportador.Value) : null
        };

        return Page();
    }

    // Chave de compra nova ainda disponivel (fornecedor ativo e nota nao lancada).
    private async Task<bool> ChaveLivre(CompraChaveDTO chave)
    {
        try
        {
            await _business.ValidarChaveNova(chave);
            return true;
        }
        catch (ValidacaoException)
        {
            return false;
        }
    }

    // ============================================================
    // Rascunho gravado desde a validacao da chave
    // ============================================================

    // Valida a chave de uma compra nova e ja grava o rascunho (so o cabecalho,
    // com a emissao de hoje e a condicao do fornecedor). Dali em diante a compra
    // existe no banco e a tela salva sozinha (OnPostAutoSalvar): queda de energia
    // ou sair da tela nao perdem o que foi lancado.
    public async Task<IActionResult> OnPostIniciarRascunho()
    {
        var chave = new CompraChaveDTO { NrNota = NrNota, NrSerie = NrSerie, NrModelo = NrModelo, IdFornecedor = IdFornecedor };
        string nomeFornecedor;
        try
        {
            nomeFornecedor = await _business.ValidarChaveNova(chave);
            await _business.Salvar(new CompraDTO
            {
                NrNota = NrNota,
                NrSerie = NrSerie,
                NrModelo = NrModelo,
                IdFornecedor = IdFornecedor,
                DtEmissao = DateOnly.FromDateTime(DateTime.Today),
                IdCondicaoPagamento = await GetCondicaoFornecedor(IdFornecedor),
                TpStatus = Compra.RASCUNHO
            });
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            // O nome do fornecedor volta mesmo no erro, para a tela mostrar de quem e a nota.
            var fornecedor = IdFornecedor > 0 ? await _fornecedor.GetFornecedor(IdFornecedor) : null;
            return new JsonResult(new { ok = false, erro = mensagem, fornecedor = fornecedor?.DsRazaoSocial });
        }

        return new JsonResult(new
        {
            ok = true,
            fornecedor = nomeFornecedor,
            // Chave normalizada (so digitos, com os zeros a esquerda), igual a gravada.
            url = Url.Page("Compra", new { nrNota = chave.NrNota, nrSerie = chave.NrSerie, nrModelo = NrModelo, idFornecedor = IdFornecedor })
        });
    }

    // Salvamento automatico chamado pela tela a cada alteracao. So vale para
    // compra que continua em rascunho: finalizada so muda pelo botao Salvar,
    // porque regrava estoque e titulos.
    public async Task<IActionResult> OnPostAutoSalvar()
    {
        var status = await _business.GetSituacao(new CompraChaveDTO
        {
            NrNota = OrigNrNota,
            NrSerie = OrigNrSerie,
            NrModelo = OrigNrModelo,
            IdFornecedor = OrigIdFornecedor
        });
        if (status == null)
            return new JsonResult(new { ok = false, erro = "Esta compra foi excluída em outra tela. Recarregue a tela." });
        if (status != Compra.RASCUNHO)
            return new JsonResult(new { ok = false, erro = "Só compras em rascunho são salvas automaticamente." });

        TpStatus = Compra.RASCUNHO;
        try
        {
            await _business.Salvar(MontarDados());
            return new JsonResult(new { ok = true, erro = (string?)null, hora = DateTime.Now.ToString("HH:mm:ss") });
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            return new JsonResult(new { ok = false, erro = mensagem });
        }
    }

    // ============================================================
    // Consultas AJAX da tela (as pesquisas de fornecedor, transportadora,
    // produto e servico usam as buscas das telas de cadastro, pelos componentes)
    // ============================================================

    private Task<int?> GetCondicaoFornecedor(int idFornecedor) => _fornecedor.GetCondicaoPagamento(idFornecedor);

    // Parcelas sugeridas a partir da condicao de pagamento escolhida.
    // Usa as parcelas ja cadastradas em tbFinCondicoesPagamentoParcelas
    // (dias de vencimento, percentual e forma de pagamento).
    public async Task<IActionResult> OnGetBuscarParcelasCondicao(int idCondicao)
    {
        var parcelas = await _condicao.GetParcelas(idCondicao);
        return new JsonResult(parcelas.Select(p => new
        {
            nrParcela = p.NrParcela,
            dias = p.NrDiasVencimento,
            percentual = p.VlPercentual,
            idFormaPagamento = p.IdFormaPagamento
        }));
    }
}
