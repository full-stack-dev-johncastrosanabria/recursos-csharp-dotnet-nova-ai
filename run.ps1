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
    if (-not (Test-Path -Path 'modules' -PathType Container)) {
        Write-Error "No module numbered $Number."
        exit 1
    }
    $candidates = @(Get-ChildItem -Path 'modules' -Directory -Filter "$Number-*")
    if ($candidates.Count -eq 0) { Write-Error "No module numbered $Number."; exit 1 }
    if ($candidates.Count -gt 1) {
        $names = ($candidates | ForEach-Object { "modules/$($_.Name)" }) -join ' '
        Write-Error "Ambiguous module number ${Number}: $names"
        exit 1
    }
    return $candidates[0].FullName
}

# Every discovered unit-tier test project, repo-relative. Discovery comes
# from Training.Audit rather than a glob on a hardcoded tier name, so a
# module that adds a new tier (say tests/ContractTests) is never silently
# skipped here. Filtered to the unit tier on purpose: integration tests
# need Docker and are deliberately excluded from the everyday
# test/status loop (see README.md).
#
# A failure of the discovery command itself must be loud and non-zero; a
# genuinely empty repo (no modules yet, or none with a unit tier) must stay
# a clean, silent success — those two look identical by output alone (both
# produce no project paths), so the discovery command's own exit status is
# checked explicitly rather than inferred from empty output. $LASTEXITCODE
# is not turned into a terminating error on its own here (see the 'test'
# case below), so the check has to be explicit.
function Get-UnitTestProjects {
    $discovered = dotnet run --project tools/Training.Audit -- test-projects
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Test-project discovery failed: dotnet run --project tools/Training.Audit -- test-projects exited $LASTEXITCODE."
        exit $LASTEXITCODE
    }
    $discovered | Where-Object { $_ -match '/UnitTests$' }
}

switch ($Command) {
    'test' {
        if ($Module) {
            dotnet test --project (Join-Path (Get-ModulePath $Module) 'tests/UnitTests')
            exit $LASTEXITCODE
        }
        else {
            # dotnet's own exit code does not become this script's exit code
            # on its own ($PSNativeCommandUseErrorActionPreference is False
            # here), so track failure explicitly. A failing module must not
            # hide every module after it, so keep going — but remember it
            # happened, so the script's own exit code still tells the truth.
            $failed = 0
            foreach ($project in Get-UnitTestProjects) {
                dotnet test --project $project
                if ($LASTEXITCODE -ne 0) { $failed = 1 }
            }
            exit $failed
        }
    }
    'status' {
        New-Item -ItemType Directory -Force -Path artifacts | Out-Null
        Remove-Item artifacts/*.trx -ErrorAction SilentlyContinue
        foreach ($project in Get-UnitTestProjects) {
            $name = Split-Path (Split-Path (Split-Path $project -Parent) -Parent) -Leaf
            # A non-zero exit is expected: unsolved exercises are failing tests.
            dotnet test --project $project `
                --report-trx --report-trx-filename "$name.trx" --results-directory artifacts
        }
        dotnet run --project tools/Training.Audit -- status --trx artifacts
        exit $LASTEXITCODE
    }
    'reset' {
        if (-not $Module) { Show-Usage; exit 2 }
        $target = Join-Path (Get-ModulePath $Module) 'src/Exercises'
        # Resolve-Path -Relative uses the platform separator, which is '\'
        # on Windows. Git treats '\' as a pathspec escape character, so a
        # ":(exclude)...\..." pathspec silently fails to match anything and
        # $outside is never empty. Normalise to '/' before it reaches git.
        $relative = (Resolve-Path -Relative $target) -replace '\\', '/'
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
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Write-Host "Reset $relative."
    }
    default { Show-Usage; exit 2 }
}
