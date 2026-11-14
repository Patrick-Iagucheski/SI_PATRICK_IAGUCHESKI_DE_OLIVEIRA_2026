# WebAppERP — R10 Barber Shop

Sistema ERP web para a gestão de uma barbearia: cadastros, agenda de atendimentos,
compras, vendas, estoque e financeiro, tudo integrado em um só lugar.

Projeto acadêmico desenvolvido na disciplina de **Prática Profissional** do curso de
**Sistemas de Informação** (2026), por **Patrick Iagucheski de Oliveira**.

---

## Sobre o projeto

A R10 Barber Shop presta serviços de barbearia (corte, barba e cuidados masculinos) e
revende produtos do segmento. Antes do sistema, não havia uma ferramenta que integrasse
clientes, agenda, fornecedores, produtos, estoque, compras, vendas e contas a pagar e a
receber. O WebAppERP reúne essas rotinas em uma única aplicação web.

## Funcionalidades

| Módulo | O que tem |
|---|---|
| **Cadastros** | Clientes (pessoa física, jurídica e estrangeiro), funcionários, cargos, serviços, categorias de serviço, fornecedores e transportadoras |
| **Localidades** | Países, estados e cidades |
| **Estoque** | Produtos, marcas, categorias de produto, unidades de medida, movimentação de estoque e balancete |
| **Financeiro** | Formas e condições de pagamento, contas a pagar e contas a receber |
| **Operacional** | Agendamentos, vendas e compras |
| **Principal** | Painel com indicadores do dia e atividades recentes do sistema |

### Regras de negócio em destaque

- **Compra** finalizada dá entrada no estoque e recalcula o **custo médio ponderado**
  (com desconto e rateio de frete, seguro e outros gastos) e gera as parcelas no
  contas a pagar. O cancelamento estorna o estoque e os títulos.
- **Venda** finalizada baixa o estoque, gera os títulos no contas a receber pela
  condição de pagamento e conclui o agendamento vinculado. Venda aberta é salva
  automaticamente a cada alteração.
- **Estoque** só entra pela compra e só sai pela venda; cada movimento fica registrado
  com o saldo e o custo médio do momento.
- **Validações** de CPF, CNPJ, Inscrição Estadual (por estado, com dígito verificador
  em PR, SC, RS e SP), Inscrição Municipal, CEP, telefone e datas.
- **Registro** de quem incluiu e quem alterou cada cadastro, com data e hora.
- Interface com **abas internas** (várias telas abertas ao mesmo tempo) e pesquisa
  com cadastro rápido em cada campo ligado a outro cadastro.

## Tecnologias

- **ASP.NET Core Razor Pages** (.NET 10)
- **Entity Framework Core** 10 com **SQL Server** (Express)
- **Bootstrap 5** e **Bootstrap Icons**
- **JavaScript** puro (sem framework de front-end)

## Arquitetura

O projeto é organizado por módulo, com as regras de negócio separadas das telas:

```
WebAppERP/
├── Business/          Regras de negócio por módulo (Compras, Estoque, Financeiro, Gerais, ...)
├── Data/              AppDbContext (Entity Framework Core)
├── DTOs/              Objetos de entrada das telas para as regras
├── Infrastructure/    Serviços de apoio (arquivos, binder de decimais, ...)
├── Models/            Entidades mapeadas para as tabelas (tb{Módulo}{Entidades})
├── Pages/             Telas Razor (.cshtml + .cshtml.cs + .cshtml.js por tela)
├── ViewComponents/    Componentes reaproveitados (cadastros rápidos, campo de cidade)
├── wwwroot/           CSS, JavaScript comum (module.js, site.js) e bibliotecas
├── Module.cs          Métodos compartilhados (validações, formatações)
└── Program.cs         Configuração da aplicação
```

- As regras ficam nas classes **Business**; uma regra não atendida gera uma
  `ValidacaoException`, e a tela mostra a mensagem ao usuário.
- As telas apenas recebem os dados e chamam a regra correspondente.
- Tabelas seguem o padrão `tb{Módulo}{Entidades}` (ex.: `tbGerClientes`,
  `tbVenVendas`, `tbEstMovimentacoes`).

## Como executar

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (Express ou superior)

### Passos

1. **Banco de dados:** crie o banco `SoftwareErp` e rode os scripts da pasta
   `WebAppERP/Database/Migracoes`, em ordem pelo número no início do nome do arquivo.
2. **Conexão:** configure o servidor em `WebAppERP/Servers/servers.json`
   (ou a `DefaultConnection` do `appsettings.json`).
3. **Pastas de dados** (opcional): em `appsettings.json`, `Arquivos:PastaUploads` define
   onde ficam as imagens enviadas e `ProtecaoDados:PastaChaves` onde ficam as chaves de
   proteção dos formulários.
4. **Executar:**

   ```bash
   cd WebAppERP
   dotnet run --launch-profile http
   ```

5. Acesse **http://localhost:5206**.


---

Projeto acadêmico — Sistemas de Informação, 2026.
