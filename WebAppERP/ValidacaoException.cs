namespace WebAppERP;

// Regra de negocio nao atendida (campo obrigatorio, duplicidade, registro em
// uso...). As classes Business lancam; as telas capturam e mostram a mensagem.
// Erro = o que aconteceu; Dica = como resolver (opcional).
public class ValidacaoException : Exception
{
    public string Erro { get; }
    public string? Dica { get; }

    public ValidacaoException(string erro, string? dica = null) : base(erro)
    {
        Erro = erro;
        Dica = dica;
    }

    // Texto para exibir na tela: o erro e, quando houver, a dica.
    public string MensagemCompleta => string.IsNullOrEmpty(Dica) ? Erro : $"{Erro} {Dica}";
}
