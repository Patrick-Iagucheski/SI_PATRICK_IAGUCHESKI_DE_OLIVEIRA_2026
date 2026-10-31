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

    // Cidades, estados e paises do modal sao carregados pelo proprio
    // componente (ViewComponents/CadastroFornecedorViewComponent).
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
    }

    private static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    public async Task<IActionResult> OnPostAsync()
    {
        var erro = await NormalizarEValidarAsync();
        if (erro != null) return await ComErroAsync(erro);

        await GravarAsync();
        return RedirectToPage();
    }

    // Cadastro pelo componente em modo AJAX (ex.: botao + da pesquisa de
    // fornecedor na Compra). Mesmas regras do OnPostAsync, mas responde JSON
    // para a tela de origem nao ser recarregada.
    public async Task<IActionResult> OnPostSalvarAjaxAsync()
    {
        var erro = await NormalizarEValidarAsync();
        if (erro != null) return new JsonResult(new { sucesso = false, mensagem = erro });

        await GravarAsync();
        return new JsonResult(new
        {
            sucesso = true,
            id = FornecedorForm.IdFornecedor,
            razaoSocial = FornecedorForm.DsRazaoSocial,
            nomeFantasia = FornecedorForm.DsNomeFantasia
        });
    }

    // Limpa mascaras, descarta campos que nao pertencem ao tipo de pessoa e
    // valida o FornecedorForm. Retorna a mensagem do primeiro erro, ou null.
    private async Task<string?> NormalizarEValidarAsync()
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
        FornecedorForm.NrRG = SomenteNumeros(FornecedorForm.NrRG);

        // Mantem preenchidos apenas os campos referentes ao tipo selecionado.
        // PJ (J)  -> CNPJ, Inscricao Estadual, Inscricao Municipal
        // PF (F)  -> CPF, RG, Data de Nascimento
        // E / O   -> Documento, Inscricao Estadual, Data de Nascimento
        switch (tipo)
        {
            case "F":
                FornecedorForm.NrCNPJ = null; FornecedorForm.NrDocumento = null;
                FornecedorForm.NrInscEstadual = null; FornecedorForm.NrInscMunicipal = null;
                break;
            case "J":
                FornecedorForm.NrCpf = null; FornecedorForm.NrDocumento = null;
                FornecedorForm.NrRG = null; FornecedorForm.DtNascimento = null;
                break;
            default: // E / O
                FornecedorForm.NrCpf = null; FornecedorForm.NrCNPJ = null;
                FornecedorForm.NrRG = null; FornecedorForm.NrInscMunicipal = null;
                break;
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
        FornecedorForm.NrInscEstadual = Truncar(FornecedorForm.NrInscEstadual, 13);
        FornecedorForm.NrInscMunicipal = Truncar(FornecedorForm.NrInscMunicipal, 15);
        FornecedorForm.NrRG = Truncar(FornecedorForm.NrRG, 9);
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
            return "Informe a Razão Social / Nome do fornecedor.";

        if (tipo == "F" && !string.IsNullOrEmpty(FornecedorForm.NrCpf) && !CpfValido(FornecedorForm.NrCpf))
            return "CPF inválido. Verifique os números informados.";

        if (tipo == "J" && !string.IsNullOrEmpty(FornecedorForm.NrCNPJ) && !CnpjValido(FornecedorForm.NrCNPJ))
            return "CNPJ inválido. Verifique os números informados.";

        // RG obrigatorio (9 digitos) para Pessoa Fisica
        if (tipo == "F")
        {
            if (string.IsNullOrWhiteSpace(FornecedorForm.NrRG))
                return "Informe o RG do fornecedor (Pessoa Física).";
            if (FornecedorForm.NrRG.Length != 9)
                return "RG inválido. Informe 9 dígitos.";
        }

        // Inscricoes para Pessoa Juridica (validadas apenas quando preenchidas)
        if (tipo == "J")
        {
            if (!string.IsNullOrEmpty(FornecedorForm.NrInscEstadual) &&
                (FornecedorForm.NrInscEstadual.Length < 8 || !FornecedorForm.NrInscEstadual.All(char.IsDigit)))
                return "Inscrição Estadual deve ter de 8 a 13 dígitos.";

            if (!string.IsNullOrEmpty(FornecedorForm.NrInscMunicipal) &&
                FornecedorForm.NrInscMunicipal.Length < 7)
                return "Inscrição Municipal deve ter de 7 a 15 caracteres.";
        }

        if (!string.IsNullOrWhiteSpace(FornecedorForm.DsEmail) &&
            !new EmailAddressAttribute().IsValid(FornecedorForm.DsEmail))
            return "E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).";

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
                return "Chave PIX inválida para o tipo selecionado.";
        }

        // Condicao de pagamento vem do dropdown; so aceita uma ja cadastrada e ativa.
        if (FornecedorForm.IdCondicaoPagamento is int idCondicao
            && !await _db.CondicoesPagamento.AnyAsync(c => c.IdCondicaoPagamento == idCondicao && c.FlAtivo))
            return "Condição de pagamento inválida.";

        // Mesmo CPF/CNPJ/documento ja cadastrado em outro fornecedor.
        var documento = tipo switch
        {
            "F" => FornecedorForm.NrCpf,
            "J" => FornecedorForm.NrCNPJ,
            _ => FornecedorForm.NrDocumento
        };
        if (!string.IsNullOrEmpty(documento))
        {
            var idAtual = FornecedorForm.IdFornecedor;
            var duplicado = await _db.Fornecedores
                .Where(f => f.IdFornecedor != idAtual
                         && (f.NrCpf == documento || f.NrCNPJ == documento || f.NrDocumento == documento))
                .Select(f => f.DsRazaoSocial)
                .FirstOrDefaultAsync();
            if (duplicado != null)
                return $"Já existe um fornecedor cadastrado com este documento: {duplicado}.";
        }

        return null;
    }

    // Inclui ou atualiza o fornecedor ja normalizado e validado.
    private async Task GravarAsync()
    {
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
                e.NrRG = FornecedorForm.NrRG;
                e.DtNascimento = FornecedorForm.DtNascimento;
                e.DsEmail = FornecedorForm.DsEmail;
                e.DsObservacao = FornecedorForm.DsObservacao;
                e.FlAtivo = FornecedorForm.FlAtivo;
                e.NrCEP = FornecedorForm.NrCEP;
                e.DsEndereco = FornecedorForm.DsEndereco;
                e.NrNumero = FornecedorForm.NrNumero;
                e.DsComplemento = FornecedorForm.DsComplemento;
                e.DsBairro = FornecedorForm.DsBairro;
                e.IdCidade = FornecedorForm.IdCidade;
                e.IdCondicaoPagamento = FornecedorForm.IdCondicaoPagamento;
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
        if (fornecedor == null)
            return RedirectToPage();

        // Filha com DbSet: tbFinContasAPagar.idFornecedor.
        // tbFisNFe.idFornecedor tambem aponta para ca, mas nao tem DbSet -
        // quem cobre esse caso e o catch de DbUpdateException abaixo.
        if (await _db.ContasAPagar.AnyAsync(c => c.IdFornecedor == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe conta a pagar para este fornecedor. Inative-o.";
            return RedirectToPage();
        }

        try
        {
            _db.Fornecedores.Remove(fornecedor);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Fornecedor excluído com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a este fornecedor. Inative-o.";
        }
        return RedirectToPage();
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
                rg = f.NrRG,
                dataNascimento = f.DtNascimento,
                email = f.DsEmail,
                observacao = f.DsObservacao,
                cep = f.NrCEP,
                endereco = f.DsEndereco,
                numero = f.NrNumero,
                complemento = f.DsComplemento,
                bairro = f.DsBairro,
                idCidade = f.IdCidade,
                idCondicaoPagamento = f.IdCondicaoPagamento,
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
