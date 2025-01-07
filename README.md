TICKETECHY 2024
Welcome to TickeTechy 2024, a robust Help Desk Ticketing System designed to streamline issue tracking and resolution processes. This document will guide you through setting up the project on your local machine using ASP.NET Core and Entity Framework.

Table of Contents
Prerequisites
Project Structure
Setup Instructions
1. Clone the Repository
2. Configure Database Connection
3. Build and Run the Project
License

.NET SDK: Version 6.0
SQL Server: For the database
Visual Studio: With ASP.NET and web development workload installed
Entity Framework Core Tools
Project Structure
The project follows a modular structure to ensure scalability and maintainability:

Data: Contains database models and the DbContext class.
Services: Includes business logic and data processing layers.
Resources: Manages shared static assets like CSS, JS, and images.
WebApp: The main ASP.NET Core MVC application, responsible for the user interface.
Setup Instructions
1. Clone the Repository
Clone the project repository from GitHub:
https://github.com/Kus07/ASIJumpstart2024-Group4-TickeTechy.git

2. Configure Database Connection
Make sure your have backed up the DB to your server (the bak file of our database is in the root folder).
Add your SQL Server connection string under the ConnectionStrings section:
json
Copy code
"ConnectionStrings": {  
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=AllianceJumpstartDB;Trusted_Connection=True;MultipleActiveResultSets=true"  
}  
Note: Replace YOUR_SERVER_NAME with your SQL Server instance name.

3. Build and Run the Project
Open the solution in Visual Studio.
Set the WebApp project as the startup project.
Build the solution using Ctrl+Shift+B or the "Build" menu.
Run the application by pressing F5 or clicking the "Run" button.


License
TickeTechy 2024 is open-source and distributed under the MIT License. See LICENSE for more information.

