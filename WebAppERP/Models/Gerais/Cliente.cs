using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerClientes")]
public class Cliente : EntidadeComRegistro
{
    [Key]
    [Column("idCliente")]
    public int IdCliente { get; set; }

    [Nome]
    [Column("nmCliente")]
    [Required, MaxLength(100)]
    public string NmCliente { get; set; } = string.Empty;

    // Tipo de pessoa: F = Fisica (CPF), J = Juridica (CNPJ), E = Estrangeiro/Outro
    [Column("tpPessoa")]
    [Required, MaxLength(1)]
    public string TpPessoa { get; set; } = "F";

    [Column("nrCpf")]
    [MaxLength(11)]
    public string? NrCpf { get; set; }

    [Column("nrCnpj")]
    [MaxLength(14)]
    public string? NrCnpj { get; set; }

    // Documento generico para estrangeiro / outro tipo (sem validacao de CPF/CNPJ)
    [Column("nrDocumento")]
    [MaxLength(20)]
    public string? NrDocumento { get; set; }

    [Column("nrRG")]
    [MaxLength(12)]
    public string? NrRG { get; set; }

    // Pessoa juridica: nome fantasia e inscricoes (mesmos tamanhos do fornecedor).
    [Nome]
    [Column("dsNomeFantasia")]
    [MaxLength(150)]
    public string? DsNomeFantasia { get; set; }

    [Column("nrInscEstadual")]
    [MaxLength(13)]
    public string? NrInscEstadual { get; set; }

    [Column("nrInscMunicipal")]
    [MaxLength(15)]
    public string? NrInscMunicipal { get; set; }

    // Contato (mesmo padrao do fornecedor): fixo, celular/WhatsApp e site.
    [Column("nrTelefoneFixo")]
    [MaxLength(11)]
    public string? NrTelefoneFixo { get; set; }

    [Column("nrCelular")]
    [MaxLength(11)]
    public string? NrCelular { get; set; }

    [Column("dsSite")]
    [MaxLength(150)]
    [ManterCaixa]
    public string? DsSite { get; set; }

    [Column("dsEmail")]
    [MaxLength(100)]
    [ManterCaixa]
    public string? DsEmail { get; set; }

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
    [MaxLength(60)]
    public string? DsBairro { get; set; }

    [Column("idCidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public Cidade? Cidade { get; set; }

    [Column("nrCEP")]
    [MaxLength(8)]
    public string? NrCEP { get; set; }

    [Column("dtNascimento")]
    public DateOnly? DtNascimento { get; set; }

    [Column("dsSexo")]
    [MaxLength(1)]
    public string? DsSexo { get; set; }

    // Condicao de pagamento padrao do cliente (opcional), como no fornecedor.
    // Coluna criada em Database/Migracoes/1794061500000_Alter_Table_tbGerClientes_Condicao_Pagamento.sql.
    [Column("idCondicaoPagamento")]
    public int? IdCondicaoPagamento { get; set; }

    [ForeignKey("IdCondicaoPagamento")]
    public CondicaoPagamento? CondicaoPagamento { get; set; }

    [MultiLinha]
    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
