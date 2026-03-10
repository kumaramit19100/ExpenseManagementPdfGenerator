## ExpenseManagementPdfGenerator

`ExpenseManagementPdfGenerator` is a small ASP.NET Core Web API service that generates PDF files from HTML content using Microsoft Playwright (headless Chromium).  
It is designed to be used by the broader Expense Management system (or any other client) as a dedicated microservice for server‑side PDF generation.

The service:
- **Accepts** HTML fragments (base64 encoded) and optional CSS.
- **Renders** the content in a headless Chromium browser.
- **Returns** a ready‑to‑download PDF file in the HTTP response.

---

## 1. Project Overview

- **What it does**:  
  - Exposes an HTTP API (`POST /api/pdf/generate`) that converts HTML content into a single PDF document.
  - Supports multiple HTML fragments, which are combined into a multi‑page PDF.
  - Allows custom CSS to style the generated PDF.

- **Main purpose**:  
  - Offload PDF rendering from the main Expense Management application into a dedicated, scalable service.
  - Provide consistent, server‑side generated PDFs (e.g., expense reports, invoices, summaries).

- **Problem it solves**:  
  - Avoids running heavy browser instances inside the main app.
  - Provides a **standard, reusable** PDF generation API that can be called from any backend or frontend.
  - Optimized for constrained environments by reusing a shared Playwright browser instance.

---

## 2. How the Project Works

### 2.1 High‑level workflow

1. **Client calls API**  
   - Sends a `POST` request to `api/pdf/generate` with:
     - An API key header: `X-Pdf-Api-Key`.
     - A JSON body of type `HtmlToPdfModel`.
2. **API key validation**  
   - `PdfController` reads the configured key from `PdfGenerator:ApiKey` in configuration.
   - Rejects the request with **401 Unauthorized** if the header is missing or invalid.
3. **Input validation**  
   - Ensures the request body exists.
   - Ensures `HtmlData` contains at least one HTML fragment (base64 encoded).
4. **HTML document assembly**  
   - Decodes each `HtmlData` entry from Base64 to UTF‑8 HTML.
   - Wraps the content in a full HTML document (`<html>`, `<head>`, `<body>`).
   - Injects optional CSS (from `Css`) into a `<style>` tag in `<head>`.
   - Inserts page‑break separators between fragments for multi‑page PDFs.
5. **Rendering via Playwright**  
   - Uses `SharedPlaywrightBrowser` to get a **shared** Chromium instance.
   - Opens a new browser context and page for each request.
   - Sets the page content to the assembled HTML and waits for **network idle**.
   - Waits an additional short delay to ensure final rendering is complete.
6. **PDF generation**  
   - Calls `page.PdfAsync` with:
     - `Format = "A4"` (default page size).
     - `PrintBackground = true` (include backgrounds and colors).
   - Returns the resulting PDF bytes.
7. **HTTP response**  
   - Returns a `File` response with:
     - Content type: `application/pdf`
     - File name: value from `FileName`, or `document.pdf` if empty.

### 2.2 Input and output models

- **Request model** `HtmlToPdfModel`:

```csharp
public class HtmlToPdfModel
{
    public List<string> HtmlData { get; set; } = new List<string>(); // Base64-encoded HTML fragments
    public string? FileName { get; set; }                             // Optional output file name
    public string? Format { get; set; }                               // Optional page format (currently fixed to A4)
    public string? Css { get; set; }                                  // Optional global CSS
}
```

- **Error response model** `PdfResponse`:

```csharp
public class PdfResponse
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public bool Success => StatusCode >= 200 && StatusCode < 300;
}
```

On success, the endpoint returns the PDF directly instead of a `PdfResponse` body.

---

## 3. Technology Stack

- **Backend**
  - **.NET**: .NET 8 (`net8.0`)
  - **Framework**: ASP.NET Core Web API

- **PDF / Browser**
  - **Microsoft.Playwright** (`Microsoft.Playwright` NuGet package)
  - Headless **Chromium** browser

- **API documentation**
  - **Swashbuckle.AspNetCore** (Swagger UI & OpenAPI)

