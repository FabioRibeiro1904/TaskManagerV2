# Task Manager - Sistema de Gerenciamento de Tarefas

Sistema completo de gerenciamento de tarefas com autenticação JWT avançada e controle de permissões. Esta aplicação combina uma API robusta com uma interface web responsiva para oferecer uma experiência completa de gerenciamento de projetos.

## Sobre o Projeto

Este é um sistema profissional de gerenciamento de tarefas desenvolvido para demonstrar boas práticas de desenvolvimento web moderno. O projeto implementa:

- Sistema de autenticação JWT com refresh tokens
- Controle granular de permissões por roles
- Interface web responsiva com Bootstrap
- API RESTful completa
- Dashboard com métricas em tempo real

## Tecnologias Utilizadas

- **ASP.NET Core 9** - Framework web moderno e performático
- **Entity Framework Core** - ORM para acesso ao banco de dados
- **SQLite** - Banco de dados leve e portável
- **JWT Bearer** - Autenticação segura com tokens
- **BCrypt** - Criptografia robusta para senhas
- **Bootstrap 5** - Framework CSS responsivo
- **Font Awesome** - Biblioteca de ícones

## Como Executar

### Pré-requisitos
- .NET 9 SDK instalado
- Editor de código (Visual Studio, VS Code ou similar)

### Passos para executar

```bash
# Clone o repositório
git clone [url-do-repositorio]

# Navegue até o diretório do projeto
cd 04-jwt-task-manager/TaskManager.Web

# Restaure as dependências
dotnet restore

# Execute a aplicação
dotnet run
```

### Acesso ao Sistema

- **Interface Web**: http://localhost:5000
- **API**: http://localhost:5000/api
- **Documentação Swagger**: http://localhost:5000/swagger

### Credenciais de Acesso

O sistema cria automaticamente um usuário administrador:
- **Email**: admin@taskmanager.com
- **Senha**: Admin123!

## Funcionalidades Principais

### Sistema de Autenticação

**Autenticação Híbrida**
O sistema implementa autenticação tanto para a interface web (cookies) quanto para a API (JWT), permitindo flexibilidade no uso.

**Segurança Avançada**
- Tokens JWT com expiração configurável
- Refresh tokens para renovação automática
- Hash seguro de senhas com BCrypt
- Controle de sessões ativas

### Níveis de Permissão

**Administrador**
Possui acesso completo ao sistema, incluindo:
- Gerenciamento de todos os usuários
- Acesso a todas as tarefas
- Criação e edição de categorias
- Relatórios e métricas globais

**Gerente**
Tem permissões para gerenciar equipes:
- Visualização de tarefas da equipe
- Atribuição de tarefas para membros
- Criação de categorias
- Relatórios da equipe

**Usuário**
Foco nas tarefas pessoais:
- Gerenciamento das próprias tarefas
- Comentários em tarefas
- Alteração de status das tarefas atribuídas

### Gerenciamento de Tarefas

**Funcionalidades Completas**
- Criação, edição e exclusão de tarefas
- Sistema de prioridades (Alta, Média, Baixa)
- Categorização com cores personalizadas
- Atribuição entre membros da equipe
- Sistema de comentários
- Controle de status (Pendente, Em Progresso, Concluída)

**Dashboard Intuitivo**
- Visão geral das tarefas
- Filtros avançados
- Métricas de produtividade
- Interface responsiva

## Estrutura do Projeto

```
TaskManager.Web/
├── Controllers/            # Controladores da aplicação
│   ├── AccountController.cs    # Autenticação web
│   ├── TaskController.cs       # Interface de tarefas
│   ├── AuthController.cs       # API de autenticação
│   └── TasksApiController.cs   # API de tarefas
├── Views/                  # Templates Razor
│   ├── Account/               # Páginas de login/registro
│   ├── Task/                  # Interface de tarefas
│   └── Shared/                # Layouts compartilhados
├── Models/                 # Modelos de dados
├── Services/               # Lógica de negócio
├── DTOs/                   # Objetos de transferência
├── Data/                   # Contexto do banco
└── wwwroot/               # Arquivos estáticos
```

