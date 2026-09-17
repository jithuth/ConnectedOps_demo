# Build stage: compile the tiny sysinfo hook
FROM ubuntu:22.04 AS build
RUN apt-get update && apt-get install -y --no-install-recommends gcc libc6-dev
WORKDIR /src
COPY src/ConnectedOps.SqlServer/fake_meminfo.c .
RUN gcc -shared -fPIC -o fake_meminfo.so fake_meminfo.c -ldl

# Runtime stage: official Microsoft SQL Server 2022
FROM mcr.microsoft.com/mssql/server:2022-latest
USER root
COPY --from=build /src/fake_meminfo.so /opt/mssql/lib/fake_meminfo.so
RUN chmod 755 /opt/mssql/lib/fake_meminfo.so && echo "/opt/mssql/lib/fake_meminfo.so" > /etc/ld.so.preload
ENV LD_PRELOAD=/opt/mssql/lib/fake_meminfo.so
USER mssql
