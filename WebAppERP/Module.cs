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

    // ============================================================
    // Consultas por nome
    // ============================================================

    // Comparacao de nomes sem diferenciar maiusculas nem acentos ("sao jose"
    // = "São José"). O banco usa SQL_Latin1_General_CP1_CI_AS (sensivel a
    // acento), por isso a collation e forcada so na comparacao:
    // EF.Functions.Collate(coluna, Module.CollationSemAcento).
    public const string CollationSemAcento = "SQL_Latin1_General_CP1_CI_AI";

    // Monta o padrao %termo% para LIKE, escapando os curingas digitados.
    public static string MontarLikeContem(string termo) =>
        "%" + termo.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";

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
}
