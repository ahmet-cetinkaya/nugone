using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using NuGone.Domain.Shared.ValueObjects;

namespace NuGone.Application.Features.PackageAnalysis.Commands.AnalyzePackageUsage;

/// <summary>
/// Command handler for analyzing package usage in a solution or project.
/// Implements the core algorithm specified in RFC-0002.
/// </summary>
public partial class AnalyzePackageUsageHandler(
    ISolutionRepository solutionRepository,
    IProjectRepository projectRepository,
    INuGetRepository nugetRepository,
    IPackageUsageAnalyzer packageUsageAnalyzer,
    ILogger<AnalyzePackageUsageHandler> logger
)
{
    private readonly ISolutionRepository _solutionRepository =
        solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
    private readonly IProjectRepository _projectRepository =
        projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    private readonly INuGetRepository _nugetRepository =
        nugetRepository ?? throw new ArgumentNullException(nameof(nugetRepository));
    private readonly IPackageUsageAnalyzer _packageUsageAnalyzer =
        packageUsageAnalyzer ?? throw new ArgumentNullException(nameof(packageUsageAnalyzer));
    private readonly ILogger<AnalyzePackageUsageHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly Action<ILogger, Exception?> _analysisCancelledWarning =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(AnalyzePackageUsageHandler)),
            "Package usage analysis was cancelled"
        );

    private static readonly Action<ILogger, Exception?> _unexpectedError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(2, nameof(AnalyzePackageUsageHandler)),
        "An unexpected error occurred during package usage analysis"
    );

    private static readonly Action<ILogger, Exception?> _errorLoadingSolution =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3, nameof(AnalyzePackageUsageHandler)),
            "Error loading solution"
        );

    private static readonly Action<ILogger, Exception?> _errorLoadingProject = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(4, nameof(AnalyzePackageUsageHandler)),
        "Error loading project"
    );

    private static readonly Action<ILogger, Exception?> _errorLoadingDirectory =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(5, nameof(AnalyzePackageUsageHandler)),
            "Error loading directory"
        );

    private static readonly Action<ILogger, Exception?> _errorLoadingPackageReferences =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(6, nameof(AnalyzePackageUsageHandler)),
            "Error loading package references"
        );

    private static readonly Action<ILogger, Exception?> _errorPerformingAnalysis =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7, nameof(AnalyzePackageUsageHandler)),
            "Error performing package analysis"
        );

    /// <summary>
    /// Handles the package usage analysis command.
    /// RFC-0002: Main entry point for unused package detection.
    /// </summary>
    /// <param name="command">The analysis command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result containing unused packages</returns>
    public async Task<Result<AnalyzePackageUsageResult>> HandleAsync(
        AnalyzePackageUsageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            LogStartingAnalysis(_logger, command.Path);

            // Step 1: Validate input
            var validationResult = ValidateCommand(command);
            if (validationResult.IsFailure)
                return Result<AnalyzePackageUsageResult>.Failure(validationResult.Error);

            // Step 2: Determine if we're analyzing a solution or individual project
            var analysisTarget = await DetermineAnalysisTargetAsync(command.Path, cancellationToken)
                .ConfigureAwait(false);
            if (analysisTarget.IsFailure)
                return Result<AnalyzePackageUsageResult>.Failure(analysisTarget.Error);

            // Step 3: Load the solution or project
            var loadResult = await LoadAnalysisTargetAsync(
                    analysisTarget.Value,
                    command.Path,
                    cancellationToken
                )
                .ConfigureAwait(false);
            if (loadResult.IsFailure)
                return Result<AnalyzePackageUsageResult>.Failure(loadResult.Error);

            // Step 4: Apply exclude patterns to projects
            var solution = loadResult.Value;
            ApplyExcludePatterns(solution, command.ExcludePatterns);

            // Step 5: Load package references for all projects
            var packageLoadResult = await LoadPackageReferencesAsync(solution, cancellationToken)
                .ConfigureAwait(false);
            if (packageLoadResult.IsFailure)
                return Result<AnalyzePackageUsageResult>.Failure(packageLoadResult.Error);

            // Step 6: Perform the analysis
            var analysisResult = await PerformAnalysisAsync(solution, command, cancellationToken)
                .ConfigureAwait(false);
            if (analysisResult.IsFailure)
                return Result<AnalyzePackageUsageResult>.Failure(analysisResult.Error);

            stopwatch.Stop();
            LogAnalysisCompleted(_logger, stopwatch.Elapsed);

            var result = new AnalyzePackageUsageResult(
                command.Path,
                stopwatch.Elapsed,
                analysisResult.Value
            );

            return Result<AnalyzePackageUsageResult>.Success(result);
        }
        catch (OperationCanceledException)
        {
            _analysisCancelledWarning(_logger, null);
            return Result<AnalyzePackageUsageResult>.Failure(
                "OPERATION_CANCELLED",
                "Analysis was cancelled"
            );
        }
