using WebAppERP.DTOs;
using WebAppERP.Models;

namespace WebAppERP.Repositories;

public interface ICompraRepository
{
    Task<List<OpeCompra>> ListarAsync();
    Task<OpeCompra?> ObterCompletaAsync(CompraChaveDto chave);
    Task<List<FinContaPagar>> ObterContasGeradasAsync(CompraChaveDto chave);
    Task<CompraCombosDto> ObterCombosAsync();

    // Grava a compra. TpStatus RASCUNHO nao gera Contas a Pagar nem estoque;
    // FINALIZADA grava tudo de forma atomica (compra + itens + parcelas +
    // contas a pagar + estoque).
    Task<(bool ok, string? erro, CompraChaveDto chave)> SalvarAsync(CompraInputDto input);

    Task<(bool ok, string? erro)> CancelarAsync(CompraChaveDto chave);
    Task<(bool ok, string? erro)> ExcluirAsync(CompraChaveDto chave);
}
