# Chamados

Sistema de gerenciamento de chamados desenvolvido em .NET 10 (ASP.NET Core Razor Pages, Minimal API e Blazor). Estado: Módulo 8.

## Primeira execução: senha dos usuários de exemplo (Módulo 8)

A aplicação exige login. Em Development ela cria três usuários de exemplo, mas a senha deles **não está no repositório**: vem dos Segredos do Usuário, guardados no seu perfil, fora da pasta do projeto. Sem ela, a aplicação para ao iniciar com uma mensagem explicando o que fazer.

No Visual Studio: botão direito em `Chamados.Web` › **Gerenciar Segredos do Usuário** e grave:

```json
{ "Semente:SenhaInicial": "escolha-uma-senha-forte" }
```

Pela CLI:

```powershell
dotnet user-secrets set "Semente:SenhaInicial" "escolha-uma-senha-forte" --project src/Chamados.Web
```

A senha precisa ter 10 caracteres ou mais, com maiúscula, minúscula, número e símbolo (regras do Identity no `Program.cs`).

| Usuário | Papel ou claim | Pode |
| --- | --- | --- |
| `ana@chamados.local` | papel `Atendente` | tudo o que um usuário conectado faz, mais a fila de atendimento, editar e mudar status |
| `bruno@chamados.local` | nenhum | abrir chamados, ver a lista e os detalhes |
| `carla@chamados.local` | claim `cargo = Coordenacao` | o que o bruno faz, mais os indicadores |

Os usuários são criados uma vez; para trocar a senha depois, apague `chamados.db` e execute de novo.

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
| `GET /api/chamados/por-responsavel?nome=` | Chamados de um responsável (Módulo 8; SQL escrito à mão com `FromSql`, parametrizado) |
| `POST /api/conta/token` | Troca e-mail e senha por um token de acesso (Módulo 8; corpo `{"email": "...", "senha": "..."}`) |

Desde o Módulo 8, toda a API exige autenticação, pelo cookie do navegador ou pelo cabeçalho `Authorization: Bearer <token>`. Sem nenhum dos dois: 401. Conectado sem permissão: 403. Iniciar atendimento e editar exigem a política `Atender`; estatísticas, a política `VerIndicadores`. O arquivo `Chamados.Web.http` pede o token primeiro; escolha o ambiente **dev**, cujo arquivo `http-client.env.json` lê a senha dos Segredos do Usuário.

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

## Decisões de Arquitetura (Módulo 8)

- **32. ASP.NET Core Identity com EF Core, no mesmo banco:** usuários, papéis e claims nas tabelas `AspNet*`, criadas pela migration `Identidade`. Login próprio, em Razor Pages, sem a interface pronta do Identity: menos código para ler e nenhum cadastro aberto.
- **33. Política de fallback:** tudo exige login, e o que é público declara `[AllowAnonymous]`. Esquecer de proteger uma página nova não a deixa aberta.
- **34. Papel para quem atende, claim para quem coordena:** `Atender` exige o papel `Atendente`; `VerIndicadores`, a claim `cargo = Coordenacao`. Mostrar ou esconder links é conforto; quem protege são as páginas, os handlers e os endpoints.
- **35. Cookie para o navegador, token para outros clientes:** a API aceita os dois esquemas (`Politicas.Api`) e responde 401/403, nunca redireciona. O token sai de `POST /api/conta/token`, não de `MapIdentityApi`, para não publicar cadastro aberto.
- **36. Nenhum segredo no repositório:** a senha dos usuários de exemplo vem dos Segredos do Usuário; sem ela a aplicação não inicia. O arquivo `.http` lê a mesma chave pelo `http-client.env.json`.
- **37. SQL à mão só com parâmetros:** `FromSql` com string interpolada vira parâmetro; `FromSqlRaw` com texto montado é bloqueado na compilação (`EF1002`/`EF1003` como erro).
- **38. Observabilidade sem pacotes extras:** logs com `[LoggerMessage]`, correlação por `TraceId` e métricas com `System.Diagnostics.Metrics`. Exportar para um painel (OpenTelemetry) fica para quando houver onde guardar os dados.
- **39. Teste de componente com `HtmlRenderer`**, que vem no ASP.NET Core: confere o HTML de cada estado. Interação (clique, digitação) pede a biblioteca bUnit, da comunidade, não usada aqui.

## Segurança e observabilidade (Módulo 8)

- **Login**: `/Conta/Entrar` (ASP.NET Core Identity, senha guardada só como hash, bloqueio de 5 minutos após 5 erros seguidos). Logout por POST em `/Conta/Sair`.
- **Autorização**: política de fallback exige login em tudo; exceções explícitas com `[AllowAnonymous]` (login e páginas de erro). Políticas em `Seguranca/Politicas.cs`.
- **Cabeçalhos**: `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options` e `Referrer-Policy` em todas as respostas (`Seguranca/CabecalhosSeguranca.cs`).
- **Logs estruturados**: eventos com Id e campos nomeados em `Diagnostico/Registro.cs`. O perfil **https-json** mostra o log em JSON, com `TraceId` em cada linha; o mesmo `traceId` volta nas respostas de erro (Problem Details).
- **Métricas**: medidor `Chamados` com `chamados.abertos` (por prioridade) e `chamados.transicoes` (por status). Para ver ao vivo: `dotnet tool install --global dotnet-counters` e `dotnet-counters monitor --name Chamados.Web --counters Chamados,Microsoft.AspNetCore.Hosting`.
- **Análise estática**: `Directory.Build.props` liga `AnalysisLevel=latest-recommended` em todos os projetos e transforma em erro os avisos de injeção de SQL (`EF1002`, `EF1003`) e de comando SQL montado com texto (`CA2100`).
- **Dependências**: a restauração audita os pacotes, inclusive os transitivos (`NuGetAuditMode=all`). Conferência manual: `dotnet package list --vulnerable --include-transitive` e `dotnet package list --outdated`. O `.github/dependabot.yml` pede atualizações semanais.

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

São 70 testes: 43 do domínio, 9 de integração com o banco e 18 da aplicação web (integração com `WebApplicationFactory`, componentes com `HtmlRenderer` e unidade das políticas de autorização).
