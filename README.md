# Chamados

Sistema de gerenciamento de chamados desenvolvido em .NET 10 (ASP.NET Core Razor Pages, Minimal API e Blazor). Estado: Módulo 7.

## Páginas Razor Pages

| Rota | Descrição |
| --- | --- |
| `/` | Painel com estatísticas e cartões por status |
| `/Chamados` | Lista filtrável, ordenável e paginada |
| `/Chamados/Novo` | Formulário de abertura de chamado |
| `/Chamados/Detalhes/{id}` | Detalhes, transições de status e histórico |
| `/Chamados/Editar/{id}` | Edição de dados com controle de concorrência otimista |
| `/Erro/{codigo?}` | Página amigável de erro (404, 400, 500) |

## Páginas Blazor

| Rota | Modo de renderização | Descrição |
| --- | --- | --- |
| `/atendimento` | `InteractiveServer` | Fila interativa de atendimento em duas colunas (*Abertos* e *Em atendimento*), com busca com cancelamento, abrir e editar (EditForm), iniciar, resolver e cancelar sem recarregar a página |
| `/modos` | Estático + ilha `InteractiveServer` | Comparação lado a lado do componente `Contador` em modo estático e interativo no servidor |

Componentes do Módulo 6: `Formularios/FormularioChamado` (EditForm com Data Annotations, validação personalizada e erros do servidor), `Layout/ResumoFila` (totais no cabeçalho, alimentados pelo serviço `Estado/EstadoFila`) e o módulo JavaScript `wwwroot/js/preferencias.js` (lembra o responsável no `localStorage`).

## API JSON

| Método e Rota | Descrição |
| --- | --- |
| `GET /api/chamados` | Lista chamados (suporta `status`, `prioridadeMinima`, `texto`, `ordem`) |
| `GET /api/chamados/estatisticas` | Estatísticas agregadas |
| `GET /api/chamados/{id}` | Consulta um chamado por Id |
| `POST /api/chamados` | Abre um novo chamado |
| `PUT /api/chamados/{id}` | Edita um chamado com verificação de concorrência (`versaoEsperada`) |
| `POST /api/chamados/{id}/atendimento` | Inicia o atendimento de um chamado |

## Decisões de Arquitetura (Módulo 5)

- **16. Blazor no mesmo projeto Web:** Razor Pages ficam com os formulários de cadastro; a fila, que muda o tempo todo, vira componente interativo no servidor.
- **17. Interatividade por página, não global:** só `/atendimento` declara `@rendermode InteractiveServer`; o resto continua estático, sem circuito.
- **18. Componente pede, página executa:** `CartaoChamado` avisa por `EventCallback<PedidoTransicao>`; só `Atendimento.razor` chama o serviço e trata exceções.

## Decisões de Arquitetura (Módulo 6)

- **19. `ChamadoInput` também no Blazor:** o `EditForm` usa o mesmo modelo e as mesmas Data Annotations das Razor Pages; a regra nova `[TextoSignificativo]` vale nas duas telas.
- **20. Erros do servidor no `EditContext`:** concorrência e regras do domínio entram por um `ValidationMessageStore` e aparecem no `ValidationSummary`; o envio usa `OnSubmit` com `Validate()` manual.
- **21. Estado compartilhado em serviço Scoped com evento:** `EstadoFila` liga a página de atendimento ao resumo do cabeçalho; quem se inscreve no evento cancela a inscrição no `Dispose`.
- **22. JavaScript só no `OnAfterRenderAsync`, em módulo ES isolado:** o `localStorage` guarda só preferência de tela, nunca dado sensível.
- **23. Fila sem busca duplicada e com cancelamento:** `[PersistentState]` leva a lista da pré-renderização para o circuito; cada busca nova cancela a anterior com `CancellationTokenSource`.

## Decisões de Arquitetura (Módulo 7)

