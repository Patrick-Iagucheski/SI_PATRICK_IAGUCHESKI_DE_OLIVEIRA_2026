using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do fornecedor. Usada pela tela de Fornecedores, pelo
// componente de cadastro de fornecedor (inclusive a pesquisa de fornecedor da Compra) e pelas
// consultas de fornecedor das outras telas (Compra, Contas a Pagar).
public class FornecedorBusiness
{
    private readonly AppDbContext _db;

    public FornecedorBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // Fornecedores para listas. Por padrao so os ativos.
    public async Task<List<Fornecedor>> Listar(bool somenteAtivos = true) =>
        await _db.Fornecedores
            .Include(f => f.Cidade).ThenInclude(c => c!.Estado)
            .Include(f => f.CondicaoPagamento)
            .Where(f => !somenteAtivos || f.FlAtivo)
            .OrderBy(f => f.DsRazaoSocial)
            .ToListAsync();

    public async Task<Fornecedor?> GetFornecedor(int id) => await _db.Fornecedores.FindAsync(id);

    // Condicao de pagamento padrao do cadastro do fornecedor (a Compra trava o campo com ela).
    public async Task<int?> GetCondicaoPagamento(int id) =>
        await _db.Fornecedores.Where(f => f.IdFornecedor == id)
            .Select(f => f.IdCondicaoPagamento)
            .FirstOrDefaultAsync();

