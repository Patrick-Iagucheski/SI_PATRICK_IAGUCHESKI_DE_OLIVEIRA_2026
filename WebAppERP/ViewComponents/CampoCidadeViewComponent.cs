using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Gerais;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Componente unico de cidade, usado por todas as telas com endereco
// (Clientes, Funcionarios, Transportadores e o componente de Fornecedor).
// Traz tudo junto: o campo so de selecao (lupa abre a pesquisa), o cadastro rapido
// de cidade/estado/pais (com as pesquisas) e o script.
// View: Pages/Shared/Components/CampoCidade/Default.cshtml.
// Script: Default.cshtml.js, na mesma pasta da view. Handlers: Pages/Gerais/Localidades.
public class CampoCidadeViewComponent : ComponenteBase
{
    private readonly CidadeBusiness _cidade;

    public CampoCidadeViewComponent(CidadeBusiness cidade) => _cidade = cidade;

    public async Task<IViewComponentResult> InvokeAsync(string nome = "", string id = "", int? idCidade = null, bool somenteCadastros = false)
    {
        var vm = new CampoCidadeViewModel { Nome = nome, Id = id, IdCidade = idCidade, SomenteCadastros = somenteCadastros };

        if (idCidade is int idc)
            vm.NomeCidade = await _cidade.GetNomeComUf(idc);

        // Modais e script so na primeira vez que o componente aparece na pagina.
        vm.IncluirModais = PrimeiraVezNaPagina();

        return View(vm);
    }
}
