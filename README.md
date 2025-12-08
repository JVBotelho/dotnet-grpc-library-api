# 📚 Library System - Enterprise .NET 10 Microservices

![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![gRPC](https://img.shields.io/badge/gRPC-244c5a?style=for-the-badge&logo=grpc&logoColor=white)

A high-performance, distributed library management system designed to demonstrate **Modern Software Architecture**
principles.

This project goes beyond simple CRUD, implementing **Domain-Driven Design (DDD)**, **CQRS**, and **Clean Architecture**
to solve complex business rules (inventory management, lending logic, and historical analytics) within a distributed *
*gRPC** environment.

---

## 🏗️ Architecture & Design Patterns

The solution is split into two main services communicating via high-performance RPC:

1. **Library.Api (Gateway):** A thin REST API acting as a Backend-for-Frontend (BFF). It handles HTTP requests,
   validation, and forwards commands to the core via gRPC.
2. **Library.Grpc (Core):** The heart of the system. It encapsulates the Domain and Application layers, manages the
   Database, and executes business logic.

### Key Concepts Applied

* **Clean Architecture:** Strict dependency rule (Domain <- Application <- Infrastructure <- Presentation).
* **Domain-Driven Design (DDD):** Rich Domain Models (`Book`, `LendingActivity`) enforce invariants. No anemic models
  allowed.
* **CQRS (Command Query Responsibility Segregation):** Implemented using **MediatR**. Writes (Commands) and Reads (
  Queries) are handled separately for scalability.
* **Vertical Slices:** Features are organized by Use Cases (e.g., `BorrowBook`, `GetMostBorrowed`) rather than technical
  layers.
* **gRPC Code-First:** Strongly typed contracts defined in `.proto` files shared between services.
* **Shift-Left Quality:** Heavy emphasis on unit testing domain logic and integration testing API contracts.

---

## 🚀 Tech Stack

* **Runtime:** .NET 10 (Preview/LTS)
* **Communication:** gRPC (HTTP/2) & REST (HTTP/1.1)
* **Data:** PostgreSQL 16
* **ORM:** Entity Framework Core (Code-First with Fluent API)
* **Mediation:** MediatR
* **Testing:** xUnit, FluentAssertions, Moq, AutoFixture
* **Containerization:** Docker & Docker Compose

---

## 🛠️ Getting Started

### Prerequisites

* Docker & Docker Compose

### Running the Application

You don't need .NET installed to run the system. Docker handles everything.

1. **Clone and Start:**
   ```bash
   docker-compose up --build
   ```

2. **Access the System:**
    * **Swagger UI (API):** `http://localhost:5000/swagger`
    * **API Internal URL:** `http://localhost:5000`
    * **gRPC Service:** `http://localhost:5001` (Internal Docker Network)

   *Note: The system automatically seeds the database with sample books and historical lending data on startup.*

---

## 🧪 Running Tests

We prioritize **Developer Experience**. You can run the entire test suite (Unit + Integration) inside a container
without setting up a local environment.

### Test Strategy

* **Domain Tests:** Verify complex business rules (e.g., "Cannot borrow if copies < 1") in isolation.
* **Application Tests:** Verify the orchestration of Use Cases and Repository calls using Mocks.
* **Integration Tests:** Verify the API Gateway correctly maps HTTP requests to gRPC calls using `WebApplicationFactory`
  and gRPC Mocks.

### Command to Run Tests

```bash
# Windows / Linux / Mac
docker-compose -f docker-compose.tests.yml up --build --abort-on-container-exit