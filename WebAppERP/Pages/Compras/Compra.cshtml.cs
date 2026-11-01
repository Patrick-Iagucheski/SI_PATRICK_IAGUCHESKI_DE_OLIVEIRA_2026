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

public class CompraModel : PageModel
{
    private readonly CompraBusiness _business;
    private readonly FornecedorBusiness _fornecedor;
    private readonly CondicaoPagamentoBusiness _condicao;
    private readonly ServicoBusiness _servico;
    private readonly TransportadorBusiness _transportador;
    private readonly ProdutoBusiness _produto;

    public CompraModel(CompraBusiness business, FornecedorBusiness fornecedor, CondicaoPagamentoBusiness condicao,
        ServicoBusiness servico, TransportadorBusiness transportador, ProdutoBusiness produto)
    {
        _business = business;
        _fornecedor = fornecedor;
        _condicao = condicao;
        _servico = servico;
        _transportador = transportador;
        _produto = produto;
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
    [BindProperty] public int NrNota { get; set; }
    [BindProperty] public int NrSerie { get; set; }
    [BindProperty] public int NrModelo { get; set; } = 55;
    [BindProperty] public int IdFornecedor { get; set; }

    // Chave com que a compra foi carregada (vazia numa inclusao). Serve para
    // o repositorio detectar tentativa de alterar a propria chave.
    [BindProperty] public int OrigNrNota { get; set; }
    [BindProperty] public int OrigNrSerie { get; set; }
    [BindProperty] public int OrigNrModelo { get; set; }
    [BindProperty] public int OrigIdFornecedor { get; set; }

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

    public async Task<IActionResult> OnGet(int? nrNota, int? nrSerie, int? nrModelo, int? idFornecedor)
    {
        Vm.Combos = await _business.GetCombos();

        var chaveInformada = nrNota is > 0 && nrModelo is > 0 && idFornecedor is > 0;
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
            NrNota = nrNota!.Value,
            NrSerie = nrSerie ?? 0,
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
        catch (ValidacaoException ex)
        {
            Erro = ex.MensagemCompleta;
        }

        // Em caso de erro, recarrega os combos e reexibe a tela com os dados digitados.
        Vm.Combos = await _business.GetCombos();
        Vm.Edicao = OrigNrNota > 0;

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
            TpStatus = TpStatus,
            DsObservacao = DsObservacao,
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
        catch (ValidacaoException ex)
        {
            // O nome do fornecedor volta mesmo no erro, para a tela mostrar de quem e a nota.
            var fornecedor = IdFornecedor > 0 ? await _fornecedor.GetFornecedor(IdFornecedor) : null;
            return new JsonResult(new { ok = false, erro = ex.MensagemCompleta, fornecedor = fornecedor?.DsRazaoSocial });
        }

        return new JsonResult(new
        {
            ok = true,
            fornecedor = nomeFornecedor,
            url = Url.Page("Compra", new { nrNota = NrNota, nrSerie = NrSerie, nrModelo = NrModelo, idFornecedor = IdFornecedor })
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
        if (status != Compra.RASCUNHO)
            return new JsonResult(new { ok = false, erro = "Só compras em rascunho são salvas automaticamente." });

        TpStatus = Compra.RASCUNHO;
        try
        {
            await _business.Salvar(MontarDados());
            return new JsonResult(new { ok = true, erro = (string?)null, hora = DateTime.Now.ToString("HH:mm:ss") });
        }
        catch (ValidacaoException ex)
        {
            return new JsonResult(new { ok = false, erro = ex.MensagemCompleta });
        }
    }

    // ============================================================
    // Consultas AJAX usadas pelos campos de pesquisa da tela
    // (mesmo padrao de OnGetBuscarContas em ContasAPagar)
    // ============================================================

    private Task<int?> GetCondicaoFornecedor(int idFornecedor) => _fornecedor.GetCondicaoPagamento(idFornecedor);

    public async Task<IActionResult> OnGetBuscarFornecedor(int? id, string? termo)
    {
        var lista = await _fornecedor.Pesquisar(id, termo);
        return new JsonResult(lista.Select(f => new { id = f.IdFornecedor, nome = f.DsRazaoSocial, fantasia = f.DsNomeFantasia }));
    }

    public async Task<IActionResult> OnGetBuscarTransportador(int? id, string? termo)
    {
        var lista = await _transportador.Pesquisar(id, termo);
        return new JsonResult(lista.Select(t => new { id = t.IdTransportador, nome = t.DsRazaoSocial }));
    }

    public async Task<IActionResult> OnGetBuscarProduto(int? id, string? termo)
    {
        var lista = await _produto.Pesquisar(id, termo);
        return new JsonResult(lista.Select(p => new
        {
            id = p.IdProduto,
            nome = p.DsProduto,
            unidade = p.SgUnidade,
            custo = p.VlCusto ?? 0,
            saldo = p.QtSaldo ?? 0
        }));
    }

    public async Task<IActionResult> OnGetBuscarServico(int? id, string? termo)
    {
        var lista = await _servico.Pesquisar(id, termo);
        return new JsonResult(lista.Select(s => new { id = s.IdServico, nome = s.DsServico, valor = s.VlServico }));
    }

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
