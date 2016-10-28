#addin "Newtonsoft.Json"
#addin "Cake.Powershell"
#tool "nuget:?package=GitVersion.CommandLine"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionDir = Directory("./");
var solutionFile = solutionDir + File("IISExpress.Runners.sln");
var buildDir = Directory("./src/Service/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/bin/**");
    CleanDirectories("./**/obj/**");    
});


Task("Restore-NuGet-Packages")
    .Does(() =>
{
    NuGetRestore(solutionFile);
});


Task("Pack")
    .Does(() => 
{
    EnsureDirectoryExists("./artifacts");
    string version = GitVersion().NuGetVersion;

    var binDir = Directory("./bin") ;
    var nugetPackageDir = Directory("./artifacts");

    var nugetFilePaths = GetFiles("./src/Service/*.csproj");

    var nuGetPackSettings = new NuGetPackSettings
    {   
        Version = version,
        BasePath = binDir + Directory(configuration),
        OutputDirectory = nugetPackageDir,
        ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)
    };

    NuGetPack(nugetFilePaths, nuGetPackSettings);
});


Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Update-Version")
    .Does(() =>
{
    // Use MSBuild
    MSBuild(solutionFile, settings => settings.SetConfiguration(configuration));
});


Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build")
    .Does(() =>
{ });


Task("Update-Version")
    .Does(() => 
{
    GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true});
    string version = GitVersion().FullSemVer;
    if (AppVeyor.IsRunningOnAppVeyor) {
        AppVeyor.UpdateBuildVersion(version);
    }
    var projectFiles = System.IO.Directory.EnumerateFiles(@".\", "project.json", SearchOption.AllDirectories).ToArray();

    foreach(var file in projectFiles)
    {
        var project = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(file, Encoding.UTF8));

        project["version"].Replace(version);

        System.IO.File.WriteAllText(file, project.ToString(), Encoding.UTF8);
    }
});


Task("Get-DotNetCli")
    .Does(() =>
{         
    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    string dotNetPath = userProfile + @"Local\Microsoft\dotnet";
    
    if (!Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine).Contains(dotNetPath))
    {
        DownloadFile("https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1", "./tools/dotnet-install.ps1");
        var version = "1.0.0-preview2-003121";
        StartPowershellFile("./tools/dotnet-install.ps1", args =>
            {
                args.Append("Version", version);
            });

        
        string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) + ";" + dotNetPath;
        Console.WriteLine(path);
        Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("Path", path);
    }
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Pack");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
