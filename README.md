# Chamados

Sistema de gerenciamento de chamados desenvolvido em .NET 10 (ASP.NET Core Razor Pages, Minimal API e Blazor).

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
| `/atendimento` | `InteractiveServer` | Fila interativa de atendimento em duas colunas (*Abertos* e *Em atendimento*), com busca em tempo real e transições sem recarregar a página |
| `/modos` | Estático + ilha `InteractiveServer` | Comparação lado a lado do componente `Contador` em modo estático e interativo no servidor |

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

## Como executar e testar

```powershell
dotnet test
dotnet run --project src/Chamados.Web
```
