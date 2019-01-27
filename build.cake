#addin "Newtonsoft.Json&version=10.0.3"
#addin "Cake.Powershell&version=0.4.7"
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"
#tool "nuget:?package=nuget.commandline&version=4.4.1"
#addin "Cake.Http&version=0.5.0"
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
    .IsDependentOn("Build");


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


//////////////////////////////////////////////////////////////////////
// INTEGRATION TEST
//////////////////////////////////////////////////////////////////////
var testDeployDir = Directory("./test/deploy");
var testPackageDir = Directory("./test/packages");
var testWebAppDir = Directory("./src/TestWebApp");
        
Task("Test")
    .IsDependentOn("Build")
    .IsDependentOn("Pack-Nuget")
    .IsDependentOn("Test-InstallNugetPackage")
    .IsDependentOn("Test-Deploy-WebAppGlobalAsax")
    .IsDependentOn("Test-UpdateConfiguration-WebAppGlobalAsax")
    .IsDependentOn("Test-HealthCheck-WebAppGlobalAsax");


Task("Test-InstallNugetPackage")
    .Does(() => {
        EnsureDirectoryExists("./test/packages");
        CleanDirectories("./test/packages");
        
        var artifacts = Directory("./artifacts");

        NuGetInstallSettings settings = new NuGetInstallSettings() {
            Source = new [] { artifacts.Path.MakeAbsolute(Context.Environment).FullPath },
            Prerelease = true,
            OutputDirectory = "./test/packages",
            ExcludeVersion = true,
            Version = GitVersion().NuGetVersion          
        };

        NuGetInstall("iisexpress.runner.service", settings);
    });


Task("Test-Deploy-WebAppGlobalAsax")
    .Does(() => {
        EnsureDirectoryExists("./test/deploy/globalasax");
        EnsureDirectoryExists("./test/deploy/globalasax");
        CleanDirectories("./test/deploy/globalasax");
        
        CopyDirectory(testWebAppDir + Directory("bin"), "./test/deploy/globalasax/bin");
        CopyFile(testWebAppDir + File("web.config"), "./test/deploy/globalasax/web.config");
        CopyFile(testWebAppDir + File("global.asax"), "./test/deploy/globalasax/global.asax");
        
        CopyDirectory("./test/packages/iisexpress.runner.service/tools", "./test/deploy/globalasax/");
    });


Task("Test-UpdateConfiguration-WebAppGlobalAsax")
    .Does(()=> { 
        var iisExpressRunnerConfig = Directory(testDeployDir) + Directory("globalasax") + File("IISExpressService.exe.config");
        var webServiceDeployDir = MakeAbsolute(Directory("./test/deploy/globalasax"));
        var webServicePort = "20000";
        var webServiceDeployDirSlashes = webServiceDeployDir.ToString().Replace("/", @"\");

        XmlPoke(iisExpressRunnerConfig, 
            "/configuration/appSettings/add[@key = 'WebSitePath']/@value", webServiceDeployDirSlashes);
        XmlPoke(iisExpressRunnerConfig, 
            "/configuration/appSettings/add[@key = 'Port']/@value", webServicePort);
    });


Task("Test-HealthCheck-WebAppGlobalAsax")
    .Does(() => {
        var iisExpressRunnerExe = Directory(testDeployDir) + Directory("globalasax") + File("IISExpressService.exe");

        using(var process = StartAndReturnProcess(iisExpressRunnerExe))
        {
            var settings = new HttpSettings { EnsureSuccessStatusCode = true };    

            ExecuteWithRetry(3, () => {
                HttpGet("http://localhost:20000/api/health", settings);
            });        

            process.Kill();
            process.WaitForExit();
            // This should output 0 as valid arguments supplied
            //Information("Exit code: {0}", process.GetExitCode());
        };                        
})
.ReportError(exception =>
{
    Error($"Unable to reach health endpoint api/health");
});

Action<int, Action> ExecuteWithRetry = (retryMax, action) => { 
    bool successful = false;
    int retry = 0;
       
    do
    {            
        try {
            action();
            successful = true;
        }
        catch (Exception) {
            retry++;            
            Information("Retrying");
            if (retry > retryMax)
                throw;
            System.Threading.Thread.Sleep(2000);
        }
    } while (!successful);        
};

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
