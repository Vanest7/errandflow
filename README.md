# ErrandFlow

**Plan errands, organize appointments, and find a more efficient route.**

ErrandFlow is a responsive, bilingual (English/Spanish) web application for creating a daily plan, adding errands at real-world locations, setting time constraints, and comparing the original visit order with an optimized itinerary. Routes can be calculated for walking or driving.

This is a **portfolio project intended to run locally**. There is no hosted demo. The Docker-based demo below is the quickest way to try it without installing the application stack manually.

## Features

- Sign in through Auth0 and manage your own plans.
- Add, edit, delete, and reorder errands.
- Search for places and addresses or select locations on an interactive map.
- Set a departure time, starting point, and optional final destination.
- Configure errands without time restrictions, with allowed time windows, or with fixed appointments.
- Calculate walking or driving routes and display their geometry on the map.
- Compare the original errand order with an optimized schedule, including time savings when both schedules are feasible.
- Use a responsive interface with English and Spanish translations.

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 22, Nx 24, TypeScript, Tailwind CSS, Angular Material, Transloco |
| Backend | C# / ASP.NET Core 10, Clean Architecture (Domain, Application, Infrastructure, API) |
| Database | MariaDB 11.8, Entity Framework Core / Pomelo |
| Authentication | Auth0 (OpenID Connect / JWT) |
| Maps | OpenLayers and OpenStreetMap |
| Place search | Photon |
| Travel-time matrices and route geometry | Valhalla |
| Tests | xUnit (.NET), Vitest (Angular) |
| Local demo | Docker Compose, Nginx, Python 3 |

## Quick start: run the local demo

### Requirements

Install only the following tools on your computer:

- **Git**, to clone the repository.
- **Docker Engine with Docker Compose v2**, or **Docker Desktop**, started and running.
- **Python 3.10 or newer**.
- An **internet connection**, both for the initial Docker image/dependency downloads and for Auth0, map tiles, place search, and routing.
- An available **port 4200** on your computer.

You **do not** need to install .NET, Node.js, Angular CLI, MariaDB, or create your own Auth0 tenant. All application services are run in containers. You will need a **review account authorized by the project owner**; login details are provided separately, never in this repository.

Check your tools:

```bash
git --version
docker --version
docker compose version
python3 --version
```

> On Windows, you can use `py -3` instead of `python3` if that is how Python is installed. Docker Desktop must be running.

### 1. Clone the repository

```bash
git clone --branch develop https://github.com/Vanest7/errandflow.git
cd errandflow
```

### 2. Check the environment

From the **repository root**, run:

```bash
python3 run_demo.py --check
```

This checks that Docker is available and that the required project files exist. It does **not** build the images or test the application itself.

### 3. Start ErrandFlow

```bash
python3 run_demo.py
```

On the first run, the script:

1. Generates local-only database and HTTPS certificate passwords.
2. Prepares the initial database schema from `backend/database/001_initial.sql`.
3. Builds the Angular/Nginx and ASP.NET Core images.
4. Starts MariaDB, the API, and the web server with Docker Compose.
5. Checks that the frontend responds and the API is reachable.
6. Opens the application in your default browser.

The first build may take longer because Docker needs to download images and dependencies.

**Open: http://localhost:4200**

### 4. Sign in and try a route

1. Select **Sign in** and use the review account provided by the project owner. No Auth0 configuration is required on your computer.
2. Create a plan, choosing a date and a departure time.
3. Set an origin and add a few errands by searching for locations or selecting points on the map.
4. Optionally assign a time window or fixed appointment to an errand.
5. Choose **Walking** or **Driving**, then calculate the route.
6. Inspect the route on the map, the suggested visit order, and the original-versus-optimized comparison.
7. Reorder an errand and recalculate the route to see how the result changes.
8. Switch between English and Spanish, or use a narrow viewport to try the mobile interface.

