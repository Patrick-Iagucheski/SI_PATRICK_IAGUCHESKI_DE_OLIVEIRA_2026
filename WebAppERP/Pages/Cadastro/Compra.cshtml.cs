using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.DTOs;
using WebAppERP.Models;
using WebAppERP.Repositories;
using WebAppERP.ViewModels;

namespace WebAppERP.Pages.Cadastro;

public class CompraModel : PageModel
{
    private readonly ICompraRepository _repo;
    private readonly AppDbContext _db;

    public CompraModel(ICompraRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
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
    [BindProperty] public string TpStatus { get; set; } = "RASCUNHO";
    [BindProperty] public string? DsObservacao { get; set; }

    // Itens e parcelas trafegam serializados em JSON, como na tela de Venda.
    [BindProperty] public string ItensJson { get; set; } = "[]";
    [BindProperty] public string PagamentosJson { get; set; } = "[]";

    // ============================================================
    // Carregamento da tela
    // ============================================================

    public async Task<IActionResult> OnGetAsync(int? nrNota, int? nrSerie, int? nrModelo, int? idFornecedor)
    {
        Vm.Combos = await _repo.ObterCombosAsync();

        var chaveInformada = nrNota is > 0 && nrModelo is > 0 && idFornecedor is > 0;
        if (!chaveInformada)
        {
            // Nova compra: valores iniciais da tela.
            DtEmissao = DateOnly.FromDateTime(DateTime.Today);
            Vm.Compra = new OpeCompra { NrModelo = 55, DtEmissao = DtEmissao.Value };
            NrModelo = 55;
            return Page();
        }

        var chave = new CompraChaveDto
        {
            NrNota = nrNota!.Value,
            NrSerie = nrSerie ?? 0,
            NrModelo = nrModelo!.Value,
            IdFornecedor = idFornecedor!.Value
        };

        var compra = await _repo.ObterCompletaAsync(chave);
        if (compra == null)
        {
            TempData["Erro"] = "Compra não encontrada.";
            return RedirectToPage("Compras");
        }

        Vm.Compra = compra;
        Vm.Itens = compra.Itens.OrderBy(i => i.NrItem).ToList();
        Vm.Pagamentos = compra.Pagamentos.OrderBy(p => p.NrParcela).ToList();
        Vm.ContasGeradas = await _repo.ObterContasGeradasAsync(chave);
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
        CondicaoFornecedor = await CondicaoDoFornecedorAsync(compra.IdFornecedor);
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
    private CompraInputDto MontarInput() => new()
    {
        NrNota = NrNota,
        NrSerie = NrSerie,
        NrModelo = NrModelo,
        IdFornecedor = IdFornecedor,
        ChaveOriginal = new CompraChaveDto
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
        Itens = JsonSerializer.Deserialize<List<CompraItemDto>>(ItensJson ?? "[]", JsonOpts) ?? [],
        Pagamentos = JsonSerializer.Deserialize<List<CompraPagamentoDto>>(PagamentosJson ?? "[]", JsonOpts) ?? []
    };

    public async Task<IActionResult> OnPostAsync()
    {
        var input = MontarInput();
        var (ok, erro, chave) = await _repo.SalvarAsync(input);

        if (ok)
        {
            TempData["Sucesso"] = TpStatus == "FINALIZADA"
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

        // Em caso de erro, recarrega os combos e reexibe a tela com os dados digitados.
        Erro = erro;
        Vm.Combos = await _repo.ObterCombosAsync();
        Vm.Edicao = OrigNrNota > 0;

        // Numa inclusao, a chave so volta travada se ainda estiver livre.
        ChaveValidada = Vm.Edicao || (await _repo.ValidarChaveNovaAsync(input.Chave())).ok;
        if (ChaveValidada) CondicaoFornecedor = await CondicaoDoFornecedorAsync(IdFornecedor);
        Vm.Compra = new OpeCompra
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
            Fornecedor = await _db.Fornecedores.FindAsync(IdFornecedor),
            Transportador = IdTransportador is > 0 ? await _db.Transportadores.FindAsync(IdTransportador) : null
        };

        return Page();
    }

    // ============================================================
    // Rascunho gravado desde a validacao da chave
    // ============================================================

    // Valida a chave de uma compra nova e ja grava o rascunho (so o cabecalho,
    // com a emissao de hoje e a condicao do fornecedor). Dali em diante a compra
    // existe no banco e a tela salva sozinha (OnPostAutoSalvar): queda de energia
    // ou sair da tela nao perdem o que foi lancado.
    public async Task<IActionResult> OnPostIniciarRascunhoAsync()
    {
        var chave = new CompraChaveDto { NrNota = NrNota, NrSerie = NrSerie, NrModelo = NrModelo, IdFornecedor = IdFornecedor };
        var (ok, erro, nomeFornecedor) = await _repo.ValidarChaveNovaAsync(chave);
        if (!ok) return new JsonResult(new { ok, erro, fornecedor = nomeFornecedor });

        var (salvo, erroSalvar, _) = await _repo.SalvarAsync(new CompraInputDto
        {
            NrNota = NrNota,
            NrSerie = NrSerie,
            NrModelo = NrModelo,
            IdFornecedor = IdFornecedor,
            DtEmissao = DateOnly.FromDateTime(DateTime.Today),
            IdCondicaoPagamento = await CondicaoDoFornecedorAsync(IdFornecedor),
            TpStatus = "RASCUNHO"
        });
        if (!salvo) return new JsonResult(new { ok = false, erro = erroSalvar, fornecedor = nomeFornecedor });

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
    public async Task<IActionResult> OnPostAutoSalvarAsync()
    {
        var status = await _db.Compras
            .Where(c => c.NrNota == OrigNrNota && c.NrSerie == OrigNrSerie
                     && c.NrModelo == OrigNrModelo && c.IdFornecedor == OrigIdFornecedor)
            .Select(c => c.TpStatus)
            .FirstOrDefaultAsync();
        if (status != "RASCUNHO")
            return new JsonResult(new { ok = false, erro = "Só compras em rascunho são salvas automaticamente." });

        TpStatus = "RASCUNHO";
        var (ok, erro, _) = await _repo.SalvarAsync(MontarInput());
        return new JsonResult(new { ok, erro, hora = DateTime.Now.ToString("HH:mm:ss") });
    }

    // ============================================================
    // Consultas AJAX usadas pelos campos de pesquisa da tela
    // (mesmo padrao de OnGetBuscarContasAsync em ContasAPagar)
    // ============================================================

    private Task<int?> CondicaoDoFornecedorAsync(int idFornecedor) =>
        _db.Fornecedores.Where(f => f.IdFornecedor == idFornecedor)
            .Select(f => f.IdCondicaoPagamento).FirstOrDefaultAsync();

    public async Task<IActionResult> OnGetBuscarFornecedorAsync(int? id, string? termo)
    {
        var query = _db.Fornecedores.Where(f => f.FlAtivo);

        if (id.HasValue)
            query = query.Where(f => f.IdFornecedor == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(f => f.DsRazaoSocial.Contains(t)
                                  || (f.DsNomeFantasia != null && f.DsNomeFantasia.Contains(t)));
        }

        var lista = await query
            .OrderBy(f => f.DsRazaoSocial)
            .Take(50)
            .Select(f => new { id = f.IdFornecedor, nome = f.DsRazaoSocial, fantasia = f.DsNomeFantasia })
            .ToListAsync();

        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetBuscarTransportadorAsync(int? id, string? termo)
    {
        var query = _db.Transportadores.AsQueryable();

        if (id.HasValue)
            query = query.Where(t => t.IdTransportador == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(x => x.DsRazaoSocial != null && x.DsRazaoSocial.Contains(t));
        }

        var lista = await query
            .OrderBy(t => t.DsRazaoSocial)
            .Take(50)
            .Select(t => new { id = t.IdTransportador, nome = t.DsRazaoSocial })
            .ToListAsync();

        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetBuscarProdutoAsync(int? id, string? termo)
    {
        var query = _db.Produtos.Where(p => p.FlAtivo);

        if (id.HasValue)
            query = query.Where(p => p.IdProduto == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(p => p.DsProduto.Contains(t));
        }

        var lista = await query
            .OrderBy(p => p.DsProduto)
            .Take(50)
            .Select(p => new
            {
                id = p.IdProduto,
                nome = p.DsProduto,
                unidade = p.SgUnidade,
                custo = p.VlCusto ?? 0,
                saldo = p.QtSaldo ?? 0
            })
            .ToListAsync();

        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetBuscarServicoAsync(int? id, string? termo)
    {
        var query = _db.Servicos.Where(s => s.FlAtivo);

        if (id.HasValue)
            query = query.Where(s => s.IdServico == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(s => s.DsServico.Contains(t));
        }

        var lista = await query
            .OrderBy(s => s.DsServico)
            .Take(50)
            .Select(s => new { id = s.IdServico, nome = s.DsServico, valor = s.VlServico })
            .ToListAsync();

        return new JsonResult(lista);
    }

    // Parcelas sugeridas a partir da condicao de pagamento escolhida.
    // Usa as parcelas ja cadastradas em tbFinCondicoesPagamentoParcelas
    // (dias de vencimento, percentual e forma de pagamento).
    public async Task<IActionResult> OnGetParcelasCondicaoAsync(int idCondicao)
    {
        var parcelas = await _db.CondicoesPagamentoParcelas
            .Where(p => p.IdCondicaoPagamento == idCondicao)
            .OrderBy(p => p.NrParcela)
            .Select(p => new
            {
                nrParcela = p.NrParcela,
                dias = p.NrDiasVencimento,
                percentual = p.VlPercentual,
                idFormaPagamento = p.IdFormaPagamento
            })
            .ToListAsync();

        return new JsonResult(parcelas);
    }
}
