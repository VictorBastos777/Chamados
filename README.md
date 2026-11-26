# Chamados

Sistema de gerenciamento de chamados desenvolvido em .NET 10 (ASP.NET Core Razor Pages, Minimal API e Blazor). Estado: Módulo 6.

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

## Configuração (só em Development)

| Chave | Valor | Para quê |
| --- | --- | --- |
| `Simulacao:LatenciaMs` | 600 | Atrasa a listagem para ver o indicador de carga e o cancelamento das buscas |
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
