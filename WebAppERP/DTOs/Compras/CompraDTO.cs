namespace WebAppERP.DTOs.Compras;

// Payload completo da compra (cabecalho + itens + parcelas).
public class CompraDTO
{
    // Chave composta
    private string _nrNota = string.Empty;
    private string _nrSerie = string.Empty;

    public string NrNota { get => _nrNota; set => _nrNota = CompraChaveDTO.Normalizar(value); }
    public string NrSerie { get => _nrSerie; set => _nrSerie = CompraChaveDTO.Normalizar(value); }
    public int NrModelo { get; set; }
    public int IdFornecedor { get; set; }

    // Chave original, quando a tela esta editando uma compra ja gravada.
    // Permite detectar tentativa de alterar a propria chave.
    public CompraChaveDTO? ChaveOriginal { get; set; }

    // Versao da compra que a tela abriu (DtCriacao em ticks; 0 numa inclusao).
    // Se a compra foi excluida e a mesma chave lancada de novo em outra tela, a
    // versao muda e a tela antiga nao grava por cima da compra nova.
    public long VersaoOriginal { get; set; }

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
