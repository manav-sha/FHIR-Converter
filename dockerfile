FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /app

COPY Fhir.Liquid.Converter.sln ./
# COPY src/*/*.csproj ./src/*/
COPY src/ ./src/

RUN dotnet restore --disable-parallel

COPY . ./

# Build the solution
RUN dotnet publish -c Release -o out

# Use the official .NET runtime image as a runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Set the working directory
WORKDIR /app

COPY data/ ./template_data/

# Copy the published output from the build stage
COPY --from=build-env /app/out .

# Expose the port the app runs on
EXPOSE 80

# Start the app
ENTRYPOINT ["dotnet", "FhirConverterApi.dll"]
