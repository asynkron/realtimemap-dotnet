FROM node:16.12.0-alpine3.14 as builder

WORKDIR /app
COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build

FROM nginx:1.21.3
COPY --from=builder /app/dist /usr/share/nginx/html
COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
