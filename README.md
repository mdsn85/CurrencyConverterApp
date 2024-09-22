# Currency Converter Application

This Currency Converter application allows users to convert amounts between different currencies, fetch the latest exchange rates, and retrieve historical exchange rates with pagination. The application interacts with the [Frankfurter API](https://www.frankfurter.app/) for real-time currency data and uses in-memory caching to enhance performance. Built using .NET 8, the application demonstrates robust handling of API interactions, caching, and error management.

## Features

- **Currency Conversion**: Convert amounts between supported currencies, with validation for excluded currencies such as TRY, PLN, THB, and MXN.
- **Latest Exchange Rates**: Fetch real-time exchange rates based on a specified base currency.
- **Historical Rates with Pagination**: Retrieve historical exchange rates within a specified date range, with support for pagination.
- **Caching for Optimal Performance**: In-memory caching is used to store frequently requested data, reducing API calls and improving performance. Future enhancement plans include adding Redis for multi-node deployments.
- **IHttpClientFactory Usage**: Leveraging IHttpClientFactory to manage HTTP clients efficiently and enhance performance and resilience.
- **Unit Testing**: Extensive unit tests covering scenarios like caching, API response handling, and error management.

## Prerequisites

To run the application, ensure the following are installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) or any compatible IDE
- [Git](https://git-scm.com/) (for version control)
- A valid internet connection to access the Frankfurter API

## Running the Application

### 1. Clone the Repository

Start by cloning the repository to your local environment:

- git clone https://github.com/your-username/currency-converter-app.git
- cd currency-converter-app
- dotnet restore
- dotnet build
- dotnet run

## 4. API Endpoints
You can use tools like Postman or Swagger UI to interact with the API.
Convert Currency:
GET /convertCurrency?fromCurrency=USD&toCurrency=EUR&amount=100
Get Latest Rates:
GET /getLatestRates?baseCurrency=USD
Get Historical Rates:
GET /getHistoricalRates?baseCurrency=USD&startDate=2022-01-01&endDate=2022-01-31&pageNumber=1&pageSize=10

## Assumptions
Caching Strategy: The application uses in-memory caching to improve response times.
Redis can be integrated in the future for distributed caching when running on multiple servers.