**Current limits:** the optimizer supports up to 10 errands, and the routing graph provider supports up to 10 distinct locations. For a first demonstration, use fewer stops.

### 5. Stop, restart, or inspect the demo

From the repository root:

```bash
# Stop and remove the demo containers without deleting database data
python3 run_demo.py --stop

# Start the demo again
python3 run_demo.py

# Show container status
python3 run_demo.py --status

# Follow container logs (press Ctrl+C to stop viewing logs)
python3 run_demo.py --logs
```

Stopping the demo **preserves the MariaDB Docker volume**, so plans and errands remain available on the next start. Do **not** run `docker compose down -v` unless you deliberately want to delete the demo's persistent data.

## Troubleshooting

| Problem                                         | What to check                                                                                                                          |
| ----------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `docker` is not found                           | Install Docker Engine or Docker Desktop and make sure it is running.                                                                   |
| Docker permission denied (Linux)                | Verify that your user has permission to use the Docker daemon or use sudo.                                                             |
| Port `4200` is already in use                   | Stop any local Angular server or other process using that port, then rerun the script.                                                 |
| `run_demo.py --check` passes, but startup fails | The check does not build images; inspect the build output and run `python3 run_demo.py --logs`.                                        |
| Login redirects incorrectly                     | Check that you are opening `http://localhost:4200` and that the project owner's Auth0 application allows that callback and web origin. |
| `401 Unauthorized` on `/api/auth/me`            | Expected when no access token is provided. Sign in through the application and try again.                                              |
| Authentication fails after sign-in              | Confirm that the review account is authorized and the Auth0 service is reachable.                                                      |
| API or database does not start                  | Inspect logs with `python3 run_demo.py --logs`; check the MariaDB health status and HTTPS certificate initialization.                  |
| Place search or route calculation returns `503` | Check internet access and whether Photon or Valhalla is temporarily unavailable or rate limited.                                       |
| Data does not appear after restart              | Check whether the existing Docker volume was removed. Never initialize the SQL baseline over an existing database.                     |

## Development without Docker (optional)

The normal development workflow is separate from the demo. It requires **.NET SDK 10**, **Node.js 22/npm**, **MariaDB 11.8**, and the appropriate Auth0 configuration and local .NET User Secrets.

Run the backend with its HTTPS launch profile:

```bash
dotnet run --project backend/src/ErrandFlow.Api --launch-profile https
```

The development API listens on **https://localhost:7094**. In a second terminal, start Angular:

```bash
cd frontend
npm ci
npm start
```

The frontend is available at **http://localhost:4200**. This development workflow uses the local API configuration rather than the Docker demo's Nginx proxy. See [`backend/database/README.md`](backend/database/README.md) for database schema requirements and [`frontend/README.md`](frontend/README.md) for frontend development details.

### Tests

With the relevant SDKs and dependencies installed locally, use:

```bash
dotnet test backend/ErrandFlow.slnx
```

```bash
cd frontend
npm run lint
npm run test:ci
npm run build:dev
```

The Docker startup builds the application images; it does not run the complete test suites. Test outcomes depend on the local environment.

## Project structure

```text
errandflow/
├── README.md
├── DEMO_SETUP.md
├── run_demo.py                  # One-command local demo launcher
├── docker-compose.demo.yml     # MariaDB, HTTPS API, and web server
├── docker/
│   ├── backend/Dockerfile
│   └── frontend/
│       ├── Dockerfile
│       ├── nginx.conf
│       └── api.config.demo.ts
├── backend/
│   ├── database/               # Versioned initial MariaDB schema
│   ├── src/
│   │   ├── ErrandFlow.Domain/          # Entities, invariants, planning algorithms
│   │   ├── ErrandFlow.Application/     # Use cases and contracts
│   │   ├── ErrandFlow.Infrastructure/  # Persistence and provider adapters
│   │   └── ErrandFlow.Api/             # Controllers and composition root
│   └── tests/                  # xUnit tests
├── frontend/                   # Angular / Nx application
```