- **24. EF Core com SQLite num projeto próprio:** `Chamados.Infra` implementa `IChamadoRepositorio`; o domínio continua sem referência a EF. A única concessão é um construtor privado sem parâmetros no `Chamado`, para o EF materializar objetos lidos do banco.
- **25. Mapeamento só por Fluent API**, numa classe `IEntityTypeConfiguration` por entidade; o histórico é entidade *owned* (tabela `HistoricoChamados`), carregada e gravada sempre com o chamado.
- **26. Consultas no banco, não em memória:** o repositório recebe o filtro e devolve resumos projetados; `Where`, `ORDER BY`, `LIMIT/OFFSET` e `GROUP BY` viram SQL. O repositório em memória mantém a mesma regra para os testes do domínio.
- **27. Um DbContext por operação, criado por `IDbContextFactory`:** no Blazor Server, um contexto por circuito seria usado por dois componentes ao mesmo tempo, o que o EF não permite.
- **28. Concorrência no banco:** `Versao` é token de concorrência; o UPDATE leva `WHERE Versao = <lida>` e a falha vira `ConcorrenciaException`.
- **29. DateTimeOffset gravado como número** (`DateTimeOffsetToBinaryConverter`): o SQLite não ordena nem compara datas com fuso guardadas como texto. O custo é a precisão: frações abaixo do milissegundo se perdem na gravação.
- **30. API com DTOs, validação automática e OpenAPI:** a entidade nunca é serializada; `AddValidation()` confere as Data Annotations do corpo; o documento OpenAPI é publicado só em Development.
- **31. Toda mudança de esquema por migration:** nada de `EnsureCreated` nem de editar o banco à mão. Se o modelo muda sem migration, o `MigrateAsync` recusa iniciar (`PendingModelChangesWarning`) e um teste de integração (`HasPendingModelChanges`) acusa antes.

## Banco de dados (Módulo 7)

Os chamados ficam num arquivo SQLite, `src/Chamados.Web/chamados.db` (string de conexão `ConnectionStrings:Chamados`). Em Development, a aplicação aplica as migrations pendentes ao iniciar e, se o banco estiver vazio, grava 12 chamados de exemplo. Para recomeçar do zero, pare a aplicação e apague `chamados.db`, `chamados.db-shm` e `chamados.db-wal`. O arquivo não vai para o Git (`.gitignore`).

Migrations no Visual Studio (**Ferramentas › Gerenciador de Pacotes do NuGet › Console do Gerenciador de Pacotes**, com o projeto padrão `Chamados.Infra` e `Chamados.Web` como projeto de inicialização):

```powershell
Add-Migration NomeDaMudanca
Update-Database
```

Pela CLI (ferramenta `dotnet-ef`):

```powershell
dotnet ef migrations add NomeDaMudanca --project src/Chamados.Infra --startup-project src/Chamados.Web
dotnet ef database update --project src/Chamados.Infra --startup-project src/Chamados.Web
```

O projeto traz duas migrations: `Inicial` (tabelas `Chamados` e `HistoricoChamados`) e `IndicePrioridade` (um índice criado depois, para mostrar o banco evoluindo sem perder dados). Em Development, o log mostra cada comando SQL (categoria `Microsoft.EntityFrameworkCore.Database.Command`), com os valores dos parâmetros ocultos.

## Configuração (só em Development)

| Chave | Valor | Para quê |
| --- | --- | --- |
| `Simulacao:LatenciaMs` | 300 | Atrasa a listagem para ver o indicador de carga e o cancelamento das buscas |
| `Simulacao:FalhaNaListagemACada` | 0 (desligada) | Faz uma listagem a cada N falhar de propósito, para testar o tratamento de erro |

Valor fora da faixa impede a aplicação de iniciar (`ValidateOnStart`). O perfil **https-falha** do `launchSettings.json` liga a falha a cada 3 listagens sem editar arquivo:

```powershell
dotnet run --project src/Chamados.Web --launch-profile https-falha
```

## Como executar e testar

```powershell
dotnet test
dotnet run --project src/Chamados.Web
```
