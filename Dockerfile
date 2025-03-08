FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

RUN apk update \
  && apk --no-cache add libc6-compat \
  && apk --no-cache add protobuf \
  && cd /root/.nuget/packages/grpc.tools/2.70.0/tools/linux_arm64 \
  && rm protoc \
  && ln -s /usr/bin/protoc protoc \
  && chmod +x grpc_csharp_plugin

COPY . ./
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /out ./
EXPOSE 5188

ENTRYPOINT ["dotnet", "WolfBankGateway.dll"]