- **Containerization**
  - **Docker** multi‑stage build
  - Runtime image: `mcr.microsoft.com/playwright/dotnet:v1.58.0-jammy`
  - Build/publish image: `mcr.microsoft.com/dotnet/sdk:8.0`

---

## 4. Project Structure

Key files and folders:

- **`Program.cs`**
  - Configures ASP.NET Core pipeline.
  - Registers controllers and Swagger.
  - On application start:
    - Calls `SharedPlaywrightBrowser.InitializeAsync()` to warm up the browser.
  - On application stop:
    - Calls `SharedPlaywrightBrowser.CloseAsync()` to dispose the browser and Playwright.

- **`Controllers/PdfController.cs`**
  - Main API controller for PDF generation.
  - Endpoint: `POST api/pdf/generate`
  - Responsibilities:
    - Validate API key (`X-Pdf-Api-Key`).
    - Validate request model (`HtmlToPdfModel`).
    - Build HTML document and invoke Playwright to generate PDF.
    - Return a file response or a structured error (`PdfResponse`).

- **`Models/HtmlToPdfModel.cs`**
  - Defines the request payload for PDF generation.

- **`Models/PdfResponse.cs`**
  - Standard error/metadata response model used when the API returns JSON instead of a file.

- **`Services/SharedPlaywrightBrowser.cs`**
  - Manages a **shared static Playwright + Chromium browser** instance.
  - Avoids spinning up a new browser for every request.
  - Provides:
    - `GetBrowserAsync()` – returns a running browser instance.
    - `InitializeAsync()` – warm‑up call on startup.
    - `CloseAsync()` – cleanup on application shutdown.

- **`appsettings.json` / `appsettings.Development.json`**
  - Application configuration (logging, `PdfGenerator:ApiKey`, etc.).

- **`Dockerfile`**
  - Multi‑stage Docker build definition (restore, build, publish, final runtime image).

- **`ExpenseManagementPdfGenerator.csproj`**
  - Project file (target framework, NuGet package references, Docker context).

---

## 5. Prerequisites

To run the project locally:

- **.NET SDK**
  - `.NET SDK 8.0` or later.

- **Playwright browsers (for local runs)**
  - Playwright browser binaries must be installed once after building the project.
  - **Steps to install on your local machine:**
    1. Ensure the `.NET SDK 8.0` is installed and the project builds successfully (`dotnet build`).
    2. Install the Playwright CLI (one time):
       - `dotnet tool install --global Microsoft.Playwright.CLI`
    3. Download the browser binaries:
       - `playwright install`
    4. (Alternative on Windows) After building the project, run the generated helper script:
       - `.\bin\Debug\net8.0\playwright.ps1 install`
    5. Once these steps succeed, Playwright’s Chromium binaries are available for all local runs.

- **Docker (optional but recommended)**
  - Docker Engine (for building and running the container image).

- **Tools (optional)**
  - Visual Studio 2022 / Rider / VS Code with C# extension.
  - PowerShell (for running the `playwright.ps1` helper script on Windows).

---

## 6. Docker Setup

The Dockerfile is configured to:
- Use a Playwright‑enabled .NET runtime image.
- Build and publish the app in a separate build stage.
- Expose the app on port `8080` inside the container.

### 6.0 Dockerfile stages explained

The Dockerfile uses a **multi‑stage build** to keep the final image small and include everything Playwright needs:

- **Base runtime (Playwright included)**  
  - `FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-jammy AS base`  
    Uses the official Microsoft Playwright image that already contains the Playwright runtime and browsers.  
  - `WORKDIR /app`  
    Sets the working directory inside the container.  
  - `ENV ASPNETCORE_URLS=http://+:8080`  
    Configures ASP.NET Core to listen on port `8080` on all interfaces.  
  - `EXPOSE 8080`  
    Documents that the container listens on port `8080` (used by Docker/compose/orchestrators).

