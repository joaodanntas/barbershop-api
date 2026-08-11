# RZR Barber Shop API

Sistema completo de agendamento online para barbearias, com backend em ASP.NET Core, autenticação JWT, notificações automáticas por e-mail e painel administrativo. Projeto em produção, atendendo clientes reais.

🔗 **Demo:** [barbershop-api-acij.onrender.com](https://barbershop-api-acij.onrender.com/api/servicos)
🔗 **Frontend (site do cliente):** [joaodanntas.github.io/barbershop-frontend](https://joaodanntas.github.io/barbershop-frontend/)
🔗 **Repositório do frontend:** [barbershop-frontend](https://github.com/joaodanntas/barbershop-frontend)

---

## 📋 Sobre o projeto

O RZR Barber Shop API é o backend de um sistema de agendamento para barbearias, construído do zero para resolver um problema real: permitir que clientes marquem horários online, sem ligação e sem espera, enquanto o administrador gerencia toda a operação (barbeiros, serviços, horários e agendamentos) por um painel próprio.

O projeto foi desenvolvido com foco em qualidade de produção — não é um protótipo. Inclui autenticação segura, prevenção de conflitos de agendamento em nível de banco de dados, notificações automáticas, conformidade com a LGPD, e está publicamente hospedado e funcional.

## ✨ Funcionalidades

- **Autenticação e autorização** — JWT com perfis distintos (Cliente/Admin), senhas com hash BCrypt, recuperação de senha por e-mail, rate limiting contra força bruta
- **Agendamento inteligente** — geração dinâmica de horários disponíveis, respeitando expediente, pausas (almoço), antecedência mínima configurável por serviço, e bloqueio de datas (feriados globais ou folgas individuais por barbeiro)
- **Prevenção de conflitos (race conditions)** — índice único parcial no PostgreSQL garante que dois clientes nunca reservem o mesmo horário simultaneamente, mesmo sob concorrência real (testado com requisições paralelas)
- **Notificações automáticas por e-mail** — confirmação, cancelamento e lembrete de agendamento (2h antes), via integração com a API do Resend e um `BackgroundService` rodando em segundo plano
- **Conformidade com a LGPD** — cliente pode consultar, editar e excluir seus dados pessoais diretamente no site; a exclusão anonimiza os dados cadastrais preservando o histórico de agendamentos (exigido para fins de auditoria/transação), e exige confirmação de senha por ser uma ação irreversível
- **Painel administrativo completo** — CRUD e edição para barbeiros, serviços, disponibilidade e bloqueios de data, com paginação na listagem de agendamentos
- **Segurança** — CORS restrito por origem, proteção contra XSS armazenado no frontend, segredos gerenciados fora do código-fonte

## 🛠️ Stack técnica

| Categoria | Tecnologia |
|---|---|
| Backend | C# · ASP.NET Core (.NET 10) |
| ORM | Entity Framework Core |
| Banco de dados | PostgreSQL (hospedado no [Neon](https://neon.tech)) |
| Autenticação | JWT Bearer + BCrypt |
| E-mail transacional | [Resend](https://resend.com) |
| Containerização | Docker |
| Deploy | [Render](https://render.com) (backend) · GitHub Pages (frontend) |
| Frontend | HTML/CSS/JavaScript vanilla |

## 🏗️ Arquitetura e decisões técnicas

- **Prevenção de duplo agendamento:** em vez de depender só de validação na aplicação, a integridade é garantida por um índice único parcial no PostgreSQL (`(BarbeiroId, DataHoraInicio) WHERE Status <> 'Cancelado'`), eliminando a janela de risco de race conditions mesmo com requisições simultâneas.
- **Background jobs:** lembretes de agendamento são processados por um `BackgroundService` nativo do .NET, que verifica periodicamente agendamentos confirmados próximos do horário, sem depender de infraestrutura externa de filas.
- **Exclusão de dados (LGPD) via anonimização:** como agendamentos têm `DeleteBehavior.Restrict` (não podem ser deletados em cascata) e o histórico de transações precisa ser preservado, a exclusão de conta substitui os dados pessoais identificáveis (nome, e-mail, telefone, senha) em vez de remover a linha do banco — mantendo a integridade referencial e cumprindo a lei.
- **Gestão de segredos:** todas as credenciais (connection strings, chaves JWT, API keys) ficam fora do controle de versão, usando User Secrets em desenvolvimento e variáveis de ambiente em produção.
- **Timezone:** o sistema trata horários de expediente como "hora de parede" (sem fuso), evitando bugs comuns de conversão UTC em aplicações locais de agendamento.

## 🚀 Rodando localmente

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (local ou um projeto no [Neon](https://neon.tech))
- Uma conta gratuita no [Resend](https://resend.com) (para envio de e-mails)

### Passos

```bash
# Clone o repositório
git clone https://github.com/joaodanntas/barbershop-api.git
cd barbershop-api

# Configure os segredos locais (nunca commitados)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=barbershop;Username=postgres;Password=SUA_SENHA"
dotnet user-secrets set "Jwt:Key" "sua-chave-secreta-com-pelo-menos-32-caracteres"
dotnet user-secrets set "Jwt:Issuer" "BarberShopApi"
dotnet user-secrets set "Jwt:Audience" "BarberShopFrontend"
dotnet user-secrets set "Jwt:ExpiracaoHoras" "8"
dotnet user-secrets set "Resend:ApiKey" "sua-api-key-do-resend"

# Aplique as migrations
dotnet ef database update

# Rode a aplicação
dotnet run
```

A API sobe em `http://localhost:5112`.

### Rodando com Docker

```bash
docker build -t barbershop-api .
docker run -p 8080:8080 --env-file .env barbershop-api
```

## 📁 Estrutura do projeto

```
BarberShopApi/
├── Controllers/    # Endpoints da API
├── Services/       # Regras de negócio (agendamento, e-mail, lembretes)
├── Models/         # Entidades do domínio
├── DTOs/           # Contratos de entrada/saída da API
├── Data/           # DbContext e configuração do EF Core
├── Helpers/        # Utilitários (ex: tratamento de horário local)
├── Migrations/      # Histórico de migrations do banco
└── Dockerfile
```

## 🔒 Segurança e privacidade

- Senhas armazenadas com hash BCrypt (nunca em texto plano)
- Tokens JWT com expiração configurável
- Rate limiting nos endpoints de autenticação
- CORS restrito a origens explicitamente permitidas
- Sanitização de dados dinâmicos no frontend para prevenção de XSS
- Segredos geridos via variáveis de ambiente / User Secrets, nunca versionados
- Endpoints de autoatendimento LGPD (`GET/PUT/DELETE /api/usuarios/me`) protegidos por autenticação, com confirmação de senha exigida para exclusão de conta

## 📌 Próximos passos

- [ ] Domínio de e-mail verificado (saindo do domínio de teste do Resend)
- [ ] Testes automatizados
- [ ] Log de auditoria de ações administrativas

## 👤 Autor

**João Gabriel Dantas**
[LinkedIn](https://linkedin.com/in/joaodanntas) · [GitHub](https://github.com/joaodanntas)

---

Projeto desenvolvido como parte da minha jornada de aprendizado em desenvolvimento backend, aplicando na prática conceitos de arquitetura, segurança e deploy em produção.
