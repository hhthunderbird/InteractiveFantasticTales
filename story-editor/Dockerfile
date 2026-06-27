# Estágio de Build
FROM node:20-alpine AS build

WORKDIR /app

# Copia arquivos de dependência
COPY package*.json ./

# Instala todas as dependências
RUN npm ci

# Copia o restante do código
COPY . .

# Executa o build de produção (Vite lê o .env se estiver presente no diretório)
RUN npm run build

# Estágio de Produção
FROM nginx:alpine

# Copia os arquivos gerados no build para o diretório padrão do Nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Copia a configuração personalizada do Nginx para tratar rotas SPA
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
