using WebAppERP.Models;

namespace WebAppERP.DTOs;

// Chave composta da compra: Numero + Serie + Modelo + Fornecedor.
// Sempre trafega inteira; nunca so o numero da nota.
public class CompraChaveDto
{
    public int NrNota { get; set; }
    public int NrSerie { get; set; }
    public int NrModelo { get; set; }
    public int IdFornecedor { get; set; }

    public bool Preenchida => NrNota > 0 && NrModelo > 0 && IdFornecedor > 0;

    public override string ToString() => $"{NrModelo}/{NrSerie}/{NrNota}/{IdFornecedor}";
}

// Item enviado pela tela. Os totais sao recalculados no servidor por seguranca.
public class CompraItemDto
{
    public string TpItem { get; set; } = "PRODUTO"; // PRODUTO | SERVICO
    public int? IdProduto { get; set; }
    public int? IdServico { get; set; }
    public int? IdClassificacaoConta { get; set; }
    public string? SgUnidade { get; set; }
    public decimal QtItem { get; set; } = 1;
    public decimal VlUnitario { get; set; }
    public decimal VlDesconto { get; set; }
}

// Parcela enviada pela tela.
public class CompraPagamentoDto
{
    public int? IdCondicaoPagamento { get; set; }
    public int IdFormaPagamento { get; set; }
    public decimal VlParcela { get; set; }
    public DateOnly? DtVencimento { get; set; }
}

// Payload completo da compra (cabecalho + itens + parcelas).
public class CompraInputDto
{
    // Chave composta
    public int NrNota { get; set; }
    public int NrSerie { get; set; }
    public int NrModelo { get; set; }
    public int IdFornecedor { get; set; }

    // Chave original, quando a tela esta editando uma compra ja gravada.
    // Permite detectar tentativa de alterar a propria chave.
    public CompraChaveDto? ChaveOriginal { get; set; }

    public DateOnly? DtEmissao { get; set; }
    public DateOnly? DtChegada { get; set; }
    public int? IdTransportador { get; set; }
    public int? IdCondicaoPagamento { get; set; }

    public decimal VlFrete { get; set; }
    public decimal VlSeguro { get; set; }
    public decimal VlOutrasDespesas { get; set; }

    // RASCUNHO (nao gera financeiro/estoque) | FINALIZADA
    public string TpStatus { get; set; } = "RASCUNHO";
    public string? DsObservacao { get; set; }

    public List<CompraItemDto> Itens { get; set; } = [];
    public List<CompraPagamentoDto> Pagamentos { get; set; } = [];

    public CompraChaveDto Chave() => new()
    {
        NrNota = NrNota,
        NrSerie = NrSerie,
        NrModelo = NrModelo,
        IdFornecedor = IdFornecedor
    };
}

// Listas usadas para popular os combos da tela de compra.
public class CompraCombosDto
{
    public List<GerFornecedor> Fornecedores { get; set; } = [];
    public List<GerTransportador> Transportadores { get; set; } = [];
    public List<EstProduto> Produtos { get; set; } = [];
    public List<GerServico> Servicos { get; set; } = [];
    public List<FinClassificacaoConta> Classificacoes { get; set; } = [];
    public List<FinCondicaoPagamento> Condicoes { get; set; } = [];
    public List<FinFormaPagamento> Formas { get; set; } = [];
}