- **Build stage**  
  - `FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build`  
    Uses the .NET 8 SDK image to restore and build the project.  
  - `ARG BUILD_CONFIGURATION=Release`  
    Allows overriding the build configuration (default `Release`).  
  - `WORKDIR /src`  
    Sets the working directory for the build.  
  - `COPY ["ExpenseManagementPdfGenerator.csproj", "./"]` and `RUN dotnet restore "ExpenseManagementPdfGenerator.csproj"`  
    Copies the project file and restores NuGet packages.  
  - `COPY . .` and `RUN dotnet build "ExpenseManagementPdfGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build`  
    Copies the full source and compiles the project into `/app/build`.

- **Publish stage**  
  - `FROM build AS publish`  
    Reuses the built artifacts to run `dotnet publish`.  
  - `RUN dotnet publish "ExpenseManagementPdfGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false`  
    Produces a self‑contained set of DLLs and dependencies in `/app/publish`, optimized for deployment.

- **Final runtime image**  
  - `FROM base AS final`  
    Starts from the Playwright runtime image (with browsers already installed).  
  - `WORKDIR /app`  
    Sets the working directory where the app will run.  
  - `COPY --from=publish /app/publish .`  
    Copies the published output from the publish stage into the runtime image.  
  - `ENTRYPOINT ["dotnet", "ExpenseManagementPdfGenerator.dll"]`  
    Defines the container entrypoint so that running the container starts the web API.

### 6.1 Build the Docker image

From the `ExpenseManagementPdfGenerator` directory:

```bash
docker build -t expensemanagementpdfgenerator .
docker run -p 5000:8080 expensemanagementpdfgenerator

```

### 6.2 Run the Docker container

Run the container and map it to a host port (example: host `5000` → container `8080`):

```bash
docker run -d ^
  --name expensemanagementpdfgenerator ^
  -p 5000:8080 ^
  -e "PdfGenerator__ApiKey=your-secure-api-key" ^
  expensemanagementpdfgenerator
```

Notes:
- Inside the container, ASP.NET Core is configured with `ASPNETCORE_URLS=http://+:8080`.
- Use **double underscores** (`__`) when setting nested configuration keys via environment variables (e.g., `PdfGenerator__ApiKey`).

### 6.3 Stop and remove the container

```bash
docker stop expensemanagementpdfgenerator
docker rm expensemanagementpdfgenerator
```

---

## 7. Local Development Setup

### 7.1 Clone and open the project

```bash
git clone <your-repo-url>
cd ExpenseManagement/ExpenseManagement/ExpenseManagementPdfGenerator
```

Open the solution/project in your preferred IDE.

### 7.2 Restore dependencies

```bash
dotnet restore
```

### 7.3 Build the project

```bash
dotnet build
```

### 7.4 Install Playwright browsers (local only)

After building once, install the Playwright browser binaries:

```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

Alternatively, you can run the generated script after build, for example on Windows:

```powershell
.\bin\Debug\net8.0\playwright.ps1 install
```

### 7.5 Configure the API key

Set the API key in one of the following ways:

- **Option 1 – `appsettings.json` (local only, do not commit real secrets)**:

```json
"PdfGenerator": {
  "ApiKey": "your-secure-api-key"
}
```

- **Option 2 – User secrets (recommended for development)**:

```bash
dotnet user-secrets init
dotnet user-secrets set "PdfGenerator:ApiKey" "your-secure-api-key"
```

- **Option 3 – Environment variable**:

```bash
set PdfGenerator__ApiKey=your-secure-api-key   # Windows CMD
$env:PdfGenerator__ApiKey="your-secure-api-key" # PowerShell
```

### 7.6 Run the application

From the project directory:

```bash
dotnet run
```

By default, the app will listen on a local HTTP/HTTPS URL (e.g., `http://localhost:5000` or `https://localhost:7000`) depending on your ASP.NET profile settings.

---

## 8. Configuration

Key configuration settings (via `appsettings.json`, user secrets, or environment variables):

- **`PdfGenerator:ApiKey`**
  - Type: `string`
  - Purpose: Shared secret required in the `X-Pdf-Api-Key` header for all PDF generation requests.
  - Do **not** commit real production keys to source control.

- **Logging**
  - `Logging:LogLevel` configuration controls log verbosity (Information, Warning, etc.).

- **Ports & URLs**
  - **Docker**: `ASPNETCORE_URLS=http://+:8080` is set in the runtime image.
  - **Local**: Standard ASP.NET Core Kestrel settings and launch profiles apply.

