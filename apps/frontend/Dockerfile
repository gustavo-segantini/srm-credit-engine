# ── Stage 1: build ────────────────────────────────────────────────────
FROM node:22-alpine AS build
# Patch OS-level packages to eliminate Alpine CVEs
RUN apk upgrade --no-cache
WORKDIR /app

# Install dependencies (leverage cache)
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# Copy source and build
COPY . .
RUN npm run build

# ── Stage 2: serve with Nginx ─────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime
# Patch OS-level packages to eliminate Alpine CVEs
RUN apk upgrade --no-cache
COPY --from=build /app/dist /usr/share/nginx/html

# Custom Nginx config: HTML5 history API + gzip + security headers
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s \
    CMD wget -qO- http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
