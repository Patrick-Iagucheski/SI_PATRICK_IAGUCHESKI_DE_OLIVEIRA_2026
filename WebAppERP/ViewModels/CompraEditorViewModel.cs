using WebAppERP.DTOs;
using WebAppERP.Models;

namespace WebAppERP.ViewModels;

public class CompraEditorViewModel
{
    public OpeCompra Compra { get; set; } = new();
    public List<OpeCompraItem> Itens { get; set; } = [];
    public List<OpeCompraPagamento> Pagamentos { get; set; } = [];
    public CompraCombosDto Combos { get; set; } = new();

    // Titulos ja gerados no Contas a Pagar por esta compra.
    public List<FinContaPagar> ContasGeradas { get; set; } = [];

    // True quando a tela esta editando uma compra ja gravada.
    public bool Edicao { get; set; }

    // Compra cancelada nao pode ser editada.
    public bool Cancelada => Compra.TpStatus == "CANCELADA";

    // Compra finalizada com titulo ja movimentado (pago/baixado) fica travada.
    public bool TemTituloMovimentado =>
        ContasGeradas.Any(c => c.DsStatus == "PAGO" || c.VlPago > 0);

    public bool SomenteLeitura => Cancelada || TemTituloMovimentado;

    public string MotivoBloqueio =>
        Cancelada ? "Esta compra está CANCELADA e não pode ser editada."
        : TemTituloMovimentado ? "Esta compra possui título já pago/baixado no Contas a Pagar e não pode ser alterada."
        : string.Empty;
}
