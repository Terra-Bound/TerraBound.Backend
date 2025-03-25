# ASP.NET Template with OpenTelemetry, PostgreSQL, and Svelte Frontend

## Overview
This is an ASP.NET Core 8 Backend with OpenTelemetry integration for collecting metrics, traces, and logs. 
The collected data is sent to an OpenTelemetry Collector, which forwards it to Prometheus, Tempo, and Loki. Grafana is used for visualization.
The Backend features a basic authentication using AspNet.Identity with an integrated Email-Sender. 

The backend includes:
- **Swagger** for API documentation.
- **Svelte frontend** located in the `/Frontend` folder, built via `npm build`, and automatically placed in `wwwroot`.
- **PostgreSQL** as the database with ASP.NET Identity.
- **EmailSender** configurable via `appsettings.json`.
- **Authentication** configurable in code.
- **Mapperly** for mapping DTOs to Entities and vice versa.

All services run in the same **Docker Compose** environment using standard ports.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js & npm](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/)

## Setup & Build
### 1. Clone the Repository
```sh
git clone https://github.com/your-repo.git
cd your-repo
```
## 2. Configuration
Modify `appsettings.json` or environment variables for:
- **Database Connection** (`ConnectionStrings:DefaultConnection`)
- **Email Sender Settings** (`EmailSender` section)
As well as grafana, Otel-collector, tempo, Loki and prometheus config-files at the project root.

### 3. Build & Run the Project
#### Using Docker Compose
```sh
docker-compose up --build
```
This will:
- Build and start the ASP.NET Core backend.
- Build the Svelte frontend and place it in `wwwroot`.
- Start PostgreSQL, OpenTelemetry Collector, Prometheus, Tempo, Loki, and Grafana.

#### Running Backend Locally (Without Docker)
1. **Start PostgreSQL** manually and update connection strings in `appsettings.json`.
2. **Build & run the backend**
   ```sh
   dotnet build
   dotnet run
   ```
3. **Build the Svelte frontend**
   ```sh
   cd Frontend
   npm install
   npm run build
   ```
4. **Integrate Svelte into Asp.Net**
   ```sh
   copy dist ../wwwroot
   ```
   
#### Running Frontend Locally (Without Docker)
1. **Build the Svelte frontend (Node 20 required)**
   ```sh
   cd Frontend
   npm install
   npm run build
   npm run dev
   ```

## Logging, Tracing & Metrics
The backend sends:
- **Logs** to Loki
- **Traces** to Tempo
- **Metrics** to Prometheus

Grafana reads from Prometheus, Tempo, and Loki for visualization.
Login to Grafana, add Prometheus, Tempo and Loki as Datasources and use an Otel-Dashboard of your choice.
However, the deployed grafana does not automatically display the dashboard and the datasources. You simply have to add them, import an AspNet-Otel dashboard of your choice and you're done!
Since grafana is deployed in docker (like the other services) you may not be able to address it directly via grafana's localhost but have to address it using its container name.

### Visualisation in grafana
1. Deploy the backend using docker compose.
2. Login to grafana http://localhost:3000 using the credentials set in the docker compose file. 
3. Add Prometheus, Tempo, Loki as Datasources
4. Import a AspNet-Otel-Dashboard of your choice (or the one included in this project)

## Accessing Services
| Service                   | URL                                    |
|---------------------------|----------------------------------------|
| API (Swagger)             | http://localhost:8080/swagger          |
| Svelte - Frontend         | http://localhost:8080                  |
| Svelte - Request Password | http://localhost:8080/request-password |
| Grafana                   | http://localhost:3000                  |
| Prometheus                | http://localhost:9090                  |
| Tempo (Traces)            | http://localhost:3200                  |
| Loki (Logs)               | http://localhost:3100                  |

## Contributing
Feel free to submit pull requests or open issues!

## License
Apache2.0 License

