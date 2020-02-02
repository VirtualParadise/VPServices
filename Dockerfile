FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

WORKDIR /vpservices
COPY . .
RUN dotnet publish -c Release -r linux-x64 -o output



FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1

WORKDIR /vpsdk
ADD http://dev.virtualparadise.org/downloads/vpsdk_20190123_1ece91e_linux_debian-stretch_x86_64.tar.gz ./vpsdk.tar.gz
RUN echo "311DEAC631893CE2EB4671EAC87A3EC35650F78342441D2CD1BC33747A227784 vpsdk.tar.gz" | sha256sum -c -&& \
    tar xfv vpsdk.tar.gz --strip-components=1 && \
    rm -r vpsdk.tar.gz include

WORKDIR /vpservices
COPY --from=builder /vpservices/output/* ./

ENV LD_LIBRARY_PATH=/vpsdk/lib
CMD ["./VPServices" ]
