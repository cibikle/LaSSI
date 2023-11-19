#!/usr/bin/env pwsh
param (
    [string]$version
)

$directory = $PSScriptRoot;
$prevDirectory = (Get-Location).Path;

$version = "0.$version";

try {
    Set-Location $directory;
    dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true
    #dotnet publish -c Release --self-contained -r linux-x64 -p:PublishSingleFile=true
    #dotnet publish -c Release --self-contained -r osx-x64 -p:PublishSingleFile=true

    $builtFilePath = "bin/Wpf/Release/net6.0-windows/{0}/publish/*";
    $compressedFilePath = "bin/Wpf/Release/net6.0-windows/{0}/LaSSI.{0}.v$version.zip";

    $win64Spec = "win-x64";
    #$linux64Spec = "linux-x64";
    #$macx64Spec = "osx-x64";
    #Compress-Archive -Path ($builtFilePath -f $linux64Spec) -DestinationPath ($compressedFilePath -f $linux64Spec)
    #Compress-Archive -Path ($builtFilePath -f $macx64Spec) -DestinationPath ($compressedFilePath -f $macx64Spec)

    New-Item "output" -ItemType Directory;
    
    #Compress-Archive -Path ($builtFilePath -f $win64Spec) -DestinationPath (Join-Path -Path "output" -ChildPath ($compressedFilePath -f $win64Spec))
    Move-Item -Path ($builtFilePath -f $win64Spec) -Destination "output";
    #Move-Item -Path ($compressedFilePath -f $linux64Spec) -Destination "output";
    #Move-Item -Path ($compressedFilePath -f $macx64Spec) -Destination "output";
}
finally {
    Set-Location $prevDirectory;
}
