using Microsoft.Extensions.Logging;
using Moq;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Shouldly;
using Xunit;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Integration tests for the complete algorithm-safety workflow.
/// Validates the complete workflow from package detection through safety validation,
/// ensuring the algorithm correctly identifies packages that are safe vs unsafe to remove.
/// Tests RFC-0002 + RFC-0004 integration scenarios.
/// </summary>
public partial class PackageUsageAnalyzerTests
{
    [Fact]
    public async Task CompleteWorkflow_WithMixedPackageUsage_ShouldCorrectlyClassifyForSafeRemoval()
    {
        // Arrange - Complete workflow test: RFC-0002 detection + RFC-0004 safety
        var usedPackage = CreateTestPackageReference("Used.Package", "1.0.0");
        var unusedPackage = CreateTestPackageReference("Unused.Package", "2.0.0");
        var conditionalPackage = CreateTestPackageReference(
            "Conditional.Package",
            "3.0.0",
            condition: "'$(Configuration)' == 'Debug'"
        );
        var transitivePackage = CreateTestPackageReference(
            "Transitive.Package",
            "4.0.0",
            isDirect: false
        );

        var project = CreateTestProject(
            "IntegrationProject",
            "net9.0",
            usedPackage,
            unusedPackage,
            conditionalPackage,
            transitivePackage
        );
        var solution = CreateTestSolution("IntegrationSolution", project);

        // Setup source files with realistic usage patterns
        SetupMockSourceFiles(
            project,
            "/test/BusinessLogic.cs",
            "/test/DataAccess.cs",
            "/test/Utilities.cs"
        );

        SetupMockFileContent(
            "/test/BusinessLogic.cs",
            @"
            using Used.Package;
            using System;
            
            namespace MyApp.Business
            {
                public class BusinessService
                {
                    public void ProcessData()
                    {
                        var processor = new Used.Package.DataProcessor();
                        processor.Process();
                    }
                }
            }"
        );

        SetupMockFileContent(
            "/test/DataAccess.cs",
            @"
            using System.Data;
            using System.Collections.Generic;
            
            namespace MyApp.Data
            {
                public class Repository
                {
                    public List<T> GetAll<T>() => new List<T>();
                }
            }"
        );

        SetupMockFileContent(
            "/test/Utilities.cs",
            @"
            using System;
            using System.Linq;
            
            namespace MyApp.Utils
            {
                public static class Helper
                {
                    public static void DoSomething() => Console.WriteLine(""Helper"");
                }
            }"
        );

        // Setup package namespaces
        SetupMockPackageNamespaces("Used.Package", "1.0.0", "net9.0", "Used.Package");
        SetupMockPackageNamespaces("Unused.Package", "2.0.0", "net9.0", "Unused.Package");
        SetupMockPackageNamespaces("Conditional.Package", "3.0.0", "net9.0", "Conditional.Package");
        SetupMockPackageNamespaces("Transitive.Package", "4.0.0", "net9.0", "Transitive.Package");

        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act - Complete analysis workflow
        var validationResult = await _analyzer.ValidateInputsAsync(solution);
        validationResult.IsValid.ShouldBeTrue();

        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert - RFC-0004 Safety Classification for Removal

        // Used package - SAFE TO KEEP (should not be removed)
        usedPackage.IsUsed.ShouldBeTrue();
        usedPackage.UsageLocations.ShouldContain("/test/BusinessLogic.cs");

        // Unused direct package - CANDIDATE FOR REMOVAL (safe to remove)
        unusedPackage.IsUsed.ShouldBeFalse();
        unusedPackage.IsDirect.ShouldBeTrue();

        // Conditional package - REQUIRES SPECIAL HANDLING (conditional removal)
        conditionalPackage.IsUsed.ShouldBeFalse();
        conditionalPackage.Condition.ShouldNotBeNull();

        // Transitive package - NOT DIRECTLY REMOVABLE (managed by package manager)
        transitivePackage.IsUsed.ShouldBeFalse();
        transitivePackage.IsDirect.ShouldBeFalse();

        // Solution-level safety analysis
        var safeToRemove = solution
            .GetAllUnusedPackages()
            .Where(p => p.IsDirect && p.Condition == null)
            .ToList();
        var requiresSpecialHandling = solution
            .GetAllUnusedPackages()
            .Where(p => p.Condition != null)
            .ToList();
        var managedByPackageManager = solution
            .GetAllUnusedPackages()
            .Where(p => !p.IsDirect)
            .ToList();

        safeToRemove.ShouldContain(unusedPackage);
        requiresSpecialHandling.ShouldContain(conditionalPackage);
        managedByPackageManager.ShouldContain(transitivePackage);
    }