#pragma warning disable CA1031 // Top-level handler must catch all exceptions to provide proper error handling
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _unexpectedError(_logger, ex);
            return Result<AnalyzePackageUsageResult>.Failure(
                "UNEXPECTED_ERROR",
                $"An unexpected error occurred: {ex.Message}"
            );
        }
    }

    private static Result ValidateCommand(AnalyzePackageUsageCommand command)
    {
        if (command == null)
            return Result.Failure("INVALID_COMMAND", "Command cannot be null");

        if (string.IsNullOrWhiteSpace(command.Path))
            return Result.Failure("INVALID_PATH", "Path cannot be null or empty");

        return Result.Success();
    }

    private async Task<Result<AnalysisTargetType>> DetermineAnalysisTargetAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        if (!await _projectRepository.ExistsAsync(path).ConfigureAwait(false))
            return Result<AnalysisTargetType>.Failure(
                "PATH_NOT_FOUND",
                $"Path does not exist: {path}"
            );

        // Check if it's a solution file
        if (
            path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
        )
        {
            return Result<AnalysisTargetType>.Success(AnalysisTargetType.Solution);
        }

        // Check if it's a project file
        if (
            path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
        )
        {
            return Result<AnalysisTargetType>.Success(AnalysisTargetType.Project);
        }

        // Check if it's a directory - look for solution files first, then projects
        var solutionFiles = await _solutionRepository
            .DiscoverSolutionFilesAsync(path, cancellationToken)
            .ConfigureAwait(false);
        if (solutionFiles.Any())
        {
            return Result<AnalysisTargetType>.Success(AnalysisTargetType.Directory);
        }

        var projectFiles = await _projectRepository
            .DiscoverProjectFilesAsync(path, cancellationToken)
            .ConfigureAwait(false);
        if (projectFiles.Any())
        {
            return Result<AnalysisTargetType>.Success(AnalysisTargetType.Directory);
        }

        return Result<AnalysisTargetType>.Failure(
            "NO_PROJECTS_FOUND",
            "No solution or project files found in the specified path"
        );
    }

    private async Task<Result<Solution>> LoadAnalysisTargetAsync(
        AnalysisTargetType targetType,
        string path,
        CancellationToken cancellationToken
    )
    {
        switch (targetType)
        {
            case AnalysisTargetType.Solution:
                return await LoadSolutionAsync(path, cancellationToken).ConfigureAwait(false);

            case AnalysisTargetType.Project:
                return await LoadSingleProjectAsSolutionAsync(path, cancellationToken)
                    .ConfigureAwait(false);

            case AnalysisTargetType.Directory:
                return await LoadDirectoryAsSolutionAsync(path, cancellationToken)
                    .ConfigureAwait(false);

            default:
                return Result<Solution>.Failure(
                    "INVALID_TARGET_TYPE",
                    $"Unknown analysis target type: {targetType}"
                );
        }
    }

    private async Task<Result<Solution>> LoadSolutionAsync(
        string solutionPath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var solution = await _solutionRepository
                .LoadSolutionAsync(solutionPath, cancellationToken)
                .ConfigureAwait(false);

            // Load each project in the solution
            var loadedProjects = new List<Project>();
            foreach (var project in solution.Projects)
            {
                var loadedProject = await _projectRepository
                    .LoadProjectAsync(project.FilePath, cancellationToken)
                    .ConfigureAwait(false);
                loadedProjects.Add(loadedProject);
            }

            // Replace projects with loaded versions
            solution.Projects.Clear();
            foreach (var project in loadedProjects)
            {
                solution.AddProject(project);
            }

            return Result<Solution>.Success(solution);
        }
#pragma warning disable CA1031 // Solution loading can fail for various reasons (file I/O, parsing, etc.)
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _errorLoadingSolution(_logger, ex);
            return Result<Solution>.Failure(
                "SOLUTION_LOAD_ERROR",
                $"Failed to load solution: {ex.Message}"
            );
        }
    }

    private async Task<Result<Solution>> LoadSingleProjectAsSolutionAsync(
        string projectPath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var project = await _projectRepository
                .LoadProjectAsync(projectPath, cancellationToken)
                .ConfigureAwait(false);
            var solutionName = Path.GetFileNameWithoutExtension(projectPath) + "_Solution";
            var solutionPath = Path.ChangeExtension(projectPath, ".sln");

            var solution = new Solution(solutionPath, solutionName, isVirtual: true);
            solution.AddProject(project);

            return Result<Solution>.Success(solution);
        }
