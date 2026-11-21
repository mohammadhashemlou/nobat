# syntax=docker/dockerfile:1
# Production image: Nuxt builds to a self-contained Node server in .output/.

# --------------------------------------------------------------------------- build
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# The API base URL is baked in at build time for the client bundle.
ARG NUXT_PUBLIC_API_BASE=http://localhost:5000
ENV NUXT_PUBLIC_API_BASE=$NUXT_PUBLIC_API_BASE
RUN npm run build

# --------------------------------------------------------------------------- runtime
FROM node:22-alpine AS final
WORKDIR /app

ENV NODE_ENV=production \
    NITRO_HOST=0.0.0.0 \
    NITRO_PORT=3000 \
    PORT=3000

# wget ships with Alpine (busybox) and backs the health check.
COPY --from=build /app/.output ./.output

USER node
EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:3000/ || exit 1

CMD ["node", ".output/server/index.mjs"]
