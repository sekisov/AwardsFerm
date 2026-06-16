FROM node:20-alpine AS build
WORKDIR /app
COPY src/AwardsFerm.Web/package.json src/AwardsFerm.Web/package-lock.json* ./
RUN npm install
COPY src/AwardsFerm.Web/ ./
ENV VITE_API_URL=
RUN npm run build

FROM nginx:alpine AS runtime
COPY docker/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
