namespace WebAppERP.DTOs.Compras;

// Chave composta da compra: Numero + Serie + Modelo + Fornecedor.
// Sempre trafega inteira; nunca so o numero da nota.
// Numero e serie sao texto (varchar) so com digitos, como foram digitados: o
// zero a esquerda faz parte da chave ("099001" e "99001" sao chaves diferentes),
// regra definida pelo professor.
public class CompraChaveDTO
{
    private string _nrNota = string.Empty;
    private string _nrSerie = string.Empty;

    public string NrNota { get => _nrNota; set => _nrNota = Normalizar(value); }
    public string NrSerie { get => _nrSerie; set => _nrSerie = Normalizar(value); }
    public int NrModelo { get; set; }
    public int IdFornecedor { get; set; }

    public bool Preenchida => NrNota != string.Empty && NrModelo > 0 && IdFornecedor > 0;

    public override string ToString() => $"{NrModelo}/{NrSerie}/{NrNota}/{IdFornecedor}";

    // So os digitos, mantendo os zeros a esquerda (vazio continua vazio).
    public static string Normalizar(string? valor) =>
        new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}