    // Pesquisa da tela de Fornecedores: por codigo, razao social, nome fantasia
    // ou CPF/CNPJ/documento. Sem "inativos" marcado traz so os ativos; marcado, so os inativos.
    public async Task<List<Fornecedor>> Buscar(int? id, string? razao, string? fantasia, string? documento, bool somenteInativos)
    {
        var query = _db.Fornecedores
            .Include(f => f.Cidade).ThenInclude(c => c!.Estado)
            .Include(f => f.CondicaoPagamento)
            .Where(f => f.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(f => f.IdFornecedor == id.Value);

        if (!string.IsNullOrWhiteSpace(razao))
        {
            query = query.Where(Module.MontarFiltroTexto<Fornecedor>(razao, f => f.DsRazaoSocial));
        }

        if (!string.IsNullOrWhiteSpace(fantasia))
        {
            query = query.Where(Module.MontarFiltroTexto<Fornecedor>(fantasia, f => f.DsNomeFantasia));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var dig = Module.SomenteNumeros(documento) ?? "";
            if (dig != "")
                query = query.Where(f =>
                    (f.NrCpf != null && f.NrCpf.Contains(dig)) ||
                    (f.NrCNPJ != null && f.NrCNPJ.Contains(dig)) ||
                    (f.NrDocumento != null && f.NrDocumento.Contains(dig)));
            else
            {
                query = query.Where(Module.MontarFiltroTexto<Fornecedor>(documento, f => f.NrDocumento));
            }
        }

        return await query.OrderBy(f => f.DsRazaoSocial).Take(100).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdFornecedor = 0) ou altera o fornecedor e devolve o registro gravado.
    public async Task<Fornecedor> Salvar(Fornecedor fornecedor)
    {
        Normalizar(fornecedor);
        await Validar(fornecedor);

        Fornecedor registro;
        if (fornecedor.IdFornecedor == 0)
        {
            registro = new Fornecedor();
            _db.Fornecedores.Add(registro);
        }
        else
        {
            registro = await _db.Fornecedores.FindAsync(fornecedor.IdFornecedor)
                ?? throw new ValidacaoException("Fornecedor não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.TpPessoa = fornecedor.TpPessoa;
        registro.DsRazaoSocial = fornecedor.DsRazaoSocial;
        registro.DsNomeFantasia = fornecedor.DsNomeFantasia;
        registro.NrCpf = fornecedor.NrCpf;
        registro.NrCNPJ = fornecedor.NrCNPJ;
        registro.NrDocumento = fornecedor.NrDocumento;
        registro.NrInscEstadual = fornecedor.NrInscEstadual;
        registro.NrInscMunicipal = fornecedor.NrInscMunicipal;
        registro.NrRG = fornecedor.NrRG;
        registro.DtNascimento = fornecedor.DtNascimento;
        registro.DsEmail = fornecedor.DsEmail;
        registro.DsObservacao = fornecedor.DsObservacao;
        registro.FlAtivo = fornecedor.FlAtivo;
        registro.NrCEP = fornecedor.NrCEP;
        registro.DsEndereco = fornecedor.DsEndereco;
        registro.NrNumero = fornecedor.NrNumero;
        registro.DsComplemento = fornecedor.DsComplemento;
        registro.DsBairro = fornecedor.DsBairro;
        registro.IdCidade = fornecedor.IdCidade;
        registro.IdCondicaoPagamento = fornecedor.IdCondicaoPagamento;
        registro.NrTelefoneFixo = fornecedor.NrTelefoneFixo;
        registro.NrCelular = fornecedor.NrCelular;
        registro.DsSite = fornecedor.DsSite;
        registro.DsBanco = fornecedor.DsBanco;
        registro.NrAgencia = fornecedor.NrAgencia;
        registro.TpConta = fornecedor.TpConta;
        registro.NrConta = fornecedor.NrConta;
        registro.NrDigitoConta = fornecedor.NrDigitoConta;
        registro.TpChavePix = fornecedor.TpChavePix;
        registro.DsChavePix = fornecedor.DsChavePix;

        await _db.SaveChangesAsync();
        return registro;
    }

    // Tipo de pessoa valido, mascaras removidas, so os campos do tipo escolhido
    // e cada campo no tamanho da coluna.
    private static void Normalizar(Fornecedor f)
    {
        // F = Fisica, J = Juridica, E = Estrangeiro, O = Outro
        var tipo = (f.TpPessoa ?? "J").Trim().ToUpper();
        if (tipo is not ("F" or "J" or "E" or "O")) tipo = "J";
        f.TpPessoa = tipo;

        f.NrCpf = Module.SomenteNumeros(f.NrCpf);
        f.NrCNPJ = Module.SomenteNumeros(f.NrCNPJ);
        // CEP e telefones brasileiros conferidos (8 digitos; DDD + 10 ou 11).
        // Estrangeiro/outro pode ter outro formato: so tira a mascara.
        var brasileiro = tipo is "F" or "J";
        f.NrCEP = brasileiro ? Module.CepValido(f.NrCEP) : Module.SomenteNumeros(f.NrCEP);
        f.NrTelefoneFixo = brasileiro ? Module.TelefoneValido(f.NrTelefoneFixo, "Telefone") : Module.SomenteNumeros(f.NrTelefoneFixo);
        f.NrCelular = brasileiro ? Module.TelefoneValido(f.NrCelular, "Celular") : Module.SomenteNumeros(f.NrCelular);
        f.NrRG = Module.SomenteNumeros(f.NrRG);

        // PJ (J)  -> CNPJ, Nome Fantasia, Inscricao Estadual, Inscricao Municipal
        // PF (F)  -> CPF, RG, Data de Nascimento
        // E / O   -> Documento, Inscricao Estadual, Data de Nascimento
        switch (tipo)
        {
            case "F":
                f.NrCNPJ = null; f.NrDocumento = null; f.NrInscEstadual = null; f.NrInscMunicipal = null;
                f.DsNomeFantasia = null;
                break;
            case "J":
                f.NrCpf = null; f.NrDocumento = null; f.NrRG = null; f.DtNascimento = null;
                break;
            default:
                f.NrCpf = null; f.NrCNPJ = null; f.NrRG = null; f.NrInscMunicipal = null;
                f.DsNomeFantasia = null;
                break;
        }

        // Chave PIX: tira a mascara quando o tipo for CPF/CNPJ/CELULAR
        var tpPix = (f.TpChavePix ?? "").Trim().ToUpper();
        if (tpPix is "CPF" or "CNPJ" or "CELULAR")
            f.DsChavePix = Module.SomenteNumeros(f.DsChavePix);

        f.DsRazaoSocial = Module.Truncar(f.DsRazaoSocial?.Trim(), 100) ?? string.Empty;
        f.DsNomeFantasia = Module.Truncar(f.DsNomeFantasia, 150);
        f.NrCpf = Module.Truncar(f.NrCpf, 11);
        f.NrCNPJ = Module.Truncar(f.NrCNPJ, 14);
        f.NrDocumento = Module.Truncar(f.NrDocumento, 30);
        f.NrInscEstadual = Module.Truncar(f.NrInscEstadual, 13);
        f.NrInscMunicipal = Module.Truncar(f.NrInscMunicipal, 15);
        f.NrRG = Module.Truncar(f.NrRG, 9);
        f.DsEmail = Module.Truncar(f.DsEmail, 100);
        f.DsObservacao = Module.Truncar(f.DsObservacao, 500);
        f.NrCEP = Module.Truncar(f.NrCEP, 8);
        f.DsEndereco = Module.Truncar(f.DsEndereco, 100);
        f.NrNumero = Module.Truncar(f.NrNumero, 10);
        f.DsComplemento = Module.Truncar(f.DsComplemento, 100);
        f.DsBairro = Module.Truncar(f.DsBairro, 60);
        f.NrTelefoneFixo = Module.Truncar(f.NrTelefoneFixo, 11);
        f.NrCelular = Module.Truncar(f.NrCelular, 11);
        f.DsSite = Module.Truncar(f.DsSite?.Trim(), 150);
        f.DsBanco = Module.Truncar(f.DsBanco, 60);
        f.NrAgencia = Module.Truncar(f.NrAgencia, 10);
        f.NrConta = Module.Truncar(f.NrConta, 20);
        f.NrDigitoConta = Module.Truncar(f.NrDigitoConta, 2);
        f.DsChavePix = Module.Truncar(f.DsChavePix, 100);
    }

    private async Task Validar(Fornecedor f)
    {
        if (string.IsNullOrWhiteSpace(f.DsRazaoSocial))
            throw new ValidacaoException("Informe a Razão Social / Nome do fornecedor.");
        Module.ValidarNascimento(f.DtNascimento);

        // Documento do tipo de pessoa obrigatorio: CPF na fisica, CNPJ na juridica.
        if (f.TpPessoa == "F")
        {
            if (string.IsNullOrEmpty(f.NrCpf))
                throw new ValidacaoException("Informe o CPF do fornecedor.");
            if (!Module.CpfValido(f.NrCpf))
                throw new ValidacaoException("CPF inválido. Verifique os números informados.");
        }

        if (f.TpPessoa == "J")
        {
            if (string.IsNullOrEmpty(f.NrCNPJ))
                throw new ValidacaoException("Informe o CNPJ do fornecedor.");
            if (!Module.CnpjValido(f.NrCNPJ))
                throw new ValidacaoException("CNPJ inválido. Verifique os números informados.");
        }

        // RG opcional; quando informado (Pessoa Fisica), com 9 digitos.
        if (f.TpPessoa == "F" && !string.IsNullOrEmpty(f.NrRG) && f.NrRG.Length != 9)
            throw new ValidacaoException("RG inválido. Informe 9 dígitos.");

        // Inscricoes para Pessoa Juridica (validadas apenas quando preenchidas)
        if (f.TpPessoa == "J")
        {
            if (!string.IsNullOrEmpty(f.NrInscEstadual) && (f.NrInscEstadual.Length < 8 || !f.NrInscEstadual.All(char.IsDigit)))
                throw new ValidacaoException("Inscrição Estadual deve ter de 8 a 13 dígitos.");
            if (!string.IsNullOrEmpty(f.NrInscMunicipal) && f.NrInscMunicipal.Length < 7)
                throw new ValidacaoException("Inscrição Municipal deve ter de 7 a 15 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(f.DsEmail) && !new EmailAddressAttribute().IsValid(f.DsEmail))
            throw new ValidacaoException("E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).");

        // Chave PIX conforme o tipo
        var tpPix = (f.TpChavePix ?? "").Trim().ToUpper();
        if (!string.IsNullOrWhiteSpace(f.DsChavePix) && tpPix != "")
        {
            var chave = f.DsChavePix;
            var pixOk = tpPix switch
            {
                "CPF" => Module.CpfValido(chave),
                "CNPJ" => Module.CnpjValido(chave),
                "EMAIL" => new EmailAddressAttribute().IsValid(chave),
                "CELULAR" => (Module.SomenteNumeros(chave) ?? "").Length is 10 or 11,
                _ => true // ALEATORIA / outros
            };
            if (!pixOk)
                throw new ValidacaoException("Chave PIX inválida para o tipo selecionado.");
        }

        // Condicao de pagamento vem da pesquisa do campo; so aceita uma ja cadastrada e ativa.
        if (f.IdCondicaoPagamento is int idCondicao
            && !await _db.CondicoesPagamento.AnyAsync(c => c.IdCondicaoPagamento == idCondicao && c.FlAtivo)
            && !await _db.Fornecedores.AnyAsync(x => x.IdFornecedor == f.IdFornecedor && x.IdCondicaoPagamento == idCondicao))
            throw new ValidacaoException("Condição de pagamento inválida.");

        // Mesmo CPF/CNPJ/documento ja cadastrado em outro fornecedor.
        var documento = f.TpPessoa switch
        {
            "F" => f.NrCpf,
            "J" => f.NrCNPJ,
            _ => f.NrDocumento
        };
        if (!string.IsNullOrEmpty(documento))
        {
            var duplicado = await _db.Fornecedores
                .Where(x => x.IdFornecedor != f.IdFornecedor
                         && (x.NrCpf == documento || x.NrCNPJ == documento || x.NrDocumento == documento))
                .Select(x => x.DsRazaoSocial)
                .FirstOrDefaultAsync();
            if (duplicado != null)
                throw new ValidacaoException($"Já existe um fornecedor cadastrado com este documento: {duplicado}.");
        }
    }

    public async Task Excluir(int id)
    {
        var fornecedor = await _db.Fornecedores.FindAsync(id);
        if (fornecedor == null)
            return;

        // Filhas com DbSet: tbCmpCompras e tbFinContasAPagar (idFornecedor).
        // tbFisNFe.idFornecedor tambem aponta para ca, mas nao tem DbSet: quem
        // cobre e o catch abaixo.
        const string dica = "Inative o fornecedor em vez de excluí-lo.";
        if (await _db.Compras.AnyAsync(c => c.IdFornecedor == id))
            throw new ValidacaoException("Não é possível excluir: existe compra deste fornecedor.", dica);
        if (await _db.ContasAPagar.AnyAsync(c => c.IdFornecedor == id))
            throw new ValidacaoException("Não é possível excluir: existe conta a pagar para este fornecedor.", dica);

        try
        {
            _db.Fornecedores.Remove(fornecedor);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este fornecedor.", dica);
        }
    }
}
