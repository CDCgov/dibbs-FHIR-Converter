# Getting started with the DIBBs FHIR Conversion Service

### Installing the .NET SDK

To check if a .NET SDK is installed, try running `dotnet --list-sdks`. You should see an output message that lists the .NET SDK version you have installed, as well as where it's located. It should look something like the following, but note that the version number and filepath will differ depending on which operating system you use and when you installed the .NET SDK.

```
8.0.117 [C:\Program Files\dotnet\sdk]
```

If you see a message like `Command 'dotnet' not found` (MacOS and Linux) or `The term 'dotnet' is not recognized as the name of a cmdlet, function, script file, or operable program` (Windows), then .NET has not been installed. Additionally, if running `dotnet --list-sdks` does not produce any output, then you likely have the .NET runtime installed, but not the SDK. In either event, you should [follow the instructions located here to install the SDK](https://learn.microsoft.com/en-us/dotnet/core/install/).

## Running the DIBBs FHIR Conversion Service

### Running with Docker (Recommended for Production)

To run the DIBBs FHIR Conversion service with Docker, follow these steps.

1. Confirm that you have Docker installed by running `docker -v`. If you don't see a response similar to what's shown below, follow [these instructions](https://docs.docker.com/get-docker/) to install Docker.

```
❯ docker -v
Docker version 20.10.21, build baeda1f
```

2. Download a copy of the Docker image from the dibbs-ecr-viewer repository by running `docker pull ghcr.io/cdcgov/dibbs-ecr-viewer/fhir-converter:latest`.
3. Run the service with ` docker run -p 8080:8080 ghcr.io/cdcgov/dibbs-ecr-viewer/fhir-converter:latest`.

Congratulations, the FHIR Conversion service should now be running on `localhost:8080`!

### Running from C# Source Code

For local development, you may prefer to run the service directly from dotnet. To do so, run the command `dotnet run` from within `src/Dibbs.FhirConverterApi`

### Building the Docker Image

To build the Docker image for the FHIR Conversion service from source code instead of downloading it from the dibbs-ecr-viewer repository, follow these steps.

1. Ensure that [Docker](https://docs.docker.com/get-docker/) is installed.
2. Navigate to the root of this repo.
3. Build the image `docker build -t fhir-converter:latest .`.
4. Run the image `docker run -it fhir-converter:latest`.

#### Tips & Tricks

- To run the unit tests, use the command `dotnet test src/Dibbs.FhirConverterApi.UnitTests`.
- To run the integration tests, use the command `dotnet test src/Dibbs.FhirConverterApi.FunctionalTests`.

## Using the FHIR Converter API

To convert your source data to FHIR, make a `POST` request to the `/convert-to-fhir` endpoint. If you are running the API locally, the URL will be `http://localhost:8080/convert-to-fhir`.
This request will include some number of the following values in the request body depending on your use case. **Please note: `input_data` and `rr_data` must be JSON escaped.**
- `input_data`: **Required**. The primary data payload that will be converted.
- `rr_data`: Optional. Reportability Response information to be merged with input_data before conversion.

### Sample FHIR Conversion Request
```
curl -X POST -H "Content-Type: application/json" -d \
'{"input_data": "<ClinicalDocument></ClinicalDocument>", "rr_data": "<ClinicalDocument></ClinicalDocument>"}' \
http://localhost:8080/convert-to-fhir
```

## Architecture Diagram

```mermaid
graph TD;
    style webApp fill:#F5F5F5,stroke:#000000,stroke-width:2px,color:#000000;
    style apiConvert fill:#E8F4F8,stroke:#2C3E50,stroke-width:2px,color:#2C3E50;
    style apiHealthCheck fill:#E8F4F8,stroke:#2C3E50,stroke-width:2px,color:#2C3E50;
    style apiSwagger fill:#E8F4F8,stroke:#2C3E50,stroke-width:2px,color:#2C3E50;

    webApp[FHIR Converter Service];

    subgraph API Endpoints
        direction TB
        apiConvert[Conversion API]
        apiHealthCheck[Health Check API]
        apiSwagger[Swagger Documentation]
    end

    webApp -->|POST /convert-to-fhir| apiConvert;
    webApp -->|GET /| apiHealthCheck;
    webApp -->|GET /swagger| apiSwagger;
```

## Testing / Debugging

When testing, you can print from the liquid templates with the following command in the templates.

```
{{ "string" | print_object }}
{{ objectName | print_object }}
```

This will print objects or strings to the console for debugging purposes. You must set the environment variable `DEBUG_LOG` to `"true"`.

If debugging an exception, the below snippet of code can be helpful to get more information on where the exception is coming from.

```csharp
Console.WriteLine("Ex: {1} StackTrace: '{0}'", Environment.StackTrace, ex);
```
