#!/usr/bin/env pwsh
# The learner's entry point on Windows. Mirrors run.sh exactly.
param(
    [Parameter(Position = 0)][string]$Command,
    [Parameter(Position = 1)][string]$Module
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Show-Usage {
    @'
usage:
  ./run.ps1 test [NN]   run a module's unit tests (all modules if NN omitted)
  ./run.ps1 status      show which modules are solved
  ./run.ps1 reset NN    restore a module's stubs so you can do it again
'@ | Write-Host
}

function Get-ModulePath([string]$Number) {
    $match = Get-ChildItem -Path 'modules' -Directory -Filter "$Number-*" | Select-Object -First 1
    if (-not $match) { Write-Error "No module numbered $Number."; exit 1 }
    return $match.FullName
}

switch ($Command) {
    'test' {
        if ($Module) { dotnet test --project (Join-Path (Get-ModulePath $Module) 'tests/UnitTests') }
        else {
            Get-ChildItem -Path 'modules' -Directory | ForEach-Object {
                dotnet test --project (Join-Path $_.FullName 'tests/UnitTests')
            }
        }
    }
    'status' {
        New-Item -ItemType Directory -Force -Path artifacts | Out-Null
        Remove-Item artifacts/*.trx -ErrorAction SilentlyContinue
        Get-ChildItem -Path 'modules' -Directory | ForEach-Object {
            dotnet test --project (Join-Path $_.FullName 'tests/UnitTests') `
                --report-trx --report-trx-filename "$($_.Name).trx" --results-directory artifacts
        }
        dotnet run --project tools/Training.Audit -- status --trx artifacts
    }
    'reset' {
        if (-not $Module) { Show-Usage; exit 2 }
        $target = Join-Path (Get-ModulePath $Module) 'src/Exercises'
        $relative = Resolve-Path -Relative $target
        $outside = git status --porcelain -- . ":(exclude)$relative"
        if ($outside) {
            Write-Error "You have uncommitted changes outside $relative. Commit or stash them first."
            exit 1
        }
        Write-Host "About to discard your work in ${relative}:"
        git status --porcelain -- $relative
        $confirm = Read-Host "Type the module number again to confirm"
        if ($confirm -ne $Module) { Write-Host 'Cancelled.'; exit 1 }
        git checkout HEAD -- $relative
        Write-Host "Reset $relative."
    }
    default { Show-Usage; exit 2 }
}
