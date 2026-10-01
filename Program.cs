using Spectre.Console;
using System.Diagnostics;
Console.OutputEncoding = System.Text.Encoding.UTF8;
AnsiConsole.Write(new FigletText("FORGE CLI").Color(Color.Cyan1));

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[yellow]What do you want to do?[/]")
        .AddChoices(
            "1. Create New Web API Solution",
            "2. Add New Entity (Scaffold)",
            "3. Exit"));

if (choice.StartsWith("1."))
{
    CreateNewSolution();
}
else if (choice.StartsWith("2."))
{
    await ScaffoldEntityAsync();
}

// ====================================================
// الوظيفة الأولى: إنشاء Solution جديد من الـ Template
// ====================================================
static void CreateNewSolution()
{
    var projectName = AnsiConsole.Ask<string>("Enter [green]Solution Name[/] (e.g., StoreApi):");

    AnsiConsole.Status()
        .Start($"Creating {projectName} solution...", _ =>
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new clean-api -n {projectName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            process?.WaitForExit();
        });

    AnsiConsole.MarkupLine($"[bold green]✔ Done![/] Solution [cyan]{projectName}[/] created successfully.");
    AnsiConsole.MarkupLine($"Run [yellow]cd {projectName}[/] then run [yellow]forge[/] to scaffold entities!");
}

// ====================================================
// الوظيفة التانية: توليد الكود جوه الـ Layers أوتوماتيك
// ====================================================
static async Task ScaffoldEntityAsync()
{
    var currentDir = Directory.GetCurrentDirectory();
    var slnFiles = Directory.GetFiles(currentDir, "*.sln")
        .Concat(Directory.GetFiles(currentDir, "*.slnx"))
        .ToArray();
    if (slnFiles.Length == 0)
    {
        AnsiConsole.MarkupLine("[red]✖ Error:[/] No .sln file found here! Please run [yellow]forge[/] inside your solution root folder.");
        return;
    }

    var solutionName = Path.GetFileNameWithoutExtension(slnFiles[0]);
    AnsiConsole.MarkupLine($"Detected Solution: [bold cyan]{solutionName}[/]");

    var entityName = AnsiConsole.Ask<string>("Enter [green]Entity Name[/] (Singular, e.g., Product):");

    var components = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("Select components to generate (Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm):")
            .PageSize(10)
            .AddChoices(
                "Domain Entity",
                "EF Core Configuration",
                "DTOs & Service Interface",
                "API Controller"));

    var srcDir = Path.Combine(currentDir, "src");

    // 1. Domain Entity
    if (components.Contains("Domain Entity"))
    {
        var dir = Path.Combine(srcDir, $"{solutionName}.Domain", "Entities");
        Directory.CreateDirectory(dir);

        var code = $$"""
        namespace {{solutionName}}.Domain.Entities;

        public class {{entityName}}
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(dir, $"{entityName}.cs"), code);
        AnsiConsole.MarkupLine($"[green]✔ Created[/] src/{solutionName}.Domain/Entities/{entityName}.cs");
    }

    // 2. EF Core Configuration
    if (components.Contains("EF Core Configuration"))
    {
        var dir = Path.Combine(srcDir, $"{solutionName}.Infrastructure", "Persistence", "Configurations");
        Directory.CreateDirectory(dir);

        var code = $$"""
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using {{solutionName}}.Domain.Entities;

        namespace {{solutionName}}.Infrastructure.Persistence.Configurations;

        public class {{entityName}}Configuration : IEntityTypeConfiguration<{{entityName}}>
        {
            public void Configure(EntityTypeBuilder<{{entityName}}> builder)
            {
                builder.HasKey(x => x.Id);
            }
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(dir, $"{entityName}Configuration.cs"), code);
        AnsiConsole.MarkupLine($"[green]✔ Created[/] src/{solutionName}.Infrastructure/Persistence/Configurations/{entityName}Configuration.cs");
    }

    // 3. DTOs & Service Interface
    if (components.Contains("DTOs & Service Interface"))
    {
        var dtoDir = Path.Combine(srcDir, $"{solutionName}.Application", "DTOs", $"{entityName}s");
        var interfaceDir = Path.Combine(srcDir, $"{solutionName}.Application", "Interfaces");
        Directory.CreateDirectory(dtoDir);
        Directory.CreateDirectory(interfaceDir);

        var dtoCode = $$"""
        namespace {{solutionName}}.Application.DTOs.{{entityName}}s;

        public record {{entityName}}Dto(int Id, DateTime CreatedAt);
        public record Create{{entityName}}Dto();
        """;

        var interfaceCode = $$"""
        using {{solutionName}}.Application.DTOs.{{entityName}}s;

        namespace {{solutionName}}.Application.Interfaces;

        public interface I{{entityName}}Service
        {
            Task<IEnumerable<{{entityName}}Dto>> GetAllAsync(CancellationToken cancellationToken = default);
            Task<{{entityName}}Dto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(dtoDir, $"{entityName}Dtos.cs"), dtoCode);
        await File.WriteAllTextAsync(Path.Combine(interfaceDir, $"I{entityName}Service.cs"), interfaceCode);
        AnsiConsole.MarkupLine($"[green]✔ Created[/] DTOs and I{entityName}Service in Application layer");
    }

    // 4. API Controller
    if (components.Contains("API Controller"))
    {
        var dir = Path.Combine(srcDir, $"{solutionName}.Api", "Controllers");
        Directory.CreateDirectory(dir);

        var code = $$"""
        using Microsoft.AspNetCore.Mvc;

        namespace {{solutionName}}.Api.Controllers;

        [ApiController]
        [Route("api/[controller]")]
        public class {{entityName}}sController : ControllerBase
        {
            [HttpGet]
            public IActionResult GetAll()
            {
                return Ok();
            }
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(dir, $"{entityName}sController.cs"), code);
        AnsiConsole.MarkupLine($"[green]✔ Created[/] src/{solutionName}.Api/Controllers/{entityName}sController.cs");
    }
}