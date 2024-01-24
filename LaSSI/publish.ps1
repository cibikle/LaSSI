#!/usr/bin/env pwsh
param (
    [string]$version
)

$directory = $PSScriptRoot;

$version = "0.$version";

try {
    pushd $directory
    dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true
    dotnet publish -c Release --self-contained -r linux-x64 -p:PublishSingleFile=true

    $builtFilePath = "bin/Wpf/Release/net6.0-windows/{0}/publish/";
    $builtFilePathLinux = "bin/Gtk/Release/net6.0/linux-x64/publish/"
    
    $win64Spec = "win-x64";

    New-Item "output" -ItemType Directory;
    
    Move-Item -Path ($builtFilePath -f $win64Spec) -Destination "output";
    Move-Item -Path $builtFilePathLinux -Destination "output";
    ls output
}
finally {
    popd
}
