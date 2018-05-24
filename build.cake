#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin "Cake.Docker"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionFile = File("./src/DockerGracefullShutdownTest.sln");
var projDir = Directory("./src/DockerGracefullShutdownTest");
var dockerBuildFile = File("Dockerfile");
var buildDir = Directory("./src/DockerGracefullShutdownTest/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(solutionFile);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
    //   MSBuild(solutionFile, settings =>
    //     settings.SetConfiguration(configuration));
        var settings = new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            OutputDirectory = "./build/"
        };

        DotNetCoreBuild(solutionFile, settings);
    }
    else
    {
      // Use XBuild
      XBuild(solutionFile, settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
});

Task("Publish")
    .IsDependentOn("Run-Unit-Tests")
    .Does(()=>
    {
        var settings = new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = "./publish/"
        };

        DotNetCorePublish(solutionFile, settings);
    });

Task("Build-Docker")
	.IsDependentOn("Publish")
	.Does(() =>
	{
        var dockerBuildSettings = new DockerImageBuildSettings
        {
            File = dockerBuildFile,
            Label = new[] {"DockerGracefullShutdownTest"}
        };
		DockerBuild(dockerBuildSettings, ".");
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
