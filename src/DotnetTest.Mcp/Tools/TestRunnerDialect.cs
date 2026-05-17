namespace DotnetTest.Mcp.Tools;

internal enum TestRunnerDialect
{
    Unknown,
    TUnit,
    VSTest,
}

internal static class TestRunnerDialectDetector
{
    internal static TestRunnerDialect Detect(
        string? projectPath,
        McpOptions options,
        string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return TestRunnerDialect.Unknown;

        var effectiveWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
            ? options.WorkingDirectory
            : workingDirectory;

        var fullPath = Path.IsPathRooted(projectPath)
            ? projectPath
            : Path.Combine(effectiveWorkingDirectory, projectPath);

        if (!File.Exists(fullPath))
            return TestRunnerDialect.Unknown;

        var projectXml = File.ReadAllText(fullPath);
        if (ContainsPackage(projectXml, "TUnit")
            || ContainsPackage(projectXml, "TUnit.Core")
            || ContainsPackage(projectXml, "TUnit.Assertions")
            || ContainsPackage(projectXml, "TUnit.Engine")
            || ContainsPackage(projectXml, "Microsoft.Testing.Platform"))
            return TestRunnerDialect.TUnit;

        if (ContainsPackage(projectXml, "Microsoft.NET.Test.Sdk")
            || ContainsPackage(projectXml, "xunit.runner.visualstudio")
            || ContainsPackage(projectXml, "MSTest.TestAdapter")
            || ContainsPackage(projectXml, "NUnit3TestAdapter"))
            return TestRunnerDialect.VSTest;

        return TestRunnerDialect.Unknown;
    }

    private static bool ContainsPackage(string projectXml, string packageName)
        => projectXml.Contains($"Include=\"{packageName}\"", StringComparison.OrdinalIgnoreCase)
            || projectXml.Contains($"Include='{packageName}'", StringComparison.OrdinalIgnoreCase);
}

internal static class TestCommandBuilder
{
    internal static string[] BuildClassRun(
        TestRunnerDialect dialect,
        string className,
        string? project)
    {
        if (dialect == TestRunnerDialect.TUnit && !string.IsNullOrWhiteSpace(project))
            return
            [
                "run",
                "--project",
                project,
                "--",
                "--no-ansi",
                "--disable-logo",
                "--treenode-filter",
                $"/*/*/{className}/*",
            ];

        if (!string.IsNullOrWhiteSpace(project))
            return ["test", "--project", project, "--filter-class", className];

        return ["test", "--filter-class", className];
    }

    internal static string[] BuildSingleTestRun(
        TestRunnerDialect dialect,
        string qualifiedMethodName,
        string? project)
    {
        if (dialect == TestRunnerDialect.TUnit && !string.IsNullOrWhiteSpace(project))
        {
            var (className, methodName) = SplitQualifiedMethodName(qualifiedMethodName);
            return
            [
                "run",
                "--project",
                project,
                "--",
                "--no-ansi",
                "--disable-logo",
                "--treenode-filter",
                $"/*/*/{className}/{methodName}",
            ];
        }

        if (!string.IsNullOrWhiteSpace(project))
            return ["test", "--project", project, "--filter-method", qualifiedMethodName];

        return ["test", "--filter-method", qualifiedMethodName];
    }

    private static (string ClassName, string MethodName) SplitQualifiedMethodName(string qualifiedMethodName)
    {
        var methodSeparator = qualifiedMethodName.LastIndexOf('.');
        if (methodSeparator <= 0 || methodSeparator == qualifiedMethodName.Length - 1)
            return ("*", qualifiedMethodName);

        var methodName = qualifiedMethodName[(methodSeparator + 1)..];
        var classQualifiedName = qualifiedMethodName[..methodSeparator];
        var classSeparator = classQualifiedName.LastIndexOf('.');
        var className = classSeparator < 0
            ? classQualifiedName
            : classQualifiedName[(classSeparator + 1)..];

        return (className, methodName);
    }
}