    [Fact]
    public async Task CompleteWorkflow_WithCentralPackageManagement_ShouldHandleSafely()
    {
        // Arrange - RFC-0004: Central package management safety considerations
        var centralPackage = CreateTestPackageReference("Central.Package", "1.0.0");
        var project = CreateTestProject("CentralProject", "net9.0", centralPackage);
        var solution = CreateTestSolution("CentralSolution", project);

        // Enable central package management
        solution.EnableCentralPackageManagement("/test/Directory.Packages.props");

        SetupMockSourceFiles(project, "/test/Service.cs");
        SetupMockFileContent(
            "/test/Service.cs",
            "using System;\nConsole.WriteLine(\"No central package usage\");"
        );
        SetupMockPackageNamespaces("Central.Package", "1.0.0", "net9.0", "Central.Package");
        SetupMockPathExists(
            solution.FilePath,
            project.FilePath,
            project.DirectoryPath,
            "/test/Directory.Packages.props"
        );

        // Act
        var validationResult = await _analyzer.ValidateInputsAsync(solution);
        validationResult.IsValid.ShouldBeTrue();

        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert - RFC-0004: Central package management requires special removal handling
        centralPackage.IsUsed.ShouldBeFalse();
        solution.CentralPackageManagementEnabled.ShouldBeTrue();
        solution.DirectoryPackagesPropsPath.ShouldBe("/test/Directory.Packages.props");

        // Central packages require different removal strategy
        var unusedCentralPackages = solution.GetAllUnusedPackages().ToList();
        unusedCentralPackages.ShouldContain(centralPackage);
    }

    [Fact]
    public async Task CompleteWorkflow_WithBuildValidationScenario_ShouldIdentifyBuildCriticalPackages()
    {
        // Arrange - RFC-0004: Build validation scenario - packages that might be build-critical
        var buildToolPackage = CreateTestPackageReference(
            "Microsoft.CodeAnalysis.Analyzers",
            "3.3.4"
        );
        var runtimePackage = CreateTestPackageReference("Runtime.Package", "1.0.0");

        var project = CreateTestProject("BuildProject", "net9.0", buildToolPackage, runtimePackage);
        var solution = CreateTestSolution("BuildSolution", project);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent(
            "/test/Program.cs",
            @"
            using Runtime.Package;
            using System;
            
            namespace MyApp
            {
                public class Program
                {
                    public static void Main()
                    {
                        var service = new Runtime.Package.Service();
                        service.Execute();
                    }
                }
            }"
        );

        SetupMockPackageNamespaces(
            "Microsoft.CodeAnalysis.Analyzers",
            "3.3.4",
            "net9.0",
            "Microsoft.CodeAnalysis"
        );
        SetupMockPackageNamespaces("Runtime.Package", "1.0.0", "net9.0", "Runtime.Package");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert - RFC-0004: Different safety levels for different package types
        runtimePackage.IsUsed.ShouldBeTrue(); // Used in code - safe to keep
        buildToolPackage.IsUsed.ShouldBeFalse(); // Not used in code but might be build-critical

        // Build tools and analyzers require special consideration for removal
        var potentiallyBuildCritical = solution
            .GetAllUnusedPackages()
            .Where(p => p.PackageId.Contains("Analyzer") || p.PackageId.Contains("CodeAnalysis"))
            .ToList();

        potentiallyBuildCritical.ShouldContain(buildToolPackage);
    }

