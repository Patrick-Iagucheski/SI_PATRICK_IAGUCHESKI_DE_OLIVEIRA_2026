using WebAppERP.DTOs.Compras;

namespace WebAppERP.ViewModels;

public class CompraEditorViewModel
{
    public Compra Compra { get; set; } = new();
    public List<CompraItem> Itens { get; set; } = [];
    public List<CompraPagamento> Pagamentos { get; set; } = [];
    public CompraCombosDTO Combos { get; set; } = new();

    // Titulos ja gerados no Contas a Pagar por esta compra.
    public List<ContaAPagar> ContasGeradas { get; set; } = [];

    // True quando a tela esta editando uma compra ja gravada.
    public bool Edicao { get; set; }

    // So rascunho e editavel: finalizada ou cancelada abre so para consulta.
    public bool SomenteLeitura => Compra.TpStatus is Compra.FINALIZADA or Compra.CANCELADA;

    public string MotivoBloqueio => Compra.TpStatus switch
    {
        Compra.FINALIZADA => "Esta compra está FINALIZADA: apenas visualização. Para corrigir, cancele a compra e lance uma nova.",
        Compra.CANCELADA => "Esta compra está CANCELADA: apenas visualização.",
        _ => string.Empty
    };
}