#pragma warning disable CA1031 // Project loading can fail for various reasons (file I/O, parsing, etc.)
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _errorLoadingProject(_logger, ex);
            return Result<Solution>.Failure(
                "PROJECT_LOAD_ERROR",
                $"Failed to load project: {ex.Message}"
            );
        }
    }

    private async Task<Result<Solution>> LoadDirectoryAsSolutionAsync(
        string directoryPath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // First try to find a solution file
            var solutionFiles = await _solutionRepository
                .DiscoverSolutionFilesAsync(directoryPath, cancellationToken)
                .ConfigureAwait(false);
            if (solutionFiles.Any())
            {
                return await LoadSolutionAsync(solutionFiles.First(), cancellationToken)
                    .ConfigureAwait(false);
            }

            // If no solution file, create a virtual solution from all projects
            var projectFiles = await _projectRepository
                .DiscoverProjectFilesAsync(directoryPath, cancellationToken)
                .ConfigureAwait(false);
            if (!projectFiles.Any())
            {
                return Result<Solution>.Failure(
                    "NO_PROJECTS_FOUND",
                    $"No projects found in directory: {directoryPath}"
                );
            }

            var solutionName = Path.GetFileName(directoryPath) + "_Solution";
            var solutionPath = Path.Combine(directoryPath, solutionName + ".sln");
            var solution = new Solution(solutionPath, solutionName, isVirtual: true);

            foreach (var projectFile in projectFiles)
            {
                var project = await _projectRepository
                    .LoadProjectAsync(projectFile, cancellationToken)
                    .ConfigureAwait(false);
                solution.AddProject(project);
            }

            return Result<Solution>.Success(solution);
        }
#pragma warning disable CA1031 // Directory loading can fail for various reasons (file I/O, parsing, etc.)
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _errorLoadingDirectory(_logger, ex);
            return Result<Solution>.Failure(
                "DIRECTORY_LOAD_ERROR",
                $"Failed to load directory: {ex.Message}"
            );
        }
    }

    private static void ApplyExcludePatterns(Solution solution, IEnumerable<string> excludePatterns)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var pattern in excludePatterns)
            {
                project.AddExcludePattern(pattern);
            }
        }
    }

    private async Task<Result> LoadPackageReferencesAsync(
        Solution solution,
        CancellationToken cancellationToken
    )
    {
        try
        {
            Dictionary<string, string>? centralPackageVersions = null;
            if (
                solution.CentralPackageManagementEnabled
                && !string.IsNullOrEmpty(solution.DirectoryPackagesPropsPath)
            )
            {
                centralPackageVersions = await _solutionRepository
                    .LoadCentralPackageVersionsAsync(
                        solution.DirectoryPackagesPropsPath,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            foreach (var project in solution.Projects)
            {
                var packageReferences = await _nugetRepository
                    .ExtractPackageReferencesAsync(
                        project.FilePath,
                        centralPackageVersions,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                foreach (var packageRef in packageReferences)
                {
                    project.AddPackageReference(packageRef);
                }
            }

            return Result.Success();
        }
#pragma warning disable CA1031 // Package reference loading can fail for various reasons (file I/O, parsing, etc.)
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _errorLoadingPackageReferences(_logger, ex);
            return Result.Failure(
                "PACKAGE_LOAD_ERROR",
                $"Failed to load package references: {ex.Message}"
            );
        }
    }

    private async Task<Result<IEnumerable<ProjectAnalysisResult>>> PerformAnalysisAsync(
        Solution solution,
        AnalyzePackageUsageCommand command,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Validate inputs
            var validationResult = await _packageUsageAnalyzer
                .ValidateInputsAsync(solution)
                .ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                return Result<IEnumerable<ProjectAnalysisResult>>.Failure(
                    "VALIDATION_ERROR",
                    $"Validation failed: {errors}"
                );
            }

            // Perform the analysis
            await _packageUsageAnalyzer
                .AnalyzePackageUsageAsync(solution, cancellationToken)
                .ConfigureAwait(false);

            // Convert results to DTOs
            var projectResults = new List<ProjectAnalysisResult>();
            foreach (var project in solution.Projects)
            {
                var unusedPackages = project
                    .GetUnusedPackages()
                    .Select(ConvertToPackageUsageDetail);
                var usedPackages = project.GetUsedPackages().Select(ConvertToPackageUsageDetail);

                var projectResult = new ProjectAnalysisResult(
                    project.Name,
                    project.FilePath,
                    project.TargetFramework,
                    unusedPackages,
                    usedPackages
                );

                projectResults.Add(projectResult);
            }

            return Result<IEnumerable<ProjectAnalysisResult>>.Success(projectResults);
        }
#pragma warning disable CA1031 // Analysis can fail for various reasons and must be handled gracefully
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _errorPerformingAnalysis(_logger, ex);
            return Result<IEnumerable<ProjectAnalysisResult>>.Failure(
                "ANALYSIS_ERROR",
                $"Analysis failed: {ex.Message}"
            );
        }
    }

    private static PackageUsageDetail ConvertToPackageUsageDetail(PackageReference packageRef)
    {
        return new PackageUsageDetail(
            packageRef.PackageId,
            packageRef.Version,
            packageRef.IsDirect,
            packageRef.IsUsed,
            packageRef.Condition,
            packageRef.UsageLocations,
            packageRef.DetectedNamespaces,
            packageRef.HasGlobalUsing
        );
    }

    private enum AnalysisTargetType
    {
        Solution,
        Project,
        Directory,
    }
}
