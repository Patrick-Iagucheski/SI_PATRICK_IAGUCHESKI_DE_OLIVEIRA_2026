using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do cliente. Usada pela tela de Clientes e pelas listas de
// clientes das outras telas (Agendamentos).
public class ClienteBusiness
{
    private readonly AppDbContext _db;

    public ClienteBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // Clientes ativos (listagem da tela e listas para escolher cliente).
    public async Task<List<Cliente>> Listar() =>
        await _db.Clientes
            .Include(c => c.Cidade).ThenInclude(c => c!.Estado)
            .Include(c => c.CondicaoPagamento)
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCliente)
            .ToListAsync();

    // Pesquisa da tela: por codigo, nome, documento (CPF/CNPJ/outro) ou e-mail.
    // Sem "inativos" marcado traz so os ativos; marcado, so os inativos.
    public async Task<List<Cliente>> Buscar(int? id, string? nome, string? documento, string? email, bool somenteInativos)
    {
        var query = _db.Clientes
            .Include(c => c.Cidade).ThenInclude(c => c!.Estado)
            .Include(c => c.CondicaoPagamento)
            .Where(c => c.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(c => c.IdCliente == id.Value);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            query = query.Where(Module.MontarFiltroTexto<Cliente>(nome, c => c.NmCliente));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var digitos = Module.SomenteNumeros(documento) ?? "";
            if (digitos != "")
                query = query.Where(c =>
                    (c.NrCpf != null && c.NrCpf.Contains(digitos)) ||
                    (c.NrCnpj != null && c.NrCnpj.Contains(digitos)) ||
                    (c.NrDocumento != null && c.NrDocumento.Contains(digitos)));
            else
            {
                query = query.Where(Module.MontarFiltroTexto<Cliente>(documento, c => c.NrDocumento));
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(Module.MontarFiltroTexto<Cliente>(email, c => c.DsEmail));
        }

        return await query.OrderBy(c => c.NmCliente).Take(100).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdCliente = 0) ou altera o cliente e devolve o registro gravado.
    public async Task<Cliente> Salvar(Cliente cliente)
    {
        Normalizar(cliente);
        await Validar(cliente);

        Cliente registro;
        if (cliente.IdCliente == 0)
        {
            registro = new Cliente { DtCriacao = DateTime.Now };
            _db.Clientes.Add(registro);
        }
        else
        {
            registro = await _db.Clientes.FindAsync(cliente.IdCliente)
                ?? throw new ValidacaoException("Cliente não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
        }

        registro.NmCliente = cliente.NmCliente;
        registro.TpPessoa = cliente.TpPessoa;
        registro.NrCpf = cliente.NrCpf;
        registro.NrCnpj = cliente.NrCnpj;
        registro.NrDocumento = cliente.NrDocumento;
        registro.NrRG = cliente.NrRG;
        registro.DsNomeFantasia = cliente.DsNomeFantasia;
        registro.NrInscEstadual = cliente.NrInscEstadual;
        registro.NrInscMunicipal = cliente.NrInscMunicipal;
        registro.NrTelefoneFixo = cliente.NrTelefoneFixo;
        registro.NrCelular = cliente.NrCelular;
        registro.DsSite = cliente.DsSite;
        registro.DsEmail = cliente.DsEmail;
        registro.DsEndereco = cliente.DsEndereco;
        registro.NrNumero = cliente.NrNumero;
        registro.DsComplemento = cliente.DsComplemento;
        registro.DsBairro = cliente.DsBairro;
        registro.IdCidade = cliente.IdCidade;
        registro.NrCEP = cliente.NrCEP;
        registro.DtNascimento = cliente.DtNascimento;
        registro.DsSexo = cliente.DsSexo;
        registro.IdCondicaoPagamento = cliente.IdCondicaoPagamento;
        registro.DsObservacao = cliente.DsObservacao;
        registro.FlAtivo = cliente.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    // Tipo de pessoa valido, mascaras removidas (o banco guarda so numeros),
    // so o documento do tipo escolhido e cada campo no tamanho da coluna.
    private static void Normalizar(Cliente c)
    {
        // F = Fisica, J = Juridica, E = Estrangeiro/Outro
        var tipo = (c.TpPessoa ?? "F").Trim().ToUpper();
        if (tipo is not ("F" or "J" or "E")) tipo = "F";
        c.TpPessoa = tipo;

        c.NrCpf = Module.SomenteNumeros(c.NrCpf);
        c.NrCnpj = Module.SomenteNumeros(c.NrCnpj);
        c.NrTelefoneFixo = Module.SomenteNumeros(c.NrTelefoneFixo);
        c.NrCelular = Module.SomenteNumeros(c.NrCelular);
        c.NrCEP = Module.SomenteNumeros(c.NrCEP);
        c.NrRG = Module.SomenteNumeros(c.NrRG);

        // So fica o que vale para o tipo: RG so na fisica; sexo e nascimento
        // nao existem na juridica; nome fantasia e inscricoes so na juridica.
        switch (tipo)
        {
            case "F":
                c.NrCnpj = null; c.NrDocumento = null;
                c.DsNomeFantasia = null; c.NrInscEstadual = null; c.NrInscMunicipal = null;
                break;
            case "J":
                c.NrCpf = null; c.NrDocumento = null; c.NrRG = null; c.DsSexo = null; c.DtNascimento = null;
                break;
            default:   // E
                c.NrCpf = null; c.NrCnpj = null; c.NrRG = null;
                c.DsNomeFantasia = null; c.NrInscEstadual = null; c.NrInscMunicipal = null;
                break;
        }

        c.NmCliente = Module.Truncar(c.NmCliente?.Trim(), 200) ?? string.Empty;
        c.NrCpf = Module.Truncar(c.NrCpf, 11);
        c.NrCnpj = Module.Truncar(c.NrCnpj, 14);
        c.NrDocumento = Module.Truncar(c.NrDocumento, 20);
        c.NrRG = Module.Truncar(c.NrRG, 12);
        c.DsNomeFantasia = Module.Truncar(c.DsNomeFantasia?.Trim(), 150);
        c.NrInscEstadual = Module.Truncar(c.NrInscEstadual?.Trim(), 13);
        c.NrInscMunicipal = Module.Truncar(c.NrInscMunicipal?.Trim(), 15);
        c.NrTelefoneFixo = Module.Truncar(c.NrTelefoneFixo, 11);
        c.NrCelular = Module.Truncar(c.NrCelular, 11);
        c.DsSite = Module.Truncar(c.DsSite?.Trim(), 150);
        c.DsEmail = Module.Truncar(c.DsEmail, 150);
        c.DsEndereco = Module.Truncar(c.DsEndereco, 200);
        c.NrNumero = Module.Truncar(c.NrNumero, 10);
        c.DsComplemento = Module.Truncar(c.DsComplemento, 100);
        c.DsBairro = Module.Truncar(c.DsBairro, 100);
        c.NrCEP = Module.Truncar(c.NrCEP, 8);
        c.DsObservacao = Module.Truncar(c.DsObservacao, 500);
    }

    // Obrigatorios: nome e o documento do tipo de pessoa (CPF na fisica, CNPJ na
    // juridica). E-mail e endereco sao opcionais.
    private async Task Validar(Cliente c)
    {
        if (string.IsNullOrWhiteSpace(c.NmCliente))
            throw new ValidacaoException("Informe o nome do cliente.");

        if (c.TpPessoa == "F")
        {
            if (string.IsNullOrEmpty(c.NrCpf))
                throw new ValidacaoException("Informe o CPF do cliente.");
            if (!Module.CpfValido(c.NrCpf))
                throw new ValidacaoException("CPF inválido. Verifique os números informados.");
        }

        if (c.TpPessoa == "J")
        {
            if (string.IsNullOrEmpty(c.NrCnpj))
                throw new ValidacaoException("Informe o CNPJ do cliente.");
            if (!Module.CnpjValido(c.NrCnpj))
                throw new ValidacaoException("CNPJ inválido. Verifique os números informados.");

            // Inscricoes opcionais, validadas quando preenchidas (mesma regra do fornecedor).
            if (!string.IsNullOrEmpty(c.NrInscEstadual) && (c.NrInscEstadual.Length < 8 || !c.NrInscEstadual.All(char.IsDigit)))
                throw new ValidacaoException("Inscrição Estadual deve ter de 8 a 13 dígitos.");
            if (!string.IsNullOrEmpty(c.NrInscMunicipal) && c.NrInscMunicipal.Length < 7)
                throw new ValidacaoException("Inscrição Municipal deve ter de 7 a 15 caracteres.");
        }

        // Condicao de pagamento vem da pesquisa do campo; so aceita uma ja cadastrada e ativa.
        if (c.IdCondicaoPagamento is int idCondicao
            && !await _db.CondicoesPagamento.AnyAsync(x => x.IdCondicaoPagamento == idCondicao && x.FlAtivo))
            throw new ValidacaoException("Condição de pagamento inválida.");

        if (!string.IsNullOrWhiteSpace(c.DsEmail) && !new EmailAddressAttribute().IsValid(c.DsEmail))
            throw new ValidacaoException("E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).");

        // CPF e CNPJ unicos (respeita UQ_tbGerClientes_nrCpf e UQ_tbGerClientes_nrCnpj)
        var mesmoDocumento = await _db.Clientes
            .Where(x => x.IdCliente != c.IdCliente &&
                        ((c.NrCpf != null && x.NrCpf == c.NrCpf) || (c.NrCnpj != null && x.NrCnpj == c.NrCnpj)))
            .Select(x => x.NmCliente)
            .FirstOrDefaultAsync();
        if (mesmoDocumento != null)
            throw new ValidacaoException($"Já existe um cliente com este {(c.TpPessoa == "J" ? "CNPJ" : "CPF")}: {mesmoDocumento}.",
                                         "Utilize a pesquisa para encontrá-lo.");
    }

    public async Task Excluir(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente == null)
            return;

        // Filhas: tbOpeAgendamentos.idCliente e tbVenVendas.idCliente.
        if (await _db.Agendamentos.AnyAsync(a => a.IdCliente == id))
            throw new ValidacaoException("Não é possível excluir: existe agendamento para este cliente.", "Inative o cliente em vez de excluí-lo.");
        if (await _db.Vendas.AnyAsync(v => v.IdCliente == id))
            throw new ValidacaoException("Não é possível excluir: existe venda para este cliente.", "Inative o cliente em vez de excluí-lo.");

        try
        {
            _db.Clientes.Remove(cliente);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este cliente.", "Inative o cliente em vez de excluí-lo.");
        }
    }
}
