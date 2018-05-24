FROM microsoft/dotnet-framework:4.7.1-runtime-windowsservercore-1709

SHELL ["powershell"]
RUN mkdir c:\app
WORKDIR /app
COPY ./publish/ /app
CMD ["DockerGracefullShutdownTest.exe"]