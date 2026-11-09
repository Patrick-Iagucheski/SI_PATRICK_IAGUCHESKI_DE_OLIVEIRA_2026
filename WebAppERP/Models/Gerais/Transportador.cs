using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Transportadora: mesmo layout de campos do fornecedor, sem dados bancarios, PIX
// e condicao de pagamento
// (Database/Migracoes/1794068000000_Alter_Table_tbGerTransportadores_Padrao_Fornecedor.sql).
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerTransportadores")]
public class Transportador : EntidadeComRegistro
{
    [Key]
    [Column("idTransportador")]
    public int IdTransportador { get; set; }

    // ===== Dados Cadastrais =====

    [Column("tpPessoa")]
    [Required, MaxLength(1)]
    public string TpPessoa { get; set; } = "J";

    [Column("dsRazaoSocial")]
    [Required, MaxLength(100)]
    public string DsRazaoSocial { get; set; } = string.Empty;

    [Column("dsNomeFantasia")]
    [MaxLength(150)]
    public string? DsNomeFantasia { get; set; }

    [Column("nrCpf")]
    [MaxLength(11)]
    public string? NrCpf { get; set; }

    [Column("nrCNPJ")]
    [MaxLength(14)]
    public string? NrCNPJ { get; set; }

    // Documento internacional (Estrangeiro) ou documento livre (Outro)
    [Column("nrDocumento")]
    [MaxLength(30)]
    public string? NrDocumento { get; set; }

    [Column("nrInscEstadual")]
    [MaxLength(13)]
    public string? NrInscEstadual { get; set; }

    [Column("nrInscMunicipal")]
    [MaxLength(15)]
    public string? NrInscMunicipal { get; set; }

    [Column("nrRG")]
    [MaxLength(9)]
    public string? NrRG { get; set; }

    [Column("dtNascimento")]
    public DateOnly? DtNascimento { get; set; }

    // ===== Endereco =====

    [Column("nrCEP")]
    [MaxLength(8)]
    public string? NrCEP { get; set; }

    [Column("dsEndereco")]
    [MaxLength(100)]
    public string? DsEndereco { get; set; }

    [Column("nrNumero")]
    [MaxLength(10)]
    public string? NrNumero { get; set; }

    [Column("dsComplemento")]
    [MaxLength(100)]
    public string? DsComplemento { get; set; }

    [Column("dsBairro")]
    [MaxLength(100)]
    public string? DsBairro { get; set; }

    [Column("idCidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public Cidade? Cidade { get; set; }

    // ===== Contato =====

    [Column("nrTelefoneFixo")]
    [MaxLength(11)]
    public string? NrTelefoneFixo { get; set; }

    [Column("nrCelular")]
    [MaxLength(11)]
    public string? NrCelular { get; set; }

    [Column("dsEmail")]
    [MaxLength(150)]
    [ManterCaixa]
    public string? DsEmail { get; set; }

    [Column("dsSite")]
    [MaxLength(150)]
    [ManterCaixa]
    public string? DsSite { get; set; }

    // ===== Status e observacoes =====

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }
}
