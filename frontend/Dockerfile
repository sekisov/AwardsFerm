# Сборка из корня репозитория:
#   docker build -f frontend/Dockerfile -t awardsferm-frontend .

FROM node:20-alpine AS build
WORKDIR /app
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
COPY src/AwardsFerm.Web/package.json src/AwardsFerm.Web/package-lock.json* ./
RUN npm ci
COPY src/AwardsFerm.Web/ ./
RUN npm run build

FROM nginx:alpine
ARG NGINX_CONFIG=nginx.compose.conf
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/${NGINX_CONFIG} /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
