<#
.SYNOPSIS
    Audits the XML documentation coverage of the public API.

.DESCRIPTION
    Uses the C# compiler's own documentation diagnostics as the source of truth:

        CS1591  Missing XML comment for publicly visible type or member
        CS1573  Parameter 'x' has no matching param tag in the XML comment
        CS1572  XML comment has a param tag, but there is no parameter by that name
        CS1574  XML comment has cref attribute that could not be resolved
        CS1570  XML comment has badly formed XML
        CS1587  XML comment is not placed on a valid language element

    The compiler is asked to build with the documentation diagnostics enabled
    (they are suppressed in the normal build via NoWarn in CSLibrary2026.csproj),
    and the warnings are then grouped per source file so that the remaining work
    can be planned file by file.

.PARAMETER TargetFramework
    Target framework to audit. The netstandard2.0 flavour is the published
    NuGet flavour (Plugin.BLE + TCP/IP HALs) and is the default.

.PARAMETER OutputPath
    Optional path of a CSV report with one row per finding.

.EXAMPLE
    pwsh -File Documents/API_Manual/tools/audit-xml-doc.ps1
    pwsh -File Documents/API_Manual/tools/audit-xml-doc.ps1 -TargetFramework net48
#>
[CmdletBinding()]
param(
    [string]$TargetFramework = 'netstandard2.0',
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
Push-Location $repoRoot

try {
    Write-Host "Auditing XML documentation for $TargetFramework ..." -ForegroundColor Cyan

    # -p:NoWarn= clears the csproj suppression so the documentation diagnostics surface.
    $arguments = @(
        'build', 'CSLibrary2026.csproj',
        '-c', 'Release',
        "-p:TargetFrameworks=$TargetFramework",
        '-p:GenerateDocumentationFile=true',
        '-p:NoWarn=',
        '-v', 'q', '--nologo',
        '--no-incremental'
    )

    $output = & dotnet @arguments 2>&1 | ForEach-Object { $_.ToString() }

    $pattern = '^(?<file>[^(]+)\((?<line>\d+),(?<col>\d+)\): warning (?<code>CS\d+): (?<message>.*)$'
    $findings = foreach ($line in $output) {
        if ($line -match $pattern) {
            [PSCustomObject]@{
                File    = $Matches['file']
                Line    = [int]$Matches['line']
                Column  = [int]$Matches['col']
                Code    = $Matches['code']
                Message = $Matches['message']
            }
        }
    }

    $docCodes = 'CS1591', 'CS1573', 'CS1572', 'CS1574', 'CS1570', 'CS1587'
    $docFindings = $findings | Where-Object { $_.Code -in $docCodes } |
        Sort-Object File, Line, Code, Message -Unique
    $other = $findings | Where-Object { $_.Code -notin $docCodes } |
        Sort-Object File, Line, Code, Message -Unique

    Write-Host ''
    Write-Host 'Documentation findings by code:' -ForegroundColor Cyan
    $docFindings | Group-Object Code | Sort-Object Name | ForEach-Object {
        Write-Host ("  {0}  {1,5}" -f $_.Name, $_.Count)
    }

    Write-Host ''
    Write-Host 'Findings by source file:' -ForegroundColor Cyan
    $byFile = $docFindings | Group-Object File | Sort-Object Count -Descending
    foreach ($group in $byFile) {
        $relative = $group.Name.Replace("$repoRoot\", '')
        Write-Host ("  {0,5}  {1}" -f $group.Count, $relative)
    }

    Write-Host ''
    Write-Host ("Total documentation findings: {0}" -f $docFindings.Count) -ForegroundColor Yellow

    if ($other) {
        Write-Host ''
        Write-Host 'Other build warnings (not documentation related):' -ForegroundColor DarkGray
        $other | Group-Object Code | Sort-Object Name | ForEach-Object {
            Write-Host ("  {0}  {1,5}" -f $_.Name, $_.Count)
        }
    }

    if ($OutputPath) {
        $resolved = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
            $OutputPath
        } else {
            Join-Path $repoRoot $OutputPath
        }
        $docFindings | Sort-Object File, Line | Export-Csv -Path $resolved -NoTypeInformation -Encoding utf8
        Write-Host ''
        Write-Host "Report written to $resolved" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
