# 🏡 Funda Backend Assignment

This project is a solution to the backend assignment from Funda. The task was to determine which real estate agents (`makelaars`) in Amsterdam have the most properties for sale, and also which ones have the most listings with a garden (`tuin`).


## 🧠 Features

- ✅ Fetches listings from the Funda public API using Refit
- ✅ Supports pagination with robust retry and backoff (Polly)
- ✅ Prevents exceeding API rate limits with throttling
- ✅ Fully configurable (search path, top count)
- ✅ Clean separation of concerns with DI
- ✅ Structured logging via `ILogger`
- ✅ Testable design with unit tests using Moq + xUnit


## 📊 Output Example
Top 10 agents listings in Amsterdam
- Makelaarsgroep Amsterdam - 44 listings
- Agent X Makelaardij - 41 listings

## ⚙️ How to Run

1. Set environment to development:

DOTNET_ENVIRONMENT=Development dotnet run

2. Make sure appsettings.Development.json is present and contains:
{
  "FundaApi": {
    "ApiKey": "76666a29898f491480386d966b7******",
    "ApiEndpoint": "https://partnerapi.funda.nl/feeds/Aanbod.svc/json"
  }
}

3. Run the app:
dotnet run --project Funda

## 🧪 How to Run Tests
dotnet test Funda.Tests


## 🧰 Stack
- .NET 8  ->  Runtime
- Refit	Type-safe ->  API client
- Polly ->	Retry and backoff logic
- Moq + xUnit	-> Unit testing
- Microsoft.Extensions.Logging ->	Structured logging

## 🤖 AI Usage
  AI (ChatGPT) was used to assist with:
- Design discussions (rate-limiting, retry logic)
- Test scaffolding using Moq
- README formatting

All architectural decisions and code implementations were reviewed and written by me. AI was used strictly as a copilot, not as an autopilot.


## 📌 Notes
- API rate limits are strict — retries and backoff are essential.

- The Funda API uses 401 instead of 429 for throttling — this is handled accordingly.