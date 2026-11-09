# RaceDay

RaceDay is a race-event platform where organisers publish running, walking and cycling events and participants browse, enrol and view their results.

**Student:** Halle Nkosi, ST10471832
**Module:** PROG6212 Programming 2B, The IIE Rosebank College Braamfontein

## Parts of the project

| Part | What it is | Folder |
|------|------------|--------|
| Part 1 | Database design (ERD, SQL, documentation) | `st10471832 prog6212 part1/docs` |
| Part 2 | ASP.NET Core Web API (RaceDay.Api) and unit tests | `st10471832 prog6212 part1/Part2` |
| Part 3 | ASP.NET Core MVC web front end (RaceDay.Web) that consumes the API | `st10471832 prog6212 part1/Part3` |

## What the system does

**Organiser**
- Register and log in
- Dashboard showing events created, total enrolments and upcoming dates
- Create, edit and delete events
- Add and delete categories for each event
- View enrolments per event, and enter results (finish time, position, outcome)

**Participant**
- Register and log in
- Browse upcoming events and filter by name, location and type
- View event details and categories, then enrol
- My Enrolments page with colour-coded status (green Confirmed, amber Pending)
- My Results page
- View and update profile

Unauthenticated users are redirected to the login page, and each role only sees its own pages.

## How the project is structured

- `RaceDay.Api` is a REST API with controllers, models and EF Core on SQL Server LocalDB. It uses session cookies for authentication and enforces roles.
- `RaceDay.Tests` contains the xUnit tests for the API.
- `RaceDay.Web` is the MVC front end. It has no database access and no business logic of its own. Everything goes through the API.
  - `Controllers/` handle page requests (Account, Home, Events, Organiser, Categories, Enrolments, Participant)
  - `Models/` hold API response models and view models
  - `Services/ApiClient.cs` sends requests to the API and keeps the API login cookie in the MVC session
  - `Views/` are the Razor pages, with a shared layout and role-based navigation

## How to run it

You need Visual Studio with the ASP.NET workload and .NET 10 SDK, plus SQL Server LocalDB.

**1. Start the API** (it must stay running)