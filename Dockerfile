FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /vpsdk
ADD http://static.virtualparadise.org/dev-downloads/vpsdk_20210802_5afc54ae_linux_debian-stretch_x86_64.tar.gz ./vpsdk.tar.gz
RUN echo "9156B19DD83D2E2290F6C49228C99320478758C41D958E50030078A62DB6417B vpsdk.tar.gz" | sha256sum -c -&& \
    tar xfv vpsdk.tar.gz --strip-components=1 && \
    rm -r vpsdk.tar.gz include
ENV LD_LIBRARY_PATH=/vpsdk/lib
WORKDIR /vpservices

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["VPServices.csproj", "."]
RUN dotnet restore "./VPServices.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "VPServices.csproj" -c Release -o /vpservices/build

FROM build AS publish
RUN dotnet publish "VPServices.csproj" -c Release -o /vpservices/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /vpservices
COPY --from=publish /vpservices/publish .
ENTRYPOINT ["dotnet", "VPServices.dll"]