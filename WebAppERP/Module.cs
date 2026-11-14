using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP;

// Metodos compartilhados por todo o sistema (telas e regras de negocio).
// Tudo que serviria a mais de uma tela fica aqui, em vez de ser copiado em
// cada uma: chamar como Module.CpfValido(...), Module.Truncar(...) etc.
public static class Module
{
    // ============================================================
    // Textos
    // ============================================================

    // Corta o valor no tamanho maximo da coluna, para nao dar erro no banco.
    public static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    // Tira a mascara (pontos, tracos, barras...) e deixa so os digitos.
    public static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    // CEP digitado com ou sem mascara: 8 digitos. Vazio: null. Letra, simbolo
    // ou quantidade errada de digitos lanca ValidacaoException.
    public static string? CepValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var digitos = SomenteNumeros(valor)!;
        if (valor.Any(ch => !char.IsDigit(ch) && !"-. ".Contains(ch)) || digitos.Length != 8)
            throw new ValidacaoException("CEP inválido.", "Informe os 8 números do CEP (exemplo: 85800-000).");
        return digitos;
    }

    // Telefone com DDD, com ou sem mascara: 10 digitos (fixo) ou 11 (celular).
    // Vazio: null. rotulo: nome do campo na mensagem ("Telefone", "Celular").
    public static string? TelefoneValido(string? valor, string rotulo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var digitos = SomenteNumeros(valor)!;
        if (valor.Any(ch => !char.IsDigit(ch) && !"()-. ".Contains(ch)) || digitos.Length is not (10 or 11))
            throw new ValidacaoException($"{rotulo} inválido.", "Informe o DDD e o número: 10 dígitos para fixo ou 11 para celular.");
        return digitos;
    }

    // Data de nascimento: nao pode ser futura nem anterior a 1900.
    public static void ValidarNascimento(DateOnly? data)
    {
        if (data == null) return;
        if (data > DateOnly.FromDateTime(DateTime.Today))
            throw new ValidacaoException("A data de nascimento não pode ser futura.");
        if (data < new DateOnly(1900, 1, 1))
            throw new ValidacaoException("Data de nascimento inválida.", "Informe uma data a partir de 01/01/1900.");
    }

    // Datas aceitas nos titulos (Contas a Receber e a Pagar): fora disso e erro de digitacao.
    public static readonly DateOnly DataTituloMinima = new(2000, 1, 1);
    public static readonly DateOnly DataTituloMaxima = new(2100, 12, 31);

    // Datas do titulo: todas entre 2000 e 2100; emissao nao futura; vencimento
    // a partir da emissao; a baixa (recebimento ou pagamento) entre a emissao e hoje.
    // nomeBaixa: "recebimento" ou "pagamento", para a mensagem.
    public static void ValidarDatasTitulo(DateOnly? emissao, DateOnly? vencimento, DateOnly? baixa, string nomeBaixa)
    {
        var faixa = $"Informe datas entre {DataTituloMinima:dd/MM/yyyy} e {DataTituloMaxima:dd/MM/yyyy}.";
        foreach (var (data, nome) in new[] { (emissao, "emissão"), (vencimento, "vencimento"), (baixa, nomeBaixa) })
            if (data != null && (data < DataTituloMinima || data > DataTituloMaxima))
                throw new ValidacaoException($"Data de {nome} inválida.", faixa);

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (emissao > hoje)
            throw new ValidacaoException("A data de emissão não pode ser futura.");
        if (emissao != null && vencimento != null && vencimento < emissao)
            throw new ValidacaoException("O vencimento não pode ser anterior à emissão.");
        if (baixa > hoje)
            throw new ValidacaoException($"A data de {nomeBaixa} não pode ser futura.");
        if (emissao != null && baixa != null && baixa < emissao)
            throw new ValidacaoException($"A data de {nomeBaixa} não pode ser anterior à emissão.");
    }

    // Valor com mais casas decimais do que a coluna grava (o banco arredondaria sem avisar).
    public static bool PassaDasCasas(decimal valor, int casas) => valor != Math.Round(valor, casas);

    // Arruma o texto digitado antes de gravar: tab, quebra de linha e outros
    // caracteres de controle viram espaco (no texto de varias linhas a quebra
    // fica), espacos repetidos viram um so e as pontas sao cortadas.
    public static string LimparTexto(string valor, bool multiLinha)
    {
        var sb = new System.Text.StringBuilder(valor.Length);
        foreach (var ch in valor.Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            if (multiLinha && ch == '\n') { sb.Append('\n'); continue; }
            var c = char.IsControl(ch) ? ' ' : ch;
            // linha unica: espacos seguidos viram um so
            if (!multiLinha && c == ' ' && sb.Length > 0 && sb[^1] == ' ') continue;
            sb.Append(c);
        }
        if (!multiLinha) return sb.ToString().Trim();
        // varias linhas: corta os espacos do fim de cada linha e as linhas vazias das pontas
        var linhas = sb.ToString().Split('\n').Select(l => l.TrimEnd());
        return string.Join("\n", linhas).Trim('\n', ' ');
    }

    // Caracteres que o banco (varchar, pagina de codigo 1252) consegue guardar:
    // letras com acento, cedilha, simbolos comuns. Emoji, letras de outros
    // alfabetos (chines, arabe...) e simbolos especiais virariam "?" na gravacao.
    private const string ExtrasCp1252 = "€‚ƒ„…†‡ˆ‰Š‹ŒŽ‘’“”•–—˜™š›œžŸ";

    // Primeiro trecho que nao pode ser gravado (o emoji inteiro), ou null se esta tudo certo.
    public static string? CaractereNaoAceito(string valor)
    {
        for (var i = 0; i < valor.Length; i++)
        {
            var ch = valor[i];
            if (ch <= '\u007F' || (ch >= ' ' && ch <= 'ÿ') || ExtrasCp1252.Contains(ch)) continue;
            return char.IsHighSurrogate(ch) && i + 1 < valor.Length ? valor.Substring(i, 2) : ch.ToString();
        }
        return null;
    }

    // ============================================================
    // Formatacao para exibicao (mesmas mascaras do module.js)
    // ============================================================
    // Valor fora do tamanho esperado volta como veio ("" quando vazio).

    public static string FormatarCpf(string? v)
    {
        var d = SomenteNumeros(v) ?? "";
        return d.Length == 11 ? $"{d[..3]}.{d.Substring(3, 3)}.{d.Substring(6, 3)}-{d.Substring(9, 2)}" : (v ?? "");
    }

    public static string FormatarCnpj(string? v)
    {
        var d = SomenteNumeros(v) ?? "";
        return d.Length == 14 ? $"{d[..2]}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}" : (v ?? "");
    }

    // Documento mostrado abaixo do nome na lista dos campos de cliente e de
    // fornecedor (nomes parecidos ou iguais): "CPF ...", "CNPJ ..." ou "Doc. ..."
    // (estrangeiro ou outro). null sem documento. Mesma regra do documentoPessoa (module.js).
    public static string? DocumentoPessoa(string? tpPessoa, string? cpf, string? cnpj, string? documento)
    {
        if (tpPessoa == "F" && !string.IsNullOrEmpty(cpf)) return "CPF " + FormatarCpf(cpf);
        if (tpPessoa == "J" && !string.IsNullOrEmpty(cnpj)) return "CNPJ " + FormatarCnpj(cnpj);
        if (!string.IsNullOrWhiteSpace(documento)) return "Doc. " + documento.Trim();
        return null;
    }

    public static string? DocumentoPessoa(Models.Gerais.Cliente c) => DocumentoPessoa(c.TpPessoa, c.NrCpf, c.NrCnpj, c.NrDocumento);
    public static string? DocumentoPessoa(Models.Gerais.Fornecedor f) => DocumentoPessoa(f.TpPessoa, f.NrCpf, f.NrCNPJ, f.NrDocumento);

    // Fixo (10 digitos) ou celular (11 digitos).
    public static string FormatarTelefone(string? v)
    {
        var d = SomenteNumeros(v) ?? "";
        if (d.Length == 11) return $"({d[..2]}) {d.Substring(2, 5)}-{d.Substring(7, 4)}";
        if (d.Length == 10) return $"({d[..2]}) {d.Substring(2, 4)}-{d.Substring(6, 4)}";
        return v ?? "";
    }

    // Cidade com a UF do estado (ex.: "Cascavel/PR"); null sem cidade.
    public static string? CidadeUf(Models.Gerais.Cidade? c) =>
        c == null ? null : c.NmCidade + "/" + c.Estado?.SgUF;

    // Registro de quem incluiu e de quem alterou por ultimo, no formato do
    // bloco "Registro" das telas (_Registro.cshtml / mostrarRegistro no module.js).
    public static object Registro(Models.EntidadeComRegistro e) => new
    {
        incluidoPor = e.UsuarioInclusao?.NmUsuario,
        incluidoEm = e.DtCriacao.ToString("dd/MM/yyyy HH:mm"),
        alteradoPor = e.UsuarioEdicao?.NmUsuario,
        alteradoEm = e.DtEdicao?.ToString("dd/MM/yyyy HH:mm")
    };

    // O mesmo registro em JSON, para o atributo data-registro dos botoes Editar.
    public static string RegistroJson(Models.EntidadeComRegistro e) =>
        System.Text.Json.JsonSerializer.Serialize(Registro(e));

    // Traco nas listagens quando nao ha valor.
    public static string OuTraco(string? v) => string.IsNullOrWhiteSpace(v) ? "-" : v;

    // ============================================================
    // Consultas por nome
    // ============================================================

    // Comparacao de nomes sem diferenciar maiusculas nem acentos ("sao jose"
    // = "São José"). O banco usa SQL_Latin1_General_CP1_CI_AS (sensivel a
    // acento), por isso a collation e forcada so na comparacao:
    // EF.Functions.Collate(coluna, Module.CollationSemAcento).
    public const string CollationSemAcento = "SQL_Latin1_General_CP1_CI_AI";

    // Monta o padrao %termo% para LIKE, escapando os curingas digitados. O termo
    // e limitado a 100 caracteres: texto gigante colado na busca nao estoura o
    // tamanho do parametro no SQL Server.
    public static string MontarLikeContem(string termo) =>
        "%" + Truncar(termo.Trim(), 100)!.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";

    // Filtro "contem o termo" das buscas por texto, sem diferenciar maiusculas
    // nem acentos ("parana" acha "Paraná"), em um ou mais campos (basta um
    // conter). Vira LIKE no banco com o termo como parametro e os curingas
    // escapados, entao o texto digitado nunca entra como SQL.
    // Uso: query.Where(Module.MontarFiltroTexto<Cliente>(termo, c => c.NmCliente, c => c.DsEmail))
    public static Expression<Func<T, bool>> MontarFiltroTexto<T>(string termo, params Expression<Func<T, string?>>[] campos)
    {
        var filtro = new { padrao = MontarLikeContem(termo) };   // membro de objeto: o EF envia como parametro
        var padrao = Expression.Property(Expression.Constant(filtro), nameof(filtro.padrao));
        var funcoes = Expression.Property(null, typeof(EF), nameof(EF.Functions));
        var like = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like),
            [typeof(DbFunctions), typeof(string), typeof(string)])!;
        var collate = typeof(RelationalDbFunctionsExtensions).GetMethod(nameof(RelationalDbFunctionsExtensions.Collate))!
            .MakeGenericMethod(typeof(string));

        var x = Expression.Parameter(typeof(T), "x");
        // Emoji e letras de outros alfabetos nao sao gravados (TratarTextos), entao
        // nenhum registro os contem. Sem isso o banco ignora o emoji na comparacao
        // e a busca traria todos os registros.
        if (CaractereNaoAceito(termo) != null)
            return Expression.Lambda<Func<T, bool>>(Expression.Constant(false), x);

        Expression? corpo = null;
        foreach (var campo in campos)
        {
            var valor = new TrocarParametro(campo.Parameters[0], x).Visit(campo.Body);
            var semAcento = Expression.Call(collate, funcoes, valor, Expression.Constant(CollationSemAcento));
            var contem = Expression.Call(like, funcoes, semAcento, padrao);
            corpo = corpo == null ? contem : Expression.OrElse(corpo, contem);
        }
        return Expression.Lambda<Func<T, bool>>(corpo ?? Expression.Constant(true), x);
    }

    // Troca o parametro de cada campo (c => ...) pelo parametro unico do filtro.
    private sealed class TrocarParametro(ParameterExpression de, ParameterExpression para) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == de ? para : base.VisitParameter(node);
    }

    // ============================================================
    // Validacoes
    // ============================================================

    // CPF pelos digitos verificadores (aceita com ou sem mascara).
    public static bool CpfValido(string? cpf)
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

    // CNPJ pelos digitos verificadores (aceita com ou sem mascara).
    public static bool CnpjValido(string? cnpj)
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

    // Valor informado e negativo (campos opcionais vazios nao contam).
    public static bool EhNegativo(decimal? valor) => valor.HasValue && valor.Value < 0;

    // ============================================================
    // Itens de Compra e Venda
    // ============================================================

    // Validacao comum do item de compra e de venda: produto ou servico escolhido,
    // quantidade e preco maiores que zero, desconto nao negativo. seq: numero do item, para a
    // mensagem dizer qual linha esta errada. O total de cada um fica no seu
    // Business ou em CalcularTotalItem (a compra usa desconto por unidade).
    public static void ValidarItem(int seq, bool produto, int? idProduto, int? idServico,
                                   decimal quantidade, decimal vlUnitario, decimal vlDesconto)
    {
        if (produto && idProduto is not > 0)
            throw new ValidacaoException($"Item {seq}: selecione o produto.");
        if (!produto && idServico is not > 0)
            throw new ValidacaoException($"Item {seq}: selecione o serviço.");

        if (quantidade <= 0)
            throw new ValidacaoException($"Item {seq}: a quantidade deve ser maior que zero.");
        if (vlUnitario <= 0)
            throw new ValidacaoException($"Item {seq}: informe o preço unitário (maior que zero).");
        if (vlDesconto < 0)
            throw new ValidacaoException($"Item {seq}: o desconto não pode ser negativo.");
    }

    // Item da venda: valida (ValidarItem) e devolve o total, com o desconto
    // sobre o item inteiro: Quantidade x Preco Unitario - Desconto. O desconto
    // nao pode zerar o item (item de graca nao e venda).
    public static decimal CalcularTotalItem(int seq, bool produto, int? idProduto, int? idServico,
                                            decimal quantidade, decimal vlUnitario, decimal vlDesconto)
    {
        ValidarItem(seq, produto, idProduto, idServico, quantidade, vlUnitario, vlDesconto);

        var bruto = Math.Round(quantidade * vlUnitario, 2, MidpointRounding.AwayFromZero);
        var total = bruto - vlDesconto;
        if (total <= 0)
            throw new ValidacaoException($"Item {seq}: o desconto (R$ {vlDesconto:N2}) não pode ser igual ou maior que "
                                       + $"o valor do item (R$ {bruto:N2}).");
        return total;
    }
}
