using System.ComponentModel.DataAnnotations.Schema;
using WebAppERP.Models.Seguranca;

namespace WebAppERP.Models;

// Base dos cadastros e movimentos com o registro de quem incluiu e de quem
// alterou por ultimo (colunas criadas em
// Database/Migracoes/1793632885204_Alter_Table_Colunas_Usuario_Criacao_Edicao.sql).
// Os valores sao preenchidos sozinhos ao gravar (AppDbContext.SaveChanges),
// com o usuario atual (UsuarioAtual); as telas so exibem (Module.Registro).
public abstract class EntidadeComRegistro
{
    [Column("idUsuario")]
    public int? IdUsuario { get; set; }

    [ForeignKey(nameof(IdUsuario))]
    public Usuario? UsuarioInclusao { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [ForeignKey(nameof(IdUsuarioEdicao))]
    public Usuario? UsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
