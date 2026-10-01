# 🛠️ Forge CLI

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)

Scaffold Clean Architecture ASP.NET Core Web APIs from the terminal.

Forge creates a layered solution from the `clean-api` template, then generates per-entity starter files (Entity, EF Core configuration, DTOs, Service interface, Controller) inside it.

<!-- TODO: add a short terminal GIF (VHS or asciinema) of: forge -> Add New Entity -> Subscription -->

## 📋 Requirements

* **.NET 9 SDK**
* The `clean-api` template. This is required for *Option 1 (Create New Web API Solution)*. 
  To install it from this repository, run:
  ```bash
  dotnet new install ./CleanApiTemplate
  ```

## 📦 Installation

```bash
git clone [https://github.com/Ommmarr111/ForgeCli.git](https://github.com/Ommmarr111/ForgeCli.git)
cd ForgeCli

# Pack the tool into a NuGet package
dotnet pack -c Release

# Install globally from the local output directory
dotnet tool install --global --add-source ./nupkg ForgeCli
```

**Update or uninstall:**
```bash
dotnet tool update --global --add-source ./nupkg ForgeCli
dotnet tool uninstall --global ForgeCli
```

## 💻 Usage

Run `forge` from your terminal:

```text
What do you want to do?
> 1. Create New Web API Solution
  2. Add New Entity (Scaffold)
  3. Exit
```

### 1. Create a Solution
Run this in an empty directory. Enter a solution name (for example, `GymManagement`). Forge will run `dotnet new clean-api -n GymManagement` behind the scenes.

### 2. Add an Entity
Run this from the solution root (the folder containing the `.sln` or `.slnx` file):
```bash
cd GymManagement
forge
```
Choose **Add New Entity (Scaffold)**, enter a singular entity name in PascalCase (for example, `Subscription`), and select the components to generate. Namespaces are automatically extracted from the solution file name, and files are written under the `src/` directory.

## 🏗️ What Gets Generated

For the entity `Subscription` in the solution `GymManagement`:

| Component | Output Path |
| :--- | :--- |
| **Domain Entity** | `src/GymManagement.Domain/Entities/Subscription.cs` |
| **EF Core Configuration** | `src/GymManagement.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs` |
| **DTOs & Service Interface** | `src/GymManagement.Application/DTOs/Subscriptions/SubscriptionDtos.cs` <br> `src/GymManagement.Application/Interfaces/ISubscriptionService.cs` |
| **API Controller** | `src/GymManagement.Api/Controllers/SubscriptionsController.cs` |

### ✨ Example Output

```csharp
// Domain/Entities/Subscription.cs
namespace GymManagement.Domain.Entities;

public class Subscription
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// Application/Interfaces/ISubscriptionService.cs
using GymManagement.Application.DTOs.Subscriptions;

namespace GymManagement.Application.Interfaces;

public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
```

```csharp
// Api/Controllers/SubscriptionsController.cs
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok();
}
```
*(The EF configuration automatically sets the primary key on `Id`. The DTO file contains `SubscriptionDto(int Id, DateTime CreatedAt)` and an empty `CreateSubscriptionDto`.)*

## 🏛️ Layers

| Layer | Responsibility |
| :--- | :--- |
| **Domain** | Entities and core business models |
| **Application** | DTOs, interfaces, and service abstractions |
| **Infrastructure** | EF Core persistence configurations and external services |
| **Api** | HTTP endpoints and controllers |

<!-- TODO: add a dependency diagram after checking the project references in the clean-api template. -->

## ⚠️ Scope and Limitations

Forge safely writes starter files (skipping existing files to prevent overwriting your work). It uses smart English pluralization (e.g., `Category` becomes `Categories`). 

However, it **does not** currently:
- Add a `DbSet<T>` to your `DbContext` or create EF migrations.
- Register configurations or services in the Dependency Injection container (`Program.cs` / `DependencyInjection.cs`).
- Implement the service interface or connect the controller to it.

*The CLI is currently interactive only.*

## 🤝 Contributing
Issues and pull requests are welcome! If you want to help add auto-registration capabilities, feel free to contribute.

## 📄 License
MIT. See `LICENSE` for details.
