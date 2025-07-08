# Quake Log Parser

Uma API RESTful construída em .NET 8 para analisar logs do servidor Quake 3 Arena e fornecer relatórios detalhados de cada jogo.

## 📋 Visão Geral

O **Quake Log Parser** é uma solução para processar arquivos de log gerados pelo servidor Quake 3 Arena, extraindo informações e estatísticas estruturadas sobre jogos, jogadores, mortes. A aplicação segue os princípios de Clean Architecture.

### Funcionalidades

- ✅ **Parser de Logs**: Análise completa do arquivo `games.log`
- ✅ **API RESTful**: Endpoints para consulta de jogos e estatisticas
- ✅ **Relatórios Detalhados**: Informações sobre kills, players e total de mortes
- ✅ **Busca por Jogo**: Consulta específica por nome do jogo
- ✅ **Documentação Swagger**: Interface interativa para teste da API
- ✅ **Testes Abrangentes**: Cobertura com testes unitários e de integração

## 🚀 Quick Start

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- IDE: Visual Studio 2022, Rider ou VS Code
- Git

### Instalação

1. **Clone o repositório:**
```bash
git clone https://github.com/JefMacedo/QuakeLogParser.git
cd QuakeLogParser
```

2. **Restaure as dependências:**
```bash
dotnet restore
```

3. **Execute a aplicação:**
```bash
dotnet run --project src/QuakeLogParser.API
```

4. **Acesse a API:**
- Swagger UI: `https://localhost:7125/swagger`
- API Base URL: `https://localhost:7125/api`

## Estrutura do Projeto

```
QuakeLogParser/
├── src/
│   ├── QuakeLogParser.API/          # Camada de apresentação (Controllers, Program.cs)
│   ├── QuakeLogParser.Application/  # Camada de aplicação (DTOs, Interfaces)
│   ├── QuakeLogParser.Domain/       # Camada de domínio (Entities)
│   └── QuakeLogParser.Infrastructure/ # Camada de infraestrutura (Services)
├── tests/
│   └── QuakeLogParser.Tests/        # Testes unitários e de integração
├── GameLog/
│   └── games.log                    # Arquivo de log do Quake 3 Arena
└── QuakeLogParser.sln               # Solution file
```

### Arquitetura em Camadas

- **API**: Controllers e configurações da aplicação web
- **Application**: DTOs, interfaces e lógica de aplicação
- **Domain**: Entidades do domínio e regras de negócio
- **Infrastructure**: Implementações de serviços e acesso a dados

## Endpoints da API

### `GET /api/LogGame/games`
Retorna todos os jogos processados do arquivo de log.

**Resposta:**
```json
[
  {
    "name": "game_1",
    "totalKills": 45,
    "players": ["Dono da bola", "Isgalamido", "Zeh"],
    "kills": {
      "Dono da bola": 5,
      "Isgalamido": 18,
      "Zeh": 20
    }
  },
  {...}
]
```

### `GET /api/LogGame/games/{name}`
Retorna um jogo específico pelo nome.

**Parâmetros:**
- `name` (string): Nome do jogo (ex: "game_1")

**Resposta:**
```json
{
  "name": "game_1",
  "totalKills": 45,
  "players": ["Dono da bola", "Isgalamido"],
  "kills": {
    "Dono da bola": 5,
    "Isgalamido": 18
  }
}
```

## Executando os Testes

### Todos os testes:
```bash
dotnet test
```

## Regras de Negócio

### Processamento de Logs

1. **Identificação de Jogos**: Cada linha com `InitGame` inicia um novo jogo
2. **Contagem de Kills**: Linhas com `Kill:` são processadas para extrair mortes
3. **Jogadores**: Extraídos automaticamente das linhas de kill
4. **Estatísticas**:
   - `<world>` não é considerado um jogador
   - Mortes por `<world>` decrementam (-1) o score do jogador
   - Mortes entre jogadores incrementam (+1) o score do killer

## Tecnologias Utilizadas

- **.NET 8**: Framework principal
- **ASP.NET Core**: API Web
- **Swashbuckle**: Documentação Swagger
- **xUnit**: Framework de testes
- **FluentAssertions**: Assertions fluentes para testes
- **Moq**: Mocking para testes unitários
- **Microsoft.AspNetCore.Mvc.Testing**: Testes de integração

## Padrões e Boas Práticas

### Código Limpo
- **SOLID**: Princípios aplicados em toda a arquitetura
- **Clean Architecture**: Separação clara de responsabilidades
- **Dependency Injection**: Baixo acoplamento entre componentes

### Testes
- **Cobertura Abrangente**: Testes unitários e de integração
- **AAA Pattern**: Arrange, Act, Assert em todos os testes
- **Test Fixtures**: Dados de teste organizados
- **Mocking**: Isolamento de dependências externas

### Estrutura de Testes

```
tests/QuakeLogParser.Tests/
├── API/
│   ├── LogGameControllerTests.cs      # Testes de integração
│   └── LogGameControllerUnitTests.cs  # Testes unitários
├── Services/
│   ├── LogParserServiceTests.cs       # Testes de integração
│   └── LogParserServiceUnitTests.cs   # Testes unitários
└── Fixtures/
    └── sampleLogs/                    # Arquivos de teste
```

### Problemas Possiveis

1. **Arquivo de log não encontrado**:
   - Verifique se o arquivo `games.log` está na pasta `GameLog/`
   - Certifique-se de que o caminho está correto

2. **Erro de parsing**:
   - Verifique o formato do arquivo de log

## Desafio Original

### Task 1 - Construa um parser para o arquivo de log games.log e exponha uma API de consulta.

O arquivo games.log é gerado pelo servidor de quake 3 arena. Ele registra todas as informações dos jogos, quando um jogo começa, quando termina, quem matou quem, quem morreu pq caiu no vazio, quem morreu machucado, entre outros.

O parser deve ser capaz de ler o arquivo, agrupar os dados de cada jogo, e em cada jogo deve coletar as informações de morte.

### Observações

1. Quando o `<world>` mata o player ele perde -1 kill.
2. `<world>` não é um player e não deve aparecer na lista de players e nem no dicionário de kills.
3. `total_kills` são os kills dos games, isso inclui mortes do `<world>`.

### Task 2 - Após construir o parser construa uma API que faça a exposição de um método de consulta que retorne um relatório de cada jogo.

---

**Desenvolvido por**: Jeferson Macedo  
**Email**: jhef.salles@gmail.com  
**Tecnologia**: .NET 8 / C# / ASP.NET Core

