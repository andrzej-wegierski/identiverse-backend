docker run -d \
  --name identiverse-db \
  -e POSTGRES_USER=local \
  -e POSTGRES_PASSWORD=psql \
  -e POSTGRES_DB=identiverse-db \
  -p 5432:5432 \
  -v identity_postgres_data:/var/lib/postgresql/data \
  postgres:16