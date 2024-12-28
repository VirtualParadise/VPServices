FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /vpsdk
ADD http://static.virtualparadise.org/dev-downloads/vpsdk_20241201_734ca140_linux_debian10_x86_64.tar.gz ./vpsdk.tar.gz
RUN echo "3033EEFE0B8E6742C690C8AC4A27864401CE5C583EB8AD6C84E1AD44E6C11679 vpsdk.tar.gz" | sha256sum -c -&& \
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
