using WebAppERP.DTOs;
using WebAppERP.Models;

namespace WebAppERP.ViewModels;

public class VendaEditorViewModel
{
    public OpeVenda Venda { get; set; } = new() { TpStatus = "ABERTA", DtVenda = DateTime.Now };
    public List<OpeVendaItem> Itens { get; set; } = [];
    public VendaCombosDto Combos { get; set; } = new();

    // Venda cancelada nao pode ser editada (regra de negocio).
    public bool SomenteLeitura => Venda.TpStatus == "CANCELADA";
}
