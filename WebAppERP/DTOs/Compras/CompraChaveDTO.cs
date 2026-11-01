namespace WebAppERP.DTOs.Compras;

// Chave composta da compra: Numero + Serie + Modelo + Fornecedor.
// Sempre trafega inteira; nunca so o numero da nota.
public class CompraChaveDTO
{
    public int NrNota { get; set; }
    public int NrSerie { get; set; }
    public int NrModelo { get; set; }
    public int IdFornecedor { get; set; }

    public bool Preenchida => NrNota > 0 && NrModelo > 0 && IdFornecedor > 0;

    public override string ToString() => $"{NrModelo}/{NrSerie}/{NrNota}/{IdFornecedor}";
}
