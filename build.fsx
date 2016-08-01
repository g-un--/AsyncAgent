#r "packages/Fake/tools/FakeLib.dll"
open Fake
open Fake.Testing
open System
open System.IO

let buildDir = "./.build/"
let testDir = "./.test/"
let packages = !! "./**/packages.config"

let testProj = @"AsyncAgent.Tests/AsyncAgent.Tests.csproj"  
let asyncAgentProj = @"AsyncAgent/AsyncAgent.csproj"
let playgroundProj = @"Playground/Playground.csproj"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
)    

Target "RestorePackages" (fun _ ->
    packages
    |> Seq.iter (RestorePackage(fun p -> { p with OutputPath = "./packages" }))
)

Target "Build" (fun _ -> 
    [asyncAgentProj; playgroundProj]
    |> Seq.iter (fun proj ->
        let folderName = Directory.GetParent(proj).Name
        let outputDir = buildDir @@ folderName
        MSBuildRelease outputDir "Build" [proj] |> ignore)
)

Target "BuildTest" (fun _ ->
    [testProj]
    |> MSBuildDebug testDir "Build"
    |> ignore
)

Target "Test" (fun _ -> 
    !! (testDir + "/*.Tests.dll")
        |> xUnit2 (fun p -> 
            {p with 
                ShadowCopy = false;
                HtmlOutputPath = Some(testDir @@ "xunit.html");
                XmlOutputPath = Some(testDir @@ "xunit.html");
            })
)

Target "Default" (fun _ ->
    ()
)

"Clean"
    ==> "RestorePackages"
    ==> "Build"
    ==> "BuildTest"
    ==> "Test"
    ==> "Default"

RunTargetOrDefault "Default"
