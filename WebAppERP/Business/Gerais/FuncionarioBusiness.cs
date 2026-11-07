using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do funcionario. Usada pela tela de Funcionarios e pelas
// listas de funcionarios das outras telas (Agendamentos).
public class FuncionarioBusiness
{
    private readonly AppDbContext _db;

    public FuncionarioBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // Funcionarios ativos (listagem da tela e listas para escolher).
    public async Task<List<Funcionario>> Listar() =>
        await _db.Funcionarios
            .Include(f => f.Cidade).ThenInclude(c => c!.Estado)
            .Include(f => f.Cargo)
            .Where(f => f.FlAtivo)
            .OrderBy(f => f.NmFuncionario)
            .ToListAsync();

    // Pesquisa da tela: por codigo, nome, apelido ou CPF. Sem "inativos"
    // marcado traz so os ativos; marcado, so os inativos.
    public async Task<List<Funcionario>> Buscar(int? id, string? nome, string? apelido, string? cpf, bool somenteInativos)
    {
        var query = _db.Funcionarios.Include(f => f.Cargo).Where(f => f.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(f => f.IdFuncionario == id.Value);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            query = query.Where(Module.MontarFiltroTexto<Funcionario>(nome, f => f.NmFuncionario));
        }

        if (!string.IsNullOrWhiteSpace(apelido))
        {
            query = query.Where(Module.MontarFiltroTexto<Funcionario>(apelido, f => f.NmApelido));
        }

        var cpfDigitos = Module.SomenteNumeros(cpf) ?? "";
        if (cpfDigitos != "")
            query = query.Where(f => f.NrCpf.Contains(cpfDigitos));

        return await query.OrderBy(f => f.NmFuncionario).Take(100).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdFuncionario = 0) ou altera o funcionario e devolve o registro gravado.
    public async Task<Funcionario> Salvar(Funcionario funcionario)
    {
        Normalizar(funcionario);
        await Validar(funcionario);

        Funcionario registro;
        if (funcionario.IdFuncionario == 0)
        {
            registro = new Funcionario { DtCriacao = DateTime.Now };
            _db.Funcionarios.Add(registro);
        }
        else
        {
            registro = await _db.Funcionarios.FindAsync(funcionario.IdFuncionario)
                ?? throw new ValidacaoException("Funcionário não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
        }

        // O cargo vem do campo de cargo (idCargo) e e a unica fonte da verdade:
        // o nome sai da navegacao Cargo.NmCargo na hora de exibir.
        registro.NmFuncionario = funcionario.NmFuncionario;
        registro.NmApelido = funcionario.NmApelido;
        registro.NrCpf = funcionario.NrCpf;
        registro.NrRG = funcionario.NrRG;
        registro.DsSexo = funcionario.DsSexo;
        registro.NrTelefone = funcionario.NrTelefone;
        registro.DsEmail = funcionario.DsEmail;
        registro.DsEndereco = funcionario.DsEndereco;
        registro.DsBairro = funcionario.DsBairro;
        registro.NrNumero = funcionario.NrNumero;
        registro.DsComplemento = funcionario.DsComplemento;
        registro.IdCidade = funcionario.IdCidade;
        registro.NrCEP = funcionario.NrCEP;
        registro.DtNascimento = funcionario.DtNascimento;
        registro.DtAdmissao = funcionario.DtAdmissao;
        registro.DtDemissao = funcionario.DtDemissao;
        registro.IdCargo = funcionario.IdCargo;
        registro.VlSalario = funcionario.VlSalario;
        registro.VlComissao = funcionario.VlComissao;
        registro.DsObservacao = funcionario.DsObservacao;
        registro.FlAtivo = funcionario.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    // Mascaras removidas (o banco guarda so numeros) e textos no tamanho da coluna.
    private static void Normalizar(Funcionario f)
    {
        f.NrCpf = Module.SomenteNumeros(f.NrCpf) ?? string.Empty;
        f.NrTelefone = Module.SomenteNumeros(f.NrTelefone);
        f.NrCEP = Module.SomenteNumeros(f.NrCEP);
        f.NrRG = Module.SomenteNumeros(f.NrRG);

        f.NmFuncionario = Module.Truncar(f.NmFuncionario?.Trim(), 200) ?? string.Empty;
        f.NmApelido = Module.Truncar(f.NmApelido?.Trim(), 100);
        f.DsSexo = Module.Truncar(f.DsSexo, 30);
        f.DsEmail = Module.Truncar(f.DsEmail?.Trim(), 150);
        f.DsEndereco = Module.Truncar(f.DsEndereco, 200);
        f.DsBairro = Module.Truncar(f.DsBairro, 100);
        f.NrNumero = Module.Truncar(f.NrNumero, 10);
        f.DsComplemento = Module.Truncar(f.DsComplemento, 100);
        f.NrTelefone = Module.Truncar(f.NrTelefone, 20);
        f.NrCEP = Module.Truncar(f.NrCEP, 10);
        f.DsObservacao = Module.Truncar(f.DsObservacao, 500);
    }

    private async Task Validar(Funcionario f)
    {
        if (string.IsNullOrWhiteSpace(f.NmFuncionario))
            throw new ValidacaoException("Informe o nome do funcionário.");

        if (string.IsNullOrEmpty(f.NrCpf))
            throw new ValidacaoException("Informe o CPF do funcionário.");
        if (!Module.CpfValido(f.NrCpf))
            throw new ValidacaoException("CPF inválido. Verifique os números informados.");

        if (f.NrRG?.Length > 9)
            throw new ValidacaoException("RG inválido. Informe no máximo 9 dígitos.");

        if (!string.IsNullOrWhiteSpace(f.DsEmail) && !new EmailAddressAttribute().IsValid(f.DsEmail))
            throw new ValidacaoException("E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).");

        if (f.IdCargo <= 0 || !await _db.Cargos.AnyAsync(c => c.IdCargo == f.IdCargo))
            throw new ValidacaoException("Selecione o cargo do funcionário.");

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (f.DtAdmissao == default)
            throw new ValidacaoException("Informe a data de admissão.");
        if (f.DtDemissao != null && f.DtDemissao < f.DtAdmissao)
            throw new ValidacaoException("A data de demissão não pode ser anterior à admissão.");
        if (f.DtNascimento != null && f.DtNascimento > hoje)
            throw new ValidacaoException("A data de nascimento não pode ser futura.");

        if (f.VlSalario < 0)
            throw new ValidacaoException("O salário não pode ser negativo.");
        if (f.VlComissao is < 0 or > 999.99m)
            throw new ValidacaoException("Comissão inválida.", "Informe um percentual entre 0 e 999,99.");

        // CPF unico (respeita a constraint UQ_tbGerFuncionarios_nrCpf)
        var mesmoCpf = await _db.Funcionarios
            .Where(x => x.NrCpf == f.NrCpf && x.IdFuncionario != f.IdFuncionario)
            .Select(x => x.NmFuncionario)
            .FirstOrDefaultAsync();
        if (mesmoCpf != null)
            throw new ValidacaoException($"Já existe um funcionário com este CPF: {mesmoCpf}.");
    }

    public async Task Excluir(int id)
    {
        var funcionario = await _db.Funcionarios.FindAsync(id);
        if (funcionario == null)
            return;

        const string dica = "Inative o funcionário em vez de excluí-lo.";

        // Filhas: tbOpeAgendamentos.idFuncionario e tbVenVendas.idFuncionario.
        if (await _db.Agendamentos.AnyAsync(a => a.IdFuncionario == id))
            throw new ValidacaoException("Não é possível excluir: existe agendamento para este funcionário.", dica);
        if (await _db.Vendas.AnyAsync(v => v.IdFuncionario == id))
            throw new ValidacaoException("Não é possível excluir: existe venda registrada por este funcionário.", dica);

        try
        {
            _db.Funcionarios.Remove(funcionario);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este funcionário.", dica);
        }
    }
}
