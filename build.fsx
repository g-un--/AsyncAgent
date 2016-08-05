#r "packages/Fake/tools/FakeLib.dll"
open Fake
open Fake.Testing
open System
open System.IO

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let buildDir = "./.build/"
let testDir = "./.test/"
let packages = !! "./**/packages.config"

let testProjs = !! "*.Tests/*.Tests.csproj"  
let asyncAgentProjs = !! "**/*.csproj" -- "*.Tests/*.Tests.csproj"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
)    

Target "RestorePackages" (fun _ ->
    packages
    |> Seq.iter (RestorePackage(fun p -> { p with OutputPath = "./packages" }))
)

Target "Build" (fun _ -> 
    asyncAgentProjs  
    |> Seq.iter (fun proj ->
        let folderName = Directory.GetParent(proj).Name
        let outputDir = buildDir @@ folderName
        MSBuildRelease outputDir "Build" [proj] |> ignore)
)

Target "BuildTest" (fun _ ->
    testProjs
    |> MSBuildDebug testDir "Build"
    |> ignore
)

Target "Test" (fun _ -> 
    !! (testDir + "/*.Tests.dll")
        |> xUnit2 (fun p -> 
            {p with 
                ShadowCopy = false;
                HtmlOutputPath = Some(testDir @@ "AsyncAgent-TestsResult.html");
                XmlOutputPath = Some(testDir @@ "AsyncAgent-TestsResults.xml");
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