---

## 9. Usage

### 9.1 Swagger UI

Once the app is running (locally or inside Docker), you can explore and test the API via Swagger:

- Navigate to:  
  - `http://localhost:5000/swagger` (if mapped host port is 5000), or  
  - The appropriate base URL / port configured for your environment.

From there, you can:
- Inspect the `POST /api/pdf/generate` endpoint.
- Try sample requests directly from the browser.

### 9.2 HTTP request example

**Endpoint**

- Method: `POST`
- URL (Docker example): `http://localhost:5000/api/pdf/generate`
- Headers:
  - `Content-Type: application/json`
  - `X-Pdf-Api-Key: your-secure-api-key`

**Request body (JSON)**

```json
{
  "htmlData": [
    "PGh0bWw+PGJvZHk+PGgxPkV4cGVuc2UgUmVwb3J0PC9oMT48L2JvZHk+PC9odG1sPg=="
  ],
  "fileName": "expense-report.pdf",
  "format": "A4",
  "css": "body { font-family: Arial, sans-serif; } h1 { color: #333; }"
}
```

In this example:
- `htmlData[0]` is a **Base64-encoded** HTML string.  
  (The decoded HTML is `<html><body><h1>Expense Report</h1></body></html>`.)

### 9.3 C# client snippet (example)

```csharp
var html = "<html><body><h1>Expense Report</h1></body></html>";
var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));

var model = new HtmlToPdfModel
{
    HtmlData = new List<string> { base64 },
    FileName = "expense-report.pdf",
    Css = "body { font-family: Arial, sans-serif; }"
};

using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
httpClient.DefaultRequestHeaders.Add("X-Pdf-Api-Key", "your-secure-api-key");

var response = await httpClient.PostAsJsonAsync("api/pdf/generate", model);
response.EnsureSuccessStatusCode();

var pdfBytes = await response.Content.ReadAsByteArrayAsync();
await File.WriteAllBytesAsync("expense-report.pdf", pdfBytes);
```

---

## 10. Troubleshooting

- **401 Unauthorized – “PDF API key is not configured”**
  - Ensure `PdfGenerator:ApiKey` is set via appsettings, user secrets, or environment variable.
  - Ensure the client includes the `X-Pdf-Api-Key` header with the correct value.

- **400 Bad Request – “Request body is required.” or “HtmlData is required to generate PDF.”**
  - Verify that the JSON body is present and correctly formatted.
  - Ensure `htmlData` is a non‑empty array of Base64‑encoded strings.

- **500 Internal Server Error – “Failed to generate PDF using Playwright”**
  - Check application logs for the underlying exception.
  - Verify that Playwright browsers are installed (for local runs).
  - Confirm that the HTML you send is valid and does not reference blocked or unreachable external resources.

- **Generated PDF is empty or missing expected content**
  - Check that HTML is correctly Base64‑encoded (UTF‑8).
  - Verify that CSS does not hide content (e.g., `display:none`).
  - Remember that multi‑page behavior depends on your HTML and inserted page breaks.

- **Port conflicts**
  - If `5000` is in use on your host, change the host port mapping:

```bash
docker run -d -p 6000:8080 expensemanagementpdfgenerator
```

---

## 11. Additional Notes

- **Stateless service**  
  - The service is stateless; it does not persist any data.  
  - Safe to scale horizontally (multiple instances behind a load balancer).

- **Security**
  - Treat the API key as a secret; do not expose it in client‑side code.
  - Prefer to call this service from a backend where the key is stored securely.

- **Performance**
  - A shared browser instance (`SharedPlaywrightBrowser`) significantly reduces cold‑start and memory usage.
  - For high‑throughput scenarios, consider tuning resources and monitoring container CPU/memory.

- **Extensibility ideas**
  - Allow additional PDF options (margins, orientation, headers/footers).
  - Support more page formats using the `Format` property.
  - Add health‑check endpoints to integrate with orchestrators (Kubernetes, Docker Swarm, etc.).

This README should give new developers everything needed to **understand**, **configure**, **run**, and **integrate** the `ExpenseManagementPdfGenerator` service.