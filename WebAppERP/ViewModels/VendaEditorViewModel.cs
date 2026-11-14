using WebAppERP.DTOs.Vendas;

namespace WebAppERP.ViewModels;

public class VendaEditorViewModel
{
    public Venda Venda { get; set; } = new() { TpStatus = Venda.ABERTA, DtVenda = DateTime.Now };
    public List<VendaItem> Itens { get; set; } = [];
    public VendaCombosDTO Combos { get; set; } = new();
    // Agendamento da venda: entra no campo mesmo fora da lista (outro dia ou cancelado).
    public Agendamento? Agendamento { get; set; }

    // So a venda aberta e editada. Finalizada nao muda: para corrigir, cancela-se
    // e lanca-se uma nova (regra de negocio, VendaBusiness.Salvar).
    public bool SomenteLeitura => Venda.TpStatus is Venda.FINALIZADA or Venda.CANCELADA;

    public string? MensagemSomenteLeitura => Venda.TpStatus switch
    {
        Venda.FINALIZADA => "Esta venda está FINALIZADA: apenas visualização. Para corrigir, cancele a venda e lance uma nova.",
        Venda.CANCELADA => "Esta venda está CANCELADA: apenas visualização.",
        _ => null
    };
}
