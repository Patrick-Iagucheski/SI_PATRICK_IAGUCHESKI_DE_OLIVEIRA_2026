using WebAppERP.DTOs;
using WebAppERP.Models;

namespace WebAppERP.Repositories;

public interface IVendaRepository
{
    Task<List<OpeVenda>> ListarAsync();
    Task<OpeVenda?> ObterComItensAsync(int id);
    Task<VendaCombosDto> ObterCombosAsync();
    Task<(bool ok, string? erro, int idVenda)> SalvarAsync(VendaInputDto input);
    Task<(bool ok, string? erro)> CancelarAsync(int id);
    Task ExcluirAsync(int id);
}
