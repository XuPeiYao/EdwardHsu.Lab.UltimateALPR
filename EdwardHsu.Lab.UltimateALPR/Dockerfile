#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

WORKDIR /alpr
RUN apt update
RUN apt install unzip wget -y
RUN wget https://github.com/DoubangoTelecom/ultimateALPR-SDK/archive/master.zip
RUN pwd
RUN unzip master.zip
RUN wget https://doubango.org/deep_learning/libtensorflow_r1.14_cpu+gpu_linux_x86-64.tar.gz
RUN chmod +x ultimateALPR-SDK-master/binaries/linux/x86_64/recognizer
RUN tar -xf libtensorflow_r1.14_cpu+gpu_linux_x86-64.tar.gz -C ultimateALPR-SDK-master/binaries/linux/x86_64

RUN rm master.zip
RUN rm libtensorflow_r1.14_cpu+gpu_linux_x86-64.tar.gz

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["EdwardHsu.Lab.UltimateALPR/EdwardHsu.Lab.UltimateALPR.csproj", "EdwardHsu.Lab.UltimateALPR/"]
RUN dotnet restore "EdwardHsu.Lab.UltimateALPR/EdwardHsu.Lab.UltimateALPR.csproj"
COPY . .
WORKDIR "/src/EdwardHsu.Lab.UltimateALPR"
RUN dotnet build "EdwardHsu.Lab.UltimateALPR.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EdwardHsu.Lab.UltimateALPR.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EdwardHsu.Lab.UltimateALPR.dll"]