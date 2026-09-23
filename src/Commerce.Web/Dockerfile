# Build from repo root: docker compose build commerce.web
FROM node:20-alpine AS build
WORKDIR /app

COPY src/Commerce.Web/package.json src/Commerce.Web/package-lock.json* ./
RUN npm ci

COPY src/Commerce.Web/ ./

ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY src/Commerce.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
