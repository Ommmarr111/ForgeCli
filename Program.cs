using Spectre.Console;
using System.Diagnostics;
using System.Text.RegularExpressions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
AnsiConsole.Write(new FigletText("FORGE CLI").Color(Color.Cyan1));

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[yellow]What do you want to do?[/]")
        .AddChoices(
            "1. Create New Web API Solution",
            "2. Add New Entity (Scaffold)",
            "3. Exit"));

var exitCode = choice switch
{
    var c when c.StartsWith("1.") => CreateNewSolution(),
    var c when c.StartsWith("2.") => await ScaffoldEntityAsync(),
    _ => 0
};

return exitCode;

// ====================================================
// Option 1: create a new solution from the 'clean-api' template
// ====================================================
static int CreateNewSolution()
{
    var projectName = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter [green]Solution Name[/] (e.g., StoreApi):")
            .Validate(n => Regex.IsMatch(n, "^[A-Za-z][A-Za-z0-9_.]*$")
                ? ValidationResult.Success()
                : ValidationResult.Error("[red]Start with a letter; use letters, digits, '.' or '_' only[/]")));

    var exitCode = -1;
    var output = "";

    try
    {
        AnsiConsole.Status().Start($"Creating {Markup.Escape(projectName)} solution...", _ =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add("new");
            psi.ArgumentList.Add("clean-api");
            psi.ArgumentList.Add("-n");
            psi.ArgumentList.Add(projectName);

            using var process = Process.Start(psi);
            if (process is null) return;

            // Read both streams before waiting, otherwise a full buffer can deadlock the process.
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            output = (stderrTask.GetAwaiter().GetResult() + stdoutTask.GetAwaiter().GetResult()).Trim();
            exitCode = process.ExitCode;
        });
    }
    catch (Exception ex)
    {
        output = ex.Message;
    }

    if (exitCode != 0)
    {
        AnsiConsole.MarkupLine("[red]✖ Failed to create the solution.[/] Is the [yellow]clean-api[/] template installed?");
        if (output.Length > 0) AnsiConsole.WriteLine(output);
        return 1;
    }

    AnsiConsole.MarkupLine($"[bold green]✔ Done![/] Solution [cyan]{Markup.Escape(projectName)}[/] created successfully.");
    AnsiConsole.MarkupLine($"Run [yellow]cd {Markup.Escape(projectName)}[/] then run [yellow]forge[/] to scaffold entities!");
    return 0;
}

// ====================================================
// Option 2: generate starter files inside the existing layers
// ====================================================
static async Task<int> ScaffoldEntityAsync()
{
    var currentDir = Directory.GetCurrentDirectory();
    var slnFiles = Directory.GetFiles(currentDir, "*.sln")
        .Concat(Directory.GetFiles(currentDir, "*.slnx"))
        .ToArray();

    if (slnFiles.Length == 0)
    {
        AnsiConsole.MarkupLine("[red]✖ Error:[/] No .sln file found here! Please run [yellow]forge[/] inside your solution root folder.");
        return 1;
    }

    var solutionName = Path.GetFileNameWithoutExtension(slnFiles[0]);
    var srcDir = Path.Combine(currentDir, "src");

    if (!Directory.Exists(srcDir))
    {
        AnsiConsole.MarkupLine("[red]✖ Error:[/] No [yellow]src[/] folder found next to the solution file.");
        return 1;
    }

    AnsiConsole.MarkupLine($"Detected Solution: [bold cyan]{Markup.Escape(solutionName)}[/]");

    var entityName = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter [green]Entity Name[/] (Singular, PascalCase, e.g., Product):")
            .Validate(n => Regex.IsMatch(n, "^[A-Z][A-Za-z0-9]*$")
                ? ValidationResult.Success()
                : ValidationResult.Error("[red]Use PascalCase letters and digits only[/]")));

    var components = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("Select components to generate (Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm):")
            .PageSize(10)
            .Required()
            .AddChoices(
                "Domain Entity",
                "EF Core Configuration",
                "DTOs & Service Interface",
                "API Controller"));

    var plural = Pluralize(entityName);
    var created = 0;
    var skipped = 0;

    async Task WriteAsync(string path, string content)
    {
        if (await WriteSafeAsync(path, content)) created++;
        else skipped++;
    }

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

        await WriteAsync(Path.Combine(dir, $"{entityName}.cs"), code);
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

        await WriteAsync(Path.Combine(dir, $"{entityName}Configuration.cs"), code);
    }

    // 3. DTOs & Service Interface
    if (components.Contains("DTOs & Service Interface"))
    {
        var dtoDir = Path.Combine(srcDir, $"{solutionName}.Application", "DTOs", plural);
        var interfaceDir = Path.Combine(srcDir, $"{solutionName}.Application", "Interfaces");
        Directory.CreateDirectory(dtoDir);
        Directory.CreateDirectory(interfaceDir);

        var dtoCode = $$"""
        namespace {{solutionName}}.Application.DTOs.{{plural}};

        public record {{entityName}}Dto(int Id, DateTime CreatedAt);
        public record Create{{entityName}}Dto();
        """;

        var interfaceCode = $$"""
        using {{solutionName}}.Application.DTOs.{{plural}};

        namespace {{solutionName}}.Application.Interfaces;

        public interface I{{entityName}}Service
        {
            Task<IEnumerable<{{entityName}}Dto>> GetAllAsync(CancellationToken cancellationToken = default);
            Task<{{entityName}}Dto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        }
        """;

        await WriteAsync(Path.Combine(dtoDir, $"{entityName}Dtos.cs"), dtoCode);
        await WriteAsync(Path.Combine(interfaceDir, $"I{entityName}Service.cs"), interfaceCode);
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
        public class {{plural}}Controller : ControllerBase
        {
            [HttpGet]
            public IActionResult GetAll()
            {
                return Ok();
            }
        }
        """;

        await WriteAsync(Path.Combine(dir, $"{plural}Controller.cs"), code);
    }

    AnsiConsole.MarkupLine($"Done: [green]{created} created[/], [yellow]{skipped} skipped[/].");
    return 0;
}

// ====================================================
// Helpers
// ====================================================

// Never overwrites an existing file. Returns true if the file was written.
static async Task<bool> WriteSafeAsync(string path, string content)
{
    var display = Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), path));

    if (File.Exists(path))
    {
        AnsiConsole.MarkupLine($"[yellow]⚠ Skipped (already exists)[/] {display}");
        return false;
    }

    await File.WriteAllTextAsync(path, content);
    AnsiConsole.MarkupLine($"[green]✔ Created[/] {display}");
    return true;
}

// Handles regular English plurals only. Irregular nouns (Person -> People) are not supported.
static string Pluralize(string name)
{
    if (name.Length > 1 && name.EndsWith('y') && !"aeiou".Contains(char.ToLowerInvariant(name[^2])))
        return name[..^1] + "ies";

    if (name.EndsWith('s') || name.EndsWith('x') || name.EndsWith('z')
        || name.EndsWith("sh", StringComparison.Ordinal)
        || name.EndsWith("ch", StringComparison.Ordinal))
        return name + "es";

    return name + "s";
}