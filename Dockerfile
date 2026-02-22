FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /vpsdk
ADD http://static.virtualparadise.org/dev-downloads/vpsdk_20260222_e8e15ab5_linux_debian10_x86_64.tar.gz ./vpsdk.tar.gz
RUN echo "CFD8E0DB2E12750B02CD8A679C56C5F7E358B161AB5F5E68BE4C9062DAD642FD vpsdk.tar.gz" | sha256sum -c -&& \
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
