using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class FornecedoresModel : PageModel
{
    private readonly AppDbContext _db;

    public FornecedoresModel(AppDbContext db) => _db = db;

    public List<GerFornecedor> Fornecedores { get; set; } = [];
    public List<GerCidade> Cidades { get; set; } = [];
    public List<GerEstado> Estados { get; set; } = [];
    public List<GerPais> Paises { get; set; } = [];

    [BindProperty]
    public GerFornecedor FornecedorForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Fornecedores = await _db.Fornecedores
            .Include(f => f.Cidade)
            .ThenInclude(c => c!.Estado)
            .Where(f => f.FlAtivo)
            .OrderBy(f => f.DsRazaoSocial)
            .ToListAsync();

        Cidades = await _db.Cidades.Include(c => c.Estado).OrderBy(c => c.NmCidade).ToListAsync();
        Estados = await _db.Estados.OrderBy(e => e.NmEstado).ToListAsync();
        Paises = await _db.Paises.OrderBy(p => p.NmPais).ToListAsync();
    }

    private static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    public async Task<IActionResult> OnPostAsync()
    {
        // Normaliza o tipo de pessoa (F/J/E/O)
        var tipo = (FornecedorForm.TpPessoa ?? "J").Trim().ToUpper();
        if (tipo is not ("F" or "J" or "E" or "O")) tipo = "J";
        FornecedorForm.TpPessoa = tipo;

        // Remove mascaras dos campos numericos
        FornecedorForm.NrCpf = SomenteNumeros(FornecedorForm.NrCpf);
        FornecedorForm.NrCNPJ = SomenteNumeros(FornecedorForm.NrCNPJ);
        FornecedorForm.NrCEP = SomenteNumeros(FornecedorForm.NrCEP);
        FornecedorForm.NrTelefoneFixo = SomenteNumeros(FornecedorForm.NrTelefoneFixo);
        FornecedorForm.NrCelular = SomenteNumeros(FornecedorForm.NrCelular);
        FornecedorForm.NrWhatsApp = SomenteNumeros(FornecedorForm.NrWhatsApp);

        // Mantem preenchido apenas o documento referente ao tipo selecionado
        switch (tipo)
        {
            case "F": FornecedorForm.NrCNPJ = null; FornecedorForm.NrDocumento = null; break;
            case "J": FornecedorForm.NrCpf = null; FornecedorForm.NrDocumento = null; break;
            default:  FornecedorForm.NrCpf = null; FornecedorForm.NrCNPJ = null; break; // E / O
        }

        // Chave PIX: tira a mascara quando o tipo for CPF/CNPJ/CELULAR
        var tpPix = (FornecedorForm.TpChavePix ?? "").Trim().ToUpper();
        if (tpPix is "CPF" or "CNPJ" or "CELULAR")
            FornecedorForm.DsChavePix = SomenteNumeros(FornecedorForm.DsChavePix);

        // Truncamento defensivo
        FornecedorForm.DsRazaoSocial = Truncar(FornecedorForm.DsRazaoSocial, 200) ?? string.Empty;
        FornecedorForm.DsNomeFantasia = Truncar(FornecedorForm.DsNomeFantasia, 150);
        FornecedorForm.DsContatoPrincipal = Truncar(FornecedorForm.DsContatoPrincipal, 120);
        FornecedorForm.NrCpf = Truncar(FornecedorForm.NrCpf, 11);
        FornecedorForm.NrCNPJ = Truncar(FornecedorForm.NrCNPJ, 14);
        FornecedorForm.NrDocumento = Truncar(FornecedorForm.NrDocumento, 30);
        FornecedorForm.NrInscEstadual = Truncar(FornecedorForm.NrInscEstadual, 20);
        FornecedorForm.NrInscMunicipal = Truncar(FornecedorForm.NrInscMunicipal, 20);
        FornecedorForm.DsEmail = Truncar(FornecedorForm.DsEmail, 150);
        FornecedorForm.DsObservacao = Truncar(FornecedorForm.DsObservacao, 500);
        FornecedorForm.NrCEP = Truncar(FornecedorForm.NrCEP, 8);
        FornecedorForm.DsEndereco = Truncar(FornecedorForm.DsEndereco, 200);
        FornecedorForm.NrNumero = Truncar(FornecedorForm.NrNumero, 10);
        FornecedorForm.DsComplemento = Truncar(FornecedorForm.DsComplemento, 100);
        FornecedorForm.DsBairro = Truncar(FornecedorForm.DsBairro, 100);
        FornecedorForm.NrTelefoneFixo = Truncar(FornecedorForm.NrTelefoneFixo, 11);
        FornecedorForm.NrCelular = Truncar(FornecedorForm.NrCelular, 11);
        FornecedorForm.NrWhatsApp = Truncar(FornecedorForm.NrWhatsApp, 11);
        FornecedorForm.DsBanco = Truncar(FornecedorForm.DsBanco, 60);
        FornecedorForm.NrAgencia = Truncar(FornecedorForm.NrAgencia, 10);
        FornecedorForm.NrConta = Truncar(FornecedorForm.NrConta, 20);
        FornecedorForm.NrDigitoConta = Truncar(FornecedorForm.NrDigitoConta, 2);
        FornecedorForm.DsChavePix = Truncar(FornecedorForm.DsChavePix, 100);

        // ===== Validacoes amigaveis =====
        if (string.IsNullOrWhiteSpace(FornecedorForm.DsRazaoSocial))
            return await ComErroAsync("Informe a Razao Social / Nome do fornecedor.");

        if (tipo == "F" && !string.IsNullOrEmpty(FornecedorForm.NrCpf) && !CpfValido(FornecedorForm.NrCpf))
            return await ComErroAsync("CPF invalido. Verifique os numeros informados.");

        if (tipo == "J" && !string.IsNullOrEmpty(FornecedorForm.NrCNPJ) && !CnpjValido(FornecedorForm.NrCNPJ))
            return await ComErroAsync("CNPJ invalido. Verifique os numeros informados.");

        if (!string.IsNullOrWhiteSpace(FornecedorForm.DsEmail) &&
            !new EmailAddressAttribute().IsValid(FornecedorForm.DsEmail))
            return await ComErroAsync("E-mail invalido. Verifique o formato (exemplo: nome@dominio.com).");

        // Validacao da chave PIX conforme o tipo
        if (!string.IsNullOrWhiteSpace(FornecedorForm.DsChavePix) && !string.IsNullOrWhiteSpace(tpPix))
        {
            var chave = FornecedorForm.DsChavePix!;
            bool pixOk = tpPix switch
            {
                "CPF" => CpfValido(chave),
                "CNPJ" => CnpjValido(chave),
                "EMAIL" => new EmailAddressAttribute().IsValid(chave),
                "CELULAR" => new string(chave.Where(char.IsDigit).ToArray()).Length is 10 or 11,
                _ => true // ALEATORIA / outros
            };
            if (!pixOk)
                return await ComErroAsync("Chave PIX invalida para o tipo selecionado.");
        }

        if (FornecedorForm.IdFornecedor == 0)
        {
            FornecedorForm.DtCriacao = DateTime.Now;
            _db.Fornecedores.Add(FornecedorForm);
        }
        else
        {
            var e = await _db.Fornecedores.FindAsync(FornecedorForm.IdFornecedor);
            if (e != null)
            {
                e.TpPessoa = FornecedorForm.TpPessoa;
                e.DsRazaoSocial = FornecedorForm.DsRazaoSocial;
                e.DsNomeFantasia = FornecedorForm.DsNomeFantasia;
                e.DsContatoPrincipal = FornecedorForm.DsContatoPrincipal;
                e.NrCpf = FornecedorForm.NrCpf;
                e.NrCNPJ = FornecedorForm.NrCNPJ;
                e.NrDocumento = FornecedorForm.NrDocumento;
                e.NrInscEstadual = FornecedorForm.NrInscEstadual;
                e.NrInscMunicipal = FornecedorForm.NrInscMunicipal;
                e.DsEmail = FornecedorForm.DsEmail;
                e.DsObservacao = FornecedorForm.DsObservacao;
                e.FlAtivo = FornecedorForm.FlAtivo;
                e.NrCEP = FornecedorForm.NrCEP;
                e.DsEndereco = FornecedorForm.DsEndereco;
                e.NrNumero = FornecedorForm.NrNumero;
                e.DsComplemento = FornecedorForm.DsComplemento;
                e.DsBairro = FornecedorForm.DsBairro;
                e.IdCidade = FornecedorForm.IdCidade;
                e.NrTelefoneFixo = FornecedorForm.NrTelefoneFixo;
                e.NrCelular = FornecedorForm.NrCelular;
                e.NrWhatsApp = FornecedorForm.NrWhatsApp;
                e.DsBanco = FornecedorForm.DsBanco;
                e.NrAgencia = FornecedorForm.NrAgencia;
                e.TpConta = FornecedorForm.TpConta;
                e.NrConta = FornecedorForm.NrConta;
                e.NrDigitoConta = FornecedorForm.NrDigitoConta;
                e.TpChavePix = FornecedorForm.TpChavePix;
                e.DsChavePix = FornecedorForm.DsChavePix;
                e.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        TempData["ErroFornecedor"] = mensagem;
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var fornecedor = await _db.Fornecedores.FindAsync(id);
        if (fornecedor != null)
        {
            _db.Fornecedores.Remove(fornecedor);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    // ============================================================
    // Cadastro rapido de Cidade via AJAX
    // ============================================================
    public class CidadeRapidoDto
    {
        public string NmCidade { get; set; } = string.Empty;
        public int IdEstado { get; set; }
    }

    public async Task<IActionResult> OnPostCidadeRapidoAsync([FromBody] CidadeRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmCidade))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome da cidade." });
        if (dto.IdEstado <= 0)
            return new JsonResult(new { sucesso = false, mensagem = "Selecione o estado." });

        var estado = await _db.Estados.FindAsync(dto.IdEstado);
        if (estado is null)
            return new JsonResult(new { sucesso = false, mensagem = "Estado invalido." });

        var nome = dto.NmCidade.Trim();
        bool existe = await _db.Cidades.AnyAsync(c => c.NmCidade == nome && c.IdEstado == dto.IdEstado);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe uma cidade com esse nome neste estado." });

        var cidade = new GerCidade { NmCidade = nome, IdEstado = dto.IdEstado, DtCriacao = DateTime.Now };
        _db.Cidades.Add(cidade);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = cidade.IdCidade, nome = cidade.NmCidade, uf = estado.SgUF });
    }

    // ============================================================
    // Cadastro rapido de Estado via AJAX
    // ============================================================
    public class EstadoRapidoDto
    {
        public string SgUF { get; set; } = string.Empty;
        public string NmEstado { get; set; } = string.Empty;
        public int IdPais { get; set; }
    }

    public async Task<IActionResult> OnPostEstadoRapidoAsync([FromBody] EstadoRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmEstado))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome do estado." });
        if (string.IsNullOrWhiteSpace(dto.SgUF) || dto.SgUF.Trim().Length != 2)
            return new JsonResult(new { sucesso = false, mensagem = "Informe a UF (2 letras)." });
        if (dto.IdPais <= 0)
            return new JsonResult(new { sucesso = false, mensagem = "Selecione o pais." });

        var pais = await _db.Paises.FindAsync(dto.IdPais);
        if (pais is null)
            return new JsonResult(new { sucesso = false, mensagem = "Pais invalido." });

        var uf = dto.SgUF.Trim().ToUpper();
        bool existe = await _db.Estados.AnyAsync(e => e.SgUF == uf && e.IdPais == dto.IdPais);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe um estado com essa UF neste pais." });

        var estado = new GerEstado { SgUF = uf, NmEstado = dto.NmEstado.Trim(), IdPais = dto.IdPais, DtCriacao = DateTime.Now };
        _db.Estados.Add(estado);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = estado.IdEstado, nome = estado.NmEstado, uf = estado.SgUF });
    }

    // ============================================================
    // Cadastro rapido de Pais via AJAX
    // ============================================================
    public class PaisRapidoDto
    {
        public string NmPais { get; set; } = string.Empty;
        public string SgPais { get; set; } = string.Empty;
        public string? NrDDI { get; set; }
        public string? DsMoeda { get; set; }
    }

    public async Task<IActionResult> OnPostPaisRapidoAsync([FromBody] PaisRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmPais))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome do pais." });
        if (string.IsNullOrWhiteSpace(dto.SgPais))
            return new JsonResult(new { sucesso = false, mensagem = "Informe a sigla do pais." });

        var nome = dto.NmPais.Trim();
        bool existe = await _db.Paises.AnyAsync(p => p.NmPais == nome);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe um pais com esse nome." });

        var pais = new GerPais
        {
            NmPais = nome,
            SgPais = dto.SgPais.Trim().ToUpper(),
            NrDDI = string.IsNullOrWhiteSpace(dto.NrDDI) ? null : SomenteNumeros(dto.NrDDI),
            DsMoeda = string.IsNullOrWhiteSpace(dto.DsMoeda) ? null : dto.DsMoeda.Trim()
        };
        _db.Paises.Add(pais);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = pais.IdPais, nome = pais.NmPais });
    }

    // ============================================================
    // Busca de fornecedores via AJAX (filtros da pagina)
    // Pesquisa por: codigo, razao social, nome fantasia ou CPF/CNPJ
    // ============================================================
    public async Task<IActionResult> OnGetBuscarFornecedoresAsync(
        int? id, string? razao, string? fantasia, string? documento, bool incluirInativos = false)
    {
        var query = _db.Fornecedores.Include(f => f.Cidade).ThenInclude(c => c!.Estado).AsQueryable();

        if (incluirInativos)
            query = query.Where(f => !f.FlAtivo);
        else
            query = query.Where(f => f.FlAtivo);

        if (id.HasValue)
            query = query.Where(f => f.IdFornecedor == id.Value);

        if (!string.IsNullOrWhiteSpace(razao))
        {
            var r = razao.Trim();
            query = query.Where(f => f.DsRazaoSocial.Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(fantasia))
        {
            var nf = fantasia.Trim();
            query = query.Where(f => f.DsNomeFantasia != null && f.DsNomeFantasia.Contains(nf));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var dig = new string(documento.Where(char.IsDigit).ToArray());
            if (dig != "")
                query = query.Where(f =>
                    (f.NrCpf != null && f.NrCpf.Contains(dig)) ||
                    (f.NrCNPJ != null && f.NrCNPJ.Contains(dig)) ||
                    (f.NrDocumento != null && f.NrDocumento.Contains(dig)));
            else
            {
                var d = documento.Trim();
                query = query.Where(f => f.NrDocumento != null && f.NrDocumento.Contains(d));
            }
        }

        var lista = await query
            .OrderBy(f => f.DsRazaoSocial)
            .Take(100)
            .Select(f => new
            {
                id = f.IdFornecedor,
                tipoPessoa = f.TpPessoa,
                razaoSocial = f.DsRazaoSocial,
                nomeFantasia = f.DsNomeFantasia,
                contatoPrincipal = f.DsContatoPrincipal,
                cpf = f.NrCpf,
                cnpj = f.NrCNPJ,
                documento = f.NrDocumento,
                inscEstadual = f.NrInscEstadual,
                inscMunicipal = f.NrInscMunicipal,
                email = f.DsEmail,
                observacao = f.DsObservacao,
                cep = f.NrCEP,
                endereco = f.DsEndereco,
                numero = f.NrNumero,
                complemento = f.DsComplemento,
                bairro = f.DsBairro,
                idCidade = f.IdCidade,
                cidadeNome = f.Cidade != null ? f.Cidade.NmCidade + "/" + (f.Cidade.Estado != null ? f.Cidade.Estado.SgUF : "") : null,
                telefoneFixo = f.NrTelefoneFixo,
                celular = f.NrCelular,
                whatsApp = f.NrWhatsApp,
                banco = f.DsBanco,
                agencia = f.NrAgencia,
                tipoConta = f.TpConta,
                conta = f.NrConta,
                digitoConta = f.NrDigitoConta,
                tipoChavePix = f.TpChavePix,
                chavePix = f.DsChavePix,
                ativo = f.FlAtivo
            })
            .ToListAsync();

        return new JsonResult(lista);
    }

    // ============================================================
    // Validacao de CPF / CNPJ (digitos verificadores)
    // ============================================================
    private static bool CpfValido(string? cpf)
    {
        var d = new string((cpf ?? "").Where(char.IsDigit).ToArray());
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        int Soma(int qtd) { int s = 0; for (int i = 0; i < qtd; i++) s += (d[i] - '0') * (qtd + 1 - i); return s; }
        int Dig(int soma) { int r = soma % 11; return r < 2 ? 0 : 11 - r; }
        return Dig(Soma(9)) == d[9] - '0' && Dig(Soma(10)) == d[10] - '0';
    }

    private static bool CnpjValido(string? cnpj)
    {
        var d = new string((cnpj ?? "").Where(char.IsDigit).ToArray());
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;
        int[] p1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] p2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int Dig(int qtd, int[] pesos) { int s = 0; for (int i = 0; i < qtd; i++) s += (d[i] - '0') * pesos[i]; int r = s % 11; return r < 2 ? 0 : 11 - r; }
        return Dig(12, p1) == d[12] - '0' && Dig(13, p2) == d[13] - '0';
    }
}
