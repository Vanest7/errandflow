FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
# The demo uses the same-origin Nginx proxy; repository development settings are untouched.
COPY docker/frontend/api.config.demo.ts ./src/app/core/config/api.config.ts
RUN npm run build

FROM nginx:1.27-alpine
COPY docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/errandflow/browser/ /usr/share/nginx/html/
EXPOSE 80
