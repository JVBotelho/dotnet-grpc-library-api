# Library System API

This project is a robust, API-driven application for a library to track its books, borrowers, and lending activity. It is designed with a clean, layered architecture and modern .NET practices, fully containerized with Docker for easy setup and execution.

## Features

The API provides key business insights through the following functionalities:

* **Book Management:** Full CRUD (Create, Read, Update, Delete) operations for books.
* **Lending Management:** Endpoints to create a loan and return a book.
* **Advanced Analytics:**
    * Get the most borrowed books.
    * Check the current availability of a specific book (borrowed vs. available copies).
    * Identify the most active users within a given time frame.
    * Retrieve the complete borrowing history for a specific user in a given period.
    * Discover "also borrowed" books (recommendations based on other users' activity).
    * Estimate the average reading rate (pages/day) for a book based on historical data.

## Tech Stack & Key Concepts

* **Backend:** C#, .NET 8
* **Framework:** ASP.NET Core Web API
* **Internal Communication:** gRPC for high-performance RPC between the API and Service layers.
* **Database:** PostgreSQL
* **ORM:** Entity Framework Core (Code-First approach)
* **Testing:**
    * **Unit Tests:** xUnit, Moq, AutoFixture
    * **Integration Tests:** xUnit, `WebApplicationFactory`, Testcontainers
* **Containerization:** Docker, Docker Compose
* **Architecture:** Clean, layered architecture with a clear separation of concerns (API, Service, Persistence).

## Architectural Decisions

* **Layered Architecture:** The solution is structured into distinct layers (API, gRPC Service, Persistence) to ensure a clean separation of concerns, making the application more maintainable and scalable.
* **gRPC for Internal Services:** As required, gRPC is used for communication between the API and the service layer. This provides a strongly-typed contract (`.proto` file), high performance, and prepares the system for a potential future migration to a microservices architecture.
* **Testcontainers for Integration Tests:** To ensure reliable and isolated integration tests, the project uses Testcontainers. This library programmatically spins up a real, disposable PostgreSQL container for each test run, eliminating test pollution and guaranteeing that tests run against a clean, consistent database environment.

## Future Improvements / Roadmap

This project serves as a foundation. The following improvements are planned to elevate it to a production-grade enterprise application:

1.  **Refactor to Clean Architecture:**
    * **Goal:** Decouple the core business logic from external concerns like gRPC and Entity Framework.
    * **Plan:** Introduce a central `Application` layer containing use cases (interactors) and domain-agnostic interfaces (`IRepository`). The gRPC service would then become a simple adapter that calls these use cases, making the business logic reusable across different delivery mechanisms (e.g., message queues, other RPC frameworks).

2.  **Enrich the Domain Model (DDD):**
    * **Goal:** Move from an anemic domain model (entities as data bags) to a rich domain model where entities enforce their own invariants.
    * **Plan:** Encapsulate business logic within the domain entities themselves. For example, the `Book` entity could have a `BorrowCopy()` method that contains the logic to check for availability, throwing a `BookNotAvailableException` if no copies are left. This centralizes business rules and makes the system more robust.

3.  **Implement Robust Validation:**
    * **Goal:** Centralize and strengthen input validation.
    * **Plan:** Integrate `FluentValidation` to create dedicated validator classes for all incoming DTOs and requests. This will replace scattered `if` checks with a declarative, reusable, and easily testable validation pipeline that runs automatically.

4.  **Add Security (Authentication & Authorization):**
    * **Goal:** Secure the API endpoints to ensure that only authorized users (e.g., librarians) can perform actions.
    * **Plan:** Implement JWT (JSON Web Token) based authentication. This would involve creating `Auth` endpoints for login and token generation. Subsequently, apply authorization policies (`[Authorize]`) to the API controllers to protect the endpoints.

## Getting Started

### Prerequisites

* Docker and Docker Compose
* .NET 8 SDK (for running tests locally)

### How to Run the Application

1.  **Start the Application and Database**

    To start the API and the database, run the following command in the project root:

    ```bash
    docker-compose up --build
    ```

2.  **Access the API**

    The API will be available at the following addresses:

    * **HTTP:** `http://localhost:8080`
    * **Swagger UI:** `http://localhost:8080/swagger`

## How to Run the Tests (Locally)

The integration tests depend on Docker to start a temporary database, ensuring isolation.

1.  **Restore Dependencies**

    Open a terminal in the project root and run:

    ```bash
    dotnet restore
    ```

2.  **Run Tests**

    With Docker Desktop running, execute the following command to run all unit and integration tests:

    ```bash
    dotnet test
    ```

    This command will compile the test projects and run all tests found in the solution.