    [Fact]
    public async Task CompleteWorkflow_WithErrorRecoveryScenario_ShouldMaintainDataIntegrity()
    {
        // Arrange - RFC-0004: Error recovery and data integrity
        var package1 = CreateTestPackageReference("Good.Package", "1.0.0");
        var package2 = CreateTestPackageReference("Error.Package", "1.0.0");

        var project = CreateTestProject("ErrorProject", "net9.0", package1, package2);
        var solution = CreateTestSolution("ErrorSolution", project);

        SetupMockSourceFiles(project, "/test/GoodFile.cs", "/test/ErrorFile.cs");
        SetupMockFileContent(
            "/test/GoodFile.cs",
            "using Good.Package;\nvar obj = new GoodClass();"
        );

        // Setup error condition for second file
        _mockProjectRepository
            .Setup(r => r.ReadSourceFileAsync("/test/ErrorFile.cs", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File access denied"));

        SetupMockPackageNamespaces("Good.Package", "1.0.0", "net9.0", "Good.Package");
        SetupMockPackageNamespaces("Error.Package", "1.0.0", "net9.0", "Error.Package");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert - RFC-0004: Failures do not leave the project in a broken state
        package1.IsUsed.ShouldBeTrue(); // Should be processed successfully
        package2.IsUsed.ShouldBeFalse(); // Should remain in consistent state despite error

        // Verify error was logged but analysis continued
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error scanning file")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        // Data integrity check
        var totalPackages = solution.GetAllPackageReferences().Count();
        var usedPackages = solution.GetAllUsedPackages().Count();
        var unusedPackages = solution.GetAllUnusedPackages().Count();

        (usedPackages + unusedPackages).ShouldBe(totalPackages);
    }

    [Fact]
    public async Task CompleteWorkflow_WithRealWorldComplexSolution_ShouldProvideAccurateRemovalGuidance()
    {
        // Arrange - Real-world scenario with complex dependencies
        var packages = new[]
        {
            CreateTestPackageReference("Newtonsoft.Json", "13.0.3"), // Used
            CreateTestPackageReference("AutoMapper", "12.0.1"), // Used
            CreateTestPackageReference("Serilog", "3.0.1"), // Unused
            CreateTestPackageReference("FluentValidation", "11.7.1"), // Used
            CreateTestPackageReference("Polly", "7.2.4"), // Unused
            CreateTestPackageReference("Microsoft.Extensions.Hosting", "8.0.0"), // Used indirectly
            CreateTestPackageReference("Swashbuckle.AspNetCore", "6.5.0"), // Unused
        };

        var webProject = CreateTestProject("WebApi", "net9.0", packages);
        var solution = CreateTestSolution("RealWorldSolution", webProject);

        // Setup realistic source files
        SetupMockSourceFiles(
            webProject,
            "/src/Controllers/ApiController.cs",
            "/src/Services/BusinessService.cs",
            "/src/Models/RequestModel.cs",
            "/src/Validators/RequestValidator.cs",
            "/src/Program.cs"
        );

        SetupMockFileContent(
            "/src/Controllers/ApiController.cs",
            @"
            using Microsoft.AspNetCore.Mvc;
            using Newtonsoft.Json;
            using AutoMapper;

            [ApiController]
            public class ApiController : ControllerBase
            {
                private readonly IMapper _mapper;
                public ApiController(IMapper mapper) => _mapper = mapper;

                [HttpPost]
                public IActionResult Post(object request)
                {
                    var json = JsonConvert.SerializeObject(request);
                    return Ok(_mapper.Map<object>(request));
                }
            }"
        );

        SetupMockFileContent(
            "/src/Services/BusinessService.cs",
            @"
            using Microsoft.Extensions.Hosting;
            using System.Threading.Tasks;

            public class BusinessService : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken stoppingToken)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }"
        );

        SetupMockFileContent(
            "/src/Validators/RequestValidator.cs",
            @"
            using FluentValidation;

            public class RequestValidator : AbstractValidator<object>
            {
                public RequestValidator()
                {
                    RuleFor(x => x).NotNull();
                }
            }"
        );

        SetupMockFileContent("/src/Models/RequestModel.cs", "public class RequestModel { }");
        SetupMockFileContent(
            "/src/Program.cs",
            "var builder = WebApplication.CreateBuilder(args);"
        );

        // Setup package namespaces
        SetupMockPackageNamespaces("Newtonsoft.Json", "13.0.3", "net9.0", "Newtonsoft.Json");
        SetupMockPackageNamespaces("AutoMapper", "12.0.1", "net9.0", "AutoMapper");
        SetupMockPackageNamespaces("Serilog", "3.0.1", "net9.0", "Serilog");
        SetupMockPackageNamespaces("FluentValidation", "11.7.1", "net9.0", "FluentValidation");
        SetupMockPackageNamespaces("Polly", "7.2.4", "net9.0", "Polly");
        SetupMockPackageNamespaces(
            "Microsoft.Extensions.Hosting",
            "8.0.0",
            "net9.0",
            "Microsoft.Extensions.Hosting"
        );
        SetupMockPackageNamespaces(
            "Swashbuckle.AspNetCore",
            "6.5.0",
            "net9.0",
            "Swashbuckle.AspNetCore"
        );

        SetupMockPathExists(solution.FilePath, webProject.FilePath, webProject.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert - Accurate removal guidance
        var usedPackages = solution.GetAllUsedPackages().ToList();
        var unusedPackages = solution.GetAllUnusedPackages().ToList();

        // Used packages (safe to keep)
        usedPackages.Select(p => p.PackageId).ShouldContain("Newtonsoft.Json");
        usedPackages.Select(p => p.PackageId).ShouldContain("AutoMapper");
        usedPackages.Select(p => p.PackageId).ShouldContain("FluentValidation");
        usedPackages.Select(p => p.PackageId).ShouldContain("Microsoft.Extensions.Hosting");

        // Unused packages (candidates for removal)
        unusedPackages.Select(p => p.PackageId).ShouldContain("Serilog");
        unusedPackages.Select(p => p.PackageId).ShouldContain("Polly");
        unusedPackages.Select(p => p.PackageId).ShouldContain("Swashbuckle.AspNetCore");

        // RFC-0004 Safety: Provide clear removal guidance
        var safeToRemove = unusedPackages.Where(p => p.IsDirect).ToList();
        safeToRemove.Count.ShouldBe(3); // Serilog, Polly, Swashbuckle

        // Verify statistics
        var stats = solution.GetPackageStatistics();
        stats.Total.ShouldBe(7);
        stats.Used.ShouldBe(4);
        stats.Unused.ShouldBe(3);
    }
}
