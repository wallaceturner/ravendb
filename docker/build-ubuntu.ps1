param(
    $Repo = "ravendb/ravendb",
    $ArtifactsDir = "..\artifacts",
    $RavenDockerSettingsPath = "..\src\Raven.Server\Properties\Settings\settings.docker.posix.json",
    $Arch = "x64",
    $DockerfileDir = "./ravendb-ubuntu",
    $DebPackagePath="",
    [switch]$NoCache)

$ErrorActionPreference = "Stop"

. ".\common.ps1"

function GetPackageFileName($arch)
{
    switch ($arch) {
        "arm32v7" { 
            return "RavenDB-$version-raspberry-pi.tar.bz2"
        }
        "arm64v8" {
            return "RavenDB-$version-linux-arm64.tar.bz2"
        }
        "x64" {
            return "RavenDB-$version-linux-x64.tar.bz2"
        }
        Default {
            throw "Arch not supported (currently x64 and arm32v7 are supported)"
        }
    }
}

function GetUbuntuVersionFromDockerfile($DockerfileDir, $DockerfileName) {
    $dockerfilePath = Join-Path $DockerfileDir "Dockerfile.$arch"
    $dockerfileContents = Get-Content $dockerfilePath
    $ubuntuVersion = $dockerfileContents | Select-String -Pattern "(?<=FROM\s+mcr\.microsoft\.com\/dotnet\/runtime-deps:7\.0-)(.*)"
    return $ubuntuVersion.Matches.Groups[1].Value
}

function SetupDebBuildEnvironment($arch, $ubuntuVersion){
    switch ($arch) {
        "x64" {
            . "..\scripts\linux\pkg\deb\set-raven-platform-amd64.ps1"
        }
        "arm64v8" {
            . "..\scripts\linux\pkg\deb\set-raven-platform-arm64.ps1"
        }
        "arm32v7" {
            . "..\scripts\linux\pkg\deb\set-raven-platform-armhf.ps1"
        }
        Default {
            Write-Error "ERROR: Unsupported architecture $($arch)"
            exit 1
        }
    }

    switch ($ubuntuVersion) {
        "bionic" {
            . "..\scripts\linux\pkg\deb\set-ubuntu-bionic.ps1"
        }
        "focal" {
            . "..\scripts\linux\pkg\deb\set-ubuntu-focal.ps1"
        }
        "jammy" {
            . "..\scripts\linux\pkg\deb\set-ubuntu-jammy.ps1"
        }
        Default {
            Write-Error "ERROR: Unsupported Ubuntu version $($ubuntuVersion). Supported version: bionic, focal, jammy."
            exit 1
        }
    }
}


function BuildUbuntuDockerImage ($version, $arch) {
    $packageFileName = GetPackageFileName $arch
    $artifactsPackagePath = Join-Path -Path $ArtifactsDir -ChildPath $packageFileName

    if ([string]::IsNullOrEmpty($artifactsPackagePath)) {
        throw "PackagePath cannot be empty."
    }

    if ($(Test-Path $artifactsPackagePath) -eq $False) {
        throw "Package file does not exist."
    }

    $dockerPackagePath = Join-Path -Path $DockerfileDir -ChildPath "RavenDB.tar.bz2"
    Copy-Item -Path $artifactsPackagePath -Destination $dockerPackagePath -Force
    Copy-Item -Path $RavenDockerSettingsPath -Destination $(Join-Path -Path $DockerfileDir -ChildPath "settings.json") -Force
    write-host "Build docker image: $version"
    $tags = GetUbuntuImageTags $repo $version $arch
    write-host "Tags: $tags"

    $fullNameTag = $tags[0]

    if ([string]::IsNullOrEmpty($DebPackagePath)) {
        $ubuntuVersion = GetUbuntuVersionFromDockerfile $DockerfileDir "Dockerfile.$arch"
        SetupDebBuildEnvironment $arch $ubuntuVersion
        
        $archNameToMatch = switch ($arch) {
            "x64" { "amd64"; break }
            "arm32v7" { "arm32"; break }
            "arm64v8" { "arm64"; break }
            Default {
                Write-Error "ERROR: Unsupported architecture $($arch)"
                exit 1
            }
        }
        

        if (!$NoCache) {
            $matchingFile = Get-ChildItem $DockerfileDir | Where-Object { $_.Name -like "ravendb*$archNameToMatch*.deb" }
        }

        if(!$matchingFile) {
            $env:RAVENDB_VERSION = $version
            $env:OUTPUT_DIR = $(Convert-Path $DockerfileDir)
            $env:PACKAGE_FILE_DIR = Resolve-Path $ArtifactsDir
        
            $currentScriptWorkingDirectory = $(Get-Location)
            $buildScriptPath = (Resolve-Path "..\scripts\linux\pkg\deb\build-deb.ps1").Path
            Set-Location $(Split-Path $buildScriptPath)
    
            . "./build-deb.ps1"
        
            Set-Location $currentScriptWorkingDirectory
            CheckLastExitCode

            $matchingFile = Get-ChildItem $DockerfileDir | Where-Object { $_.Name -like "ravendb*$version*$archNameToMatch*.deb" }
            if ($matchingFile) {
                $pathToDeb = $matchingFile.FullName
            } else {
                Write-Host "FATAL: No ravendb .deb file for '$($arch)' architecture found after running script building .deb package." 
                exit 1
            }
        }
        else {
            $pathToDeb = $matchingFile.FullName
        }
    }
    else {
        $pathToDeb = $DebPackagePath
    }

    Write-Host "Providing deb path '$($pathToDeb)' to Dockerfile.."
    docker build $DockerfileDir -f "$($DockerfileDir)/Dockerfile.$($arch)" -t "$fullNameTag" --build-arg "PATH_TO_DEB=./$matchingFile"
    CheckLastExitCode
    
    foreach ($tag in $tags[1..$tags.Length]) {
        write-host "Tag $fullNameTag as $tag"
        docker tag "$fullNameTag" $tag
        CheckLastExitCode
    }

    Remove-Item -Path $dockerPackagePath
}

BuildUbuntuDockerImage $(GetVersionFromArtifactName) $Arch
