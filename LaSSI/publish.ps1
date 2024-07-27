#!/usr/bin/env pwsh
param (
    [string]$version
)

$directory = $PSScriptRoot;

$version = "0.$version";

try {
    pushd $directory
    dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true -p:AssemblyVersion=$version

    $builtFilePath = "bin/Wpf/Release/net6.0-windows/{0}/publish/";
    
    $win64Spec = "win-x64";

    New-Item "output" -ItemType Directory;
    
    Move-Item -Path ($builtFilePath -f $win64Spec) -Destination "output";
    ls output
}
finally {
    popd
}