## API RESTful

### Endpoints de Autenticação

```http
POST /api/auth/login        # Autenticação
POST /api/auth/register     # Registro de usuários
POST /api/auth/refresh      # Renovação de token
POST /api/auth/logout       # Logout
```

### Endpoints de Tarefas

```http
GET /api/tasks              # Listar tarefas
POST /api/tasks             # Criar tarefa
GET /api/tasks/{id}         # Obter tarefa específica
PUT /api/tasks/{id}         # Atualizar tarefa
DELETE /api/tasks/{id}      # Excluir tarefa
```

### Exemplo de Uso da API

**Login**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@taskmanager.com",
  "password": "Admin123!"
}
```

**Criar Tarefa**
```http
POST /api/tasks
Authorization: Bearer [token]
Content-Type: application/json

{
  "title": "Implementar nova funcionalidade",
  "description": "Descrição detalhada da tarefa",
  "priority": 3,
  "categoryId": 1,
  "dueDate": "2025-12-31T23:59:59"
}
```

## Segurança Implementada

### Proteção de Dados
- Senhas criptografadas com BCrypt
- Tokens JWT assinados digitalmente
- Validação de entrada em todos os endpoints
- Proteção contra ataques CSRF

### Controle de Acesso
- Autorização baseada em roles
- Validação de permissões em cada operação
- Tokens com expiração configurável
- Revogação de tokens no logout

## Banco de Dados

O sistema utiliza SQLite para simplicidade e portabilidade. O banco é criado automaticamente na primeira execução, incluindo:

- Tabelas otimizadas com relacionamentos
- Índices para performance
- Dados iniciais (usuário admin e categorias)
- Migrações automáticas

## Interface do Usuário

### Design Responsivo
- Interface adaptável para desktop, tablet e mobile
- Componentes Bootstrap para consistência
- Ícones Font Awesome para melhor UX
- Cores e tipografia profissionais

### Experiência do Usuário
- Navegação intuitiva
- Feedback visual para ações
- Formulários com validação em tempo real
- Loading states e mensagens de erro claras

## Desenvolvimento e Arquitetura

### Padrões Utilizados
- **Repository Pattern** para acesso a dados
- **Service Layer** para lógica de negócio
- **DTO Pattern** para transferência de dados
- **Dependency Injection** para inversão de controle

### Boas Práticas
- Código limpo e bem documentado
- Separação clara de responsabilidades
- Tratamento adequado de erros
- Logging estruturado

## Configuração e Personalização

### Configurações JWT
As configurações de autenticação podem ser ajustadas no arquivo `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "chave-secreta-jwt",
    "Issuer": "TaskManager.Web",
    "Audience": "TaskManager.Client",
    "ExpirationMinutes": 60
  }
}
```

### Banco de Dados
A string de conexão pode ser personalizada conforme necessário:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=TaskManager.db"
  }
}
```

## Próximas Melhorias

### Funcionalidades Futuras
- Notificações por email para tarefas vencidas
- Sistema de anexos para tarefas
- Relatórios avançados em PDF
- Integração com calendários externos
- Notificações push em tempo real

### Melhorias Técnicas
- Cache Redis para performance
- Logs mais detalhados com Serilog
- Testes automatizados abrangentes
- Pipeline CI/CD
- Containerização com Docker

## Contribuição

Este projeto foi desenvolvido com foco em demonstrar boas práticas de desenvolvimento web com .NET Core. Contribuições são bem-vindas através de pull requests.

### Como Contribuir
1. Faça um fork do projeto
2. Crie uma branch para sua feature
3. Implemente suas mudanças
4. Adicione testes se necessário
5. Envie um pull request

## Suporte e Documentação

Para dúvidas sobre implementação ou uso do sistema, consulte:
- Código-fonte comentado
- Documentação da API via Swagger
- Exemplos de uso inclusos

## Licença

Este projeto está disponível sob a licença MIT, permitindo uso livre para fins educacionais e comerciais.

---

**Desenvolvido para demonstrar expertise em desenvolvimento web full-stack com .NET Core, focando em segurança, performance e experiência do usuário.**
