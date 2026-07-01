# Guia de Administração e Deploy via Google Cloud SDK (gcloud)

Este guia destina-se a desenvolvedores e agentes LLM encarregados de acessar, gerenciar e realizar o deploy do projeto **Interactive Fantastic Tales** utilizando a CLI oficial do Google Cloud (`gcloud`).

---

## 🔑 Autenticação e Configuração Inicial

Para rodar comandos de administração e deploy de forma automatizada (head-less) ou local, utilize a chave de conta de serviço unificada fornecida em **`firebase-key.json`** na raiz do projeto.

### 1. Ativar a Conta de Serviço no gcloud
Esta chave possui o papel de **Editor (`roles/editor`)** no GCP, permitindo acesso total a recursos do projeto:
```bash
gcloud auth activate-service-account --key-file=firebase-key.json
```

### 2. Configurar o Projeto Ativo
Defina o projeto padrão para evitar a necessidade de passar o parâmetro `--project` em cada comando:
```bash
gcloud config set project interactive-tales-2026
```

### 3. Verificar o Status de Conexão
Para confirmar que você está autenticado com as permissões corretas no projeto correto:
```bash
gcloud auth list
gcloud config list
```

---

## 📦 Deploy da Aplicação

### Firebase Hosting (Plano Spark - Gratuito) ⚡
Como o projeto está na camada gratuita do Firebase, o deploy da aplicação estática (SPA) é feito usando o Firebase CLI (localizado nas dependências do projeto):
```bash
# Executa o script de deploy que detecta automaticamente a chave de serviço
# Windows:
.\deploy-to-gcp.bat --non-interactive

# Unix/macOS:
./deploy-to-gcp.sh --non-interactive
```

---

## 🗄️ Administração do Firestore (Banco de Dados)

O Firestore guarda o estado das histórias do editor. Comandos úteis do `gcloud` para administrá-lo:

### 1. Exportar Dados (Backup)
Para exportar os documentos do Firestore para um bucket do Cloud Storage (Requer Billing ativo para criar o bucket de destino):
```bash
gcloud firestore export gs://NOME_DO_BUCKET_DE_BACKUP --project=interactive-tales-2026
```

### 2. Importar Dados (Restaurar)
Para restaurar dados a partir de um backup exportado anteriormente no Cloud Storage:
```bash
gcloud firestore import gs://NOME_DO_BUCKET_DE_BACKUP/PASTA_DO_BACKUP/ --project=interactive-tales-2026
```

---

## 🛡️ Controle de Acessos e IAM

Para gerenciar quais contas ou outros agentes podem acessar e modificar os recursos deste projeto:

### 1. Listar Usuários e Contas de Serviço com Acesso
```bash
gcloud projects get-iam-policy interactive-tales-2026
```

### 2. Conceder Permissões a um Novo Usuário/Agente
Para adicionar um novo desenvolvedor ou conta de serviço com a permissão de Visualizador (Viewer) ou Editor:
```bash
# Conceder papel de Leitor (Viewer)
gcloud projects add-iam-policy-binding interactive-tales-2026 \
    --member="user:email-do-usuario@gmail.com" \
    --role="roles/viewer"
```

### 3. Remover Permissões de Acesso
```bash
gcloud projects remove-iam-policy-binding interactive-tales-2026 \
    --member="user:email-do-usuario@gmail.com" \
    --role="roles/viewer"
```

---

## 📊 Logs e Monitoramento

Caso ocorram problemas de conexão de APIs ou erros na aplicação, você pode monitorar as requisições de serviço em tempo real:

### 1. Ler logs de auditoria do Firestore ou APIs de Autenticação
```bash
gcloud logging read "resource.type=audited_resource" --limit=20
```

### 2. Ler logs de deploy e build do Cloud Build (caso ativado)
```bash
gcloud builds list --limit=5
```

---

## 💳 Gerenciamento de Faturamento (Billing Account)

> [!IMPORTANT]
> O projeto `interactive-tales-2026` está sem faturamento ativo. Recursos do GCP como **Cloud Run**, **Cloud Build** e **Cloud Storage** estão desabilitados pela Google Cloud.
>
> Para desbloquear e realizar o deploy do editor no Cloud Run:
> 1. Acesse o [Console do GCP - Billing](https://console.cloud.google.com/billing).
> 2. Vincule uma conta de faturamento ativa ao projeto `interactive-tales-2026`.
