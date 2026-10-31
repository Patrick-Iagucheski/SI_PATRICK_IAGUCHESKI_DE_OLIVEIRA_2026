using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class ClientesModel : PageModel
{
    private readonly AppDbContext _db;

    public ClientesModel(AppDbContext db) => _db = db;

    public List<GerCliente> Clientes { get; set; } = [];

    [BindProperty]
    public GerCliente ClienteForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Clientes = await _db.Clientes
            .Include(c => c.Cidade)
            .ThenInclude(c => c!.Estado)
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCliente)
            .ToListAsync();
    }

    private static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    // Trunca o valor ao tamanho maximo da coluna para evitar erro de banco.
    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    public async Task<IActionResult> OnPostAsync()
    {
        // Normaliza o tipo de pessoa (F = Fisica, J = Juridica, E = Estrangeiro/Outro)
        var tipo = (ClienteForm.TpPessoa ?? "F").Trim().ToUpper();
        if (tipo != "F" && tipo != "J" && tipo != "E") tipo = "F";
        ClienteForm.TpPessoa = tipo;

        // Remove as mascaras antes de salvar (banco guarda so os numeros)
        ClienteForm.NrCpf = SomenteNumeros(ClienteForm.NrCpf);
        ClienteForm.NrCnpj = SomenteNumeros(ClienteForm.NrCnpj);
        ClienteForm.NrTelefone = SomenteNumeros(ClienteForm.NrTelefone);
        ClienteForm.NrCEP = SomenteNumeros(ClienteForm.NrCEP);
        ClienteForm.NrRG = SomenteNumeros(ClienteForm.NrRG);

        // Mantem preenchido apenas o documento referente ao tipo selecionado.
        switch (tipo)
        {
            case "F":
                ClienteForm.NrCnpj = null;
                ClienteForm.NrDocumento = null;
                break;
            case "J":
                ClienteForm.NrCpf = null;
                ClienteForm.NrDocumento = null;
                break;
            default: // E
                ClienteForm.NrCpf = null;
                ClienteForm.NrCnpj = null;
                break;
        }

        // Truncamento defensivo de acordo com o tamanho de cada coluna.
        ClienteForm.NmCliente = Truncar(ClienteForm.NmCliente, 200) ?? string.Empty;
        ClienteForm.NrCpf = Truncar(ClienteForm.NrCpf, 11);
        ClienteForm.NrCnpj = Truncar(ClienteForm.NrCnpj, 14);
        ClienteForm.NrDocumento = Truncar(ClienteForm.NrDocumento, 20);
        ClienteForm.NrRG = Truncar(ClienteForm.NrRG, 12);
        ClienteForm.NrTelefone = Truncar(ClienteForm.NrTelefone, 11);
        ClienteForm.DsEmail = Truncar(ClienteForm.DsEmail, 150);
        ClienteForm.DsEndereco = Truncar(ClienteForm.DsEndereco, 200);
        ClienteForm.NrNumero = Truncar(ClienteForm.NrNumero, 10);
        ClienteForm.DsComplemento = Truncar(ClienteForm.DsComplemento, 100);
        ClienteForm.DsBairro = Truncar(ClienteForm.DsBairro, 100);
        ClienteForm.NrCEP = Truncar(ClienteForm.NrCEP, 8);
        ClienteForm.DsObservacao = Truncar(ClienteForm.DsObservacao, 500);

        // ===== Validacoes de servidor (mensagens amigaveis) =====
        if (string.IsNullOrWhiteSpace(ClienteForm.NmCliente))
            return await ComErroAsync("Informe o nome do cliente.");

        if (tipo == "F" && !string.IsNullOrEmpty(ClienteForm.NrCpf) && !CpfValido(ClienteForm.NrCpf))
            return await ComErroAsync("CPF inválido. Verifique os números informados.");

        if (tipo == "J" && !string.IsNullOrEmpty(ClienteForm.NrCnpj) && !CnpjValido(ClienteForm.NrCnpj))
            return await ComErroAsync("CNPJ inválido. Verifique os números informados.");

        if (!string.IsNullOrWhiteSpace(ClienteForm.DsEmail) &&
            !new EmailAddressAttribute().IsValid(ClienteForm.DsEmail))
            return await ComErroAsync("E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).");

        if (ClienteForm.IdCliente == 0)
        {
            ClienteForm.DtCriacao = DateTime.Now;
            _db.Clientes.Add(ClienteForm);
        }
        else
        {
            var existing = await _db.Clientes.FindAsync(ClienteForm.IdCliente);
            if (existing != null)
            {
                existing.NmCliente = ClienteForm.NmCliente;
                existing.TpPessoa = ClienteForm.TpPessoa;
                existing.NrCpf = ClienteForm.NrCpf;
                existing.NrCnpj = ClienteForm.NrCnpj;
                existing.NrDocumento = ClienteForm.NrDocumento;
                existing.NrRG = ClienteForm.NrRG;
                existing.NrTelefone = ClienteForm.NrTelefone;
                existing.DsEmail = ClienteForm.DsEmail;
                existing.DsEndereco = ClienteForm.DsEndereco;
                existing.NrNumero = ClienteForm.NrNumero;
                existing.DsComplemento = ClienteForm.DsComplemento;
                existing.DsBairro = ClienteForm.DsBairro;
                existing.IdCidade = ClienteForm.IdCidade;
                existing.NrCEP = ClienteForm.NrCEP;
                existing.DtNascimento = ClienteForm.DtNascimento;
                existing.DsSexo = ClienteForm.DsSexo;
                existing.DsObservacao = ClienteForm.DsObservacao;
                existing.FlAtivo = ClienteForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    // Recarrega a pagina mantendo a mensagem de erro amigavel.
    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        TempData["ErroCliente"] = mensagem;
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente == null)
            return RedirectToPage();

        // Filhas: tbOpeAgendamentos.idCliente e tbOpeVendas.idCliente.
        if (await _db.Agendamentos.AnyAsync(a => a.IdCliente == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe agendamento para este cliente. Inative-o.";
            return RedirectToPage();
        }

        if (await _db.Vendas.AnyAsync(v => v.IdCliente == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe venda para este cliente. Inative-o.";
            return RedirectToPage();
        }

        try
        {
            _db.Clientes.Remove(cliente);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Cliente excluído com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a este cliente. Inative-o.";
        }
        return RedirectToPage();
    }

    // Busca de clientes via AJAX (filtros da pagina)
    // Pesquisa por: codigo, nome, documento (CPF/CNPJ/outro) ou e-mail

    public async Task<IActionResult> OnGetBuscarClientesAsync(
        int? id, string? nome, string? documento, string? email, bool incluirInativos = false)
    {
        var query = _db.Clientes.Include(c => c.Cidade).ThenInclude(c => c!.Estado).AsQueryable();

        // Checkbox desmarcada => apenas ativos. Marcada => apenas inativos.
        if (incluirInativos)
            query = query.Where(c => !c.FlAtivo);
        else
            query = query.Where(c => c.FlAtivo);

        if (id.HasValue)
            query = query.Where(c => c.IdCliente == id.Value);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var n = nome.Trim();
            query = query.Where(c => c.NmCliente.Contains(n));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var digitos = new string(documento.Where(char.IsDigit).ToArray());
            if (digitos != "")
                query = query.Where(c =>
                    (c.NrCpf != null && c.NrCpf.Contains(digitos)) ||
                    (c.NrCnpj != null && c.NrCnpj.Contains(digitos)) ||
                    (c.NrDocumento != null && c.NrDocumento.Contains(digitos)));
            else
            {
                var d = documento.Trim();
                query = query.Where(c => c.NrDocumento != null && c.NrDocumento.Contains(d));
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var e = email.Trim();
            query = query.Where(c => c.DsEmail != null && c.DsEmail.Contains(e));
        }

        var lista = await query
            .OrderBy(c => c.NmCliente)
            .Take(100)
            .Select(c => new
            {
                id = c.IdCliente,
                nome = c.NmCliente,
                tipoPessoa = c.TpPessoa,
                cpf = c.NrCpf,
                cnpj = c.NrCnpj,
                documento = c.NrDocumento,
                rg = c.NrRG,
                tel = c.NrTelefone,
                email = c.DsEmail,
                end = c.DsEndereco,
                numero = c.NrNumero,
                complemento = c.DsComplemento,
                bairro = c.DsBairro,
                idCidade = c.IdCidade,
                cidadeNome = c.Cidade != null ? c.Cidade.NmCidade + "/" + (c.Cidade.Estado != null ? c.Cidade.Estado.SgUF : "") : null,
                cep = c.NrCEP,
                nasc = c.DtNascimento,
                sexo = c.DsSexo,
                obs = c.DsObservacao,
                ativo = c.FlAtivo
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

        int Soma(int qtd)
        {
            int s = 0;
            for (int i = 0; i < qtd; i++) s += (d[i] - '0') * (qtd + 1 - i);
            return s;
        }
        int Dig(int soma)
        {
            int r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }

        return Dig(Soma(9)) == d[9] - '0' && Dig(Soma(10)) == d[10] - '0';
    }

    private static bool CnpjValido(string? cnpj)
    {
        var d = new string((cnpj ?? "").Where(char.IsDigit).ToArray());
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;

        int[] p1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] p2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int Dig(int qtd, int[] pesos)
        {
            int s = 0;
            for (int i = 0; i < qtd; i++) s += (d[i] - '0') * pesos[i];
            int r = s % 11;
            return r < 2 ? 0 : 11 - r;
        }

        return Dig(12, p1) == d[12] - '0' && Dig(13, p2) == d[13] - '0';
    }
}
