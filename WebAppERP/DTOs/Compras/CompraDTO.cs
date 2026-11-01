namespace WebAppERP.DTOs.Compras;

// Payload completo da compra (cabecalho + itens + parcelas).
public class CompraDTO
{
    // Chave composta
    public int NrNota { get; set; }
    public int NrSerie { get; set; }
    public int NrModelo { get; set; }
    public int IdFornecedor { get; set; }

    // Chave original, quando a tela esta editando uma compra ja gravada.
    // Permite detectar tentativa de alterar a propria chave.
    public CompraChaveDTO? ChaveOriginal { get; set; }

    public DateOnly? DtEmissao { get; set; }
    public DateOnly? DtChegada { get; set; }
    public int? IdTransportador { get; set; }
    public int? IdCondicaoPagamento { get; set; }

    public decimal VlFrete { get; set; }
    public decimal VlSeguro { get; set; }
    public decimal VlOutrasDespesas { get; set; }

    // RASCUNHO (nao gera financeiro/estoque) | FINALIZADA
    public string TpStatus { get; set; } = Compra.RASCUNHO;
    public string? DsObservacao { get; set; }

    public List<CompraItemDTO> Itens { get; set; } = [];
    public List<CompraPagamentoDTO> Pagamentos { get; set; } = [];

    public CompraChaveDTO GetChave() => new()
    {
        NrNota = NrNota,
        NrSerie = NrSerie,
        NrModelo = NrModelo,
        IdFornecedor = IdFornecedor
    };
}
