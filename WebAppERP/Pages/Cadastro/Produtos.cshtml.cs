using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class ProdutosModel : PageModel
{
    private readonly AppDbContext _db;

    public ProdutosModel(AppDbContext db) => _db = db;

    public List<EstProduto> Produtos { get; set; } = [];

    [BindProperty]
    public EstProduto ProdutoForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Produtos = await _db.Produtos.OrderBy(p => p.DsProduto).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ProdutoForm.IdProduto == 0)
        {
            _db.Produtos.Add(ProdutoForm);
        }
        else
        {
            var existing = await _db.Produtos.FindAsync(ProdutoForm.IdProduto);
            if (existing != null)
            {
                existing.DsProduto = ProdutoForm.DsProduto;
                existing.IdNCMSH = ProdutoForm.IdNCMSH;
                existing.SgUnidade = ProdutoForm.SgUnidade;
                existing.VlPesoBruto = ProdutoForm.VlPesoBruto;
                existing.VlPesoLiquido = ProdutoForm.VlPesoLiquido;
                existing.QtSaldo = ProdutoForm.QtSaldo;
                existing.VlCustoMedio = ProdutoForm.VlCustoMedio;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto != null)
        {
            _db.Produtos.Remove(produto);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
