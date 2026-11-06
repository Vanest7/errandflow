# Errandflow

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.5.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## OpenAPI client

The typed Angular client lives in `src/app/core/api/generated/`. It is generated
from the committed OpenAPI snapshot in `openapi/errandflow.json` and must not be
edited by hand.

Start the backend in Development mode and regenerate the snapshot and client:

```bash
npm run api:update
```

To regenerate only from the existing snapshot:

```bash
npm run api:generate
```

The snapshot command uses `curl -k` because the local ASP.NET Core development
certificate may not be trusted. Do not use that flag against a remote API.

The backend base URL is defined once in `src/app/core/config/api.config.ts` and
provided to the generated `ApiConfiguration`.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
