using WebAppERP.DTOs.Vendas;

namespace WebAppERP.ViewModels;

public class VendaEditorViewModel
{
    public Venda Venda { get; set; } = new() { TpStatus = Venda.ABERTA, DtVenda = DateTime.Now };
    public List<VendaItem> Itens { get; set; } = [];
    public VendaCombosDTO Combos { get; set; } = new();

    // Venda cancelada nao pode ser editada (regra de negocio).
    public bool SomenteLeitura => Venda.TpStatus == Venda.CANCELADA;
}
