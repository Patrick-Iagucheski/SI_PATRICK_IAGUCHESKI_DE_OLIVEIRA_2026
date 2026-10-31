using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Componente unico de cidade, usado por todas as telas com endereco
// (Clientes, Funcionarios, Transportadores e o componente de Fornecedor).
// Traz tudo junto: o campo com lista suspensa (droplist), o cadastro rapido
// de cidade/estado/pais (com busca no topo) e o script.
// View: Pages/Shared/Components/CampoCidade/Default.cshtml.
// Script: wwwroot/js/cadastro-localidade.js. Handlers: Pages/Cadastro/Localidades.
public class CampoCidadeViewComponent : ViewComponent
{
    private const string ChaveModaisIncluidos = "CampoCidade.ModaisIncluidos";
    private readonly AppDbContext _db;

    public CampoCidadeViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(string nome, string id, int? idCidade = null)
    {
        var vm = new CampoCidadeViewModel { Nome = nome, Id = id, IdCidade = idCidade };

        if (idCidade is int idc)
            vm.NomeCidade = await _db.Cidades
                .Where(c => c.IdCidade == idc)
                .Select(c => c.NmCidade + (c.Estado != null ? " / " + c.Estado.SgUF : ""))
                .FirstOrDefaultAsync();

        // Modais e script so na primeira chamada da requisicao.
        if (!HttpContext.Items.ContainsKey(ChaveModaisIncluidos))
        {
            HttpContext.Items[ChaveModaisIncluidos] = true;
            vm.IncluirModais = true;
        }

        return View(vm);
    }
}
