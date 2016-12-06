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


Task("Pack-Nuget")
    .Does(() => 
{
    EnsureDirectoryExists("./artifacts");
    string version = GitVersion().NuGetVersion;

    var nugetPackageDir = Directory("./artifacts");

    var nuGetPackSettings = new NuGetPackSettings
    {   
        Version                 = version,
        OutputDirectory         = nugetPackageDir,
        ArgumentCustomization   = args => args.Append("-Prop Configuration=" + configuration)
    };

    NuGetPack("src/iisexpress.runner.service.nuspec", nuGetPackSettings);
});


Task("Pack-NugetTestPackage")
    .Does(() => 
{
    EnsureDirectoryExists("./test/artifacts");
    CleanDirectories("./test/artifacts");
    string version = GitVersion().NuGetVersion;

    var nugetPackageDir = Directory("./test/artifacts");

    var nuGetPackSettings = new NuGetPackSettings
    {   
        Version                 = version,
        OutputDirectory         = nugetPackageDir,
        ArgumentCustomization   = args => args.Append("-Prop Configuration=" + configuration)
    };

    NuGetPack("src/iisexpress.runner.service.nuspec", nuGetPackSettings);
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
// INTEGRATION TEST
//////////////////////////////////////////////////////////////////////
var testDeployDir = Directory("./test/deploy");
var testPackageDir = Directory("./test/packages");
var testWebAppDir = Directory("./src/TestWebApp");
        
Task("Test")
    .IsDependentOn("Build")
    .IsDependentOn("Deploy-NugetTestPackage")
    .IsDependentOn("Update-TestConfiguration")
    .Does(() => {
        
    });

Task("Install-NugetTestPackage")
    .Does(() => {
        EnsureDirectoryExists("./test/packages");
        CleanDirectories("./test/packages");

        NuGetInstallSettings settings = new NuGetInstallSettings() {
            Source = new [] { "d:/dev/IISExpress.Runners/test/artifacts"},
            Prerelease = true,
            OutputDirectory = "./test/packages",
            ExcludeVersion = true
        };

        NuGetInstall("iisexpress.runner.service", settings);
    });

Task("Deploy-NugetTestPackage")
    .IsDependentOn("Pack-NugetTestPackage")
    .IsDependentOn("Install-NugetTestPackage")
    .Does(() => {        
        EnsureDirectoryExists(testDeployDir);
        CleanDirectories(testDeployDir);
        
        CopyDirectory((testPackageDir + Directory("iisexpress.runner.service/tools")), testDeployDir);
        CopyDirectory(testWebAppDir + Directory("bin"), testDeployDir + Directory("bin"));

        CopyFileToDirectory(testWebAppDir + File("Global.asax"), testDeployDir);
        CopyFileToDirectory(testWebAppDir + File("Web.config"), testDeployDir);
    });

Task("Update-TestConfiguration")
    .Does(()=> { 
        var iisExpressRunnerConfig = "./test/deploy" + "/IISExpressService.exe.config";
        var webServiceDeployDir = MakeAbsolute(Directory("./test/deploy"));
        var webServicePort = "2000";
        var webServiceDeployDirSlashes = webServiceDeployDir.ToString().Replace("/", @"\");

        XmlPoke(iisExpressRunnerConfig, 
            "/configuration/appSettings/add[@key = 'WebSitePath']/@value", webServiceDeployDirSlashes);
        XmlPoke(iisExpressRunnerConfig, 
            "/configuration/appSettings/add[@key = 'Port']/@value", webServicePort);
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
