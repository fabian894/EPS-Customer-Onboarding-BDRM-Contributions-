# EPS-Customer-Onboarding-BDRM-Contributions-
Evaluate candidates' skills in .NET Core, Entity Framework, SQL Server, and  software architecture principles.

Develop a Pension Contribution Management System with the following features:
A. Member Management
• Register, update, retrieve, and soft-delete members.
B. Contribution Processing
• Handle Monthly Contributions (one per month) and Voluntary Contributions
(multiple per month).
• Calculate total contributions and generate statements.
• Enforce business rules (minimum contribution period before benefit eligibility).
C. Background Jobs (Hangfire)
• Validate contributions.
• Generate benefit eligibility updates and interest calculations.
• Handle failed transactions and notifications.
D. Data Validation
• Member details (Name, Date of Birth, Email, Phone, Age restrictions: 18-70).
• Contribution validation (amount > 0, valid contribution date checks).
• Employer registration (Company name, valid registration, active status).

**Setup instructions (including database schema, dependencies, and how to run locally)**
Open project on VIsual Studio or Visual Studio Code
Open terminal or package console
run the following commands:
dotnet restore
dotnet clean
dotnet build
dotnet run - it will generate the running site/localhost
for the unit and integration tests, run
**dotnet test EPSPlus.Tests
dotnet test EPSPlus.IntegrationTests**

If running on Visual Studio IDE
there is already a build and clean option on the upper side menu
then execute the project by clicking on the IIS Express with the green play icon and the swagger UI will open
then you can test the endpoints on the swagger UI according to the swaggerJSON API documentation being provided


**Design Patterns Used**
To ensure a scalable, maintainable, and testable architecture, i incorporated the following design patterns:

i. Mediator Pattern (via MediatR)
Reduces direct dependencies between handlers and controllers.
Helps in making the code modular and scalable.
ii. Repository Pattern (Simulated)
i kept data access within the Application DBcontext.
If needed, this was extended into a full repository layer for better abstraction.
iii. Seperation of Concern
 Service, Interface, Repository was implemented to improve project performance, readability, maintainablity

iv. Logging/Debugging: Implemented logging using a tool like Serilog, with output to a file in logs/app.log in the file directory. Check the program.cs for the configuration implementation


**Testing Strategy**
I implemented unit and integration tests using xUnit, Moq, FluentAssertions, and EF Core In-Memory:

Why Use Redis for Caching?
Faster Read Operations: Queries return data instantly without hitting the database.
Reduced Database Load: Minimizes redundant queries.
Optimized Performance: Ideal for GetAllTasks and GetTaskById.

