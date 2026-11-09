namespace WebAppERP.Models;

// Cadastro gravado em MAIUSCULO: todo texto da entidade e convertido ao gravar
// (AppDbContext.ConverterCaixaAlta), menos as propriedades marcadas com
// [ManterCaixa]. Os dados antigos foram convertidos em
// Database/Migracoes/1794070000000_Update_Cadastros_Caixa_Alta.sql.
// Na tela, os campos de texto dos modais aparecem em maiusculo (site.css).
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class CaixaAltaAttribute : Attribute { }

// Texto que fica como foi digitado num cadastro [CaixaAlta]: e-mail, site,
// caminho de arquivo e chave PIX (chave aleatoria diferencia maiusculas).
[AttributeUsage(AttributeTargets.Property)]
public sealed class ManterCaixaAttribute : Attribute { }
