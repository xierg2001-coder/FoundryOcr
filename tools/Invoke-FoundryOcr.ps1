function Invoke-FoundryOcr {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false, Position=0)]
        [string]$ImagePath,
        [switch]$Pretty,
        [string]$Base64,
        [switch]$FromStdin,
        [string]$CliPath
    )

    if (-not $CliPath) {
        $CliPath = Join-Path $PSScriptRoot '..\src\FoundryOcr.Cliin\Release
et8.0-windows10.0.19041.0\win-x64\publish\FoundryOcr.Cli.exe'
    }
    if (-not (Test-Path $CliPath)) { throw "CLI not found at $CliPath" }

    $args = @()
    if ($FromStdin -and $Base64) { $args += '--stdin --base64' }
    elseif ($FromStdin)          { $args += '--stdin' }
    elseif ($Base64)             { $args += @('--base64', $Base64) }
    elseif ($ImagePath)          { $args += $ImagePath }
    else { throw "Provide -ImagePath OR -Base64 OR -FromStdin with piped input." }

    if ($Pretty) { $args += '--pretty' }

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo -Property @{
        FileName = $CliPath
        Arguments = [string]::Join(' ', $args)
        RedirectStandardOutput = $true
        RedirectStandardError  = $true
        RedirectStandardInput  = $FromStdin
        UseShellExecute        = $false
        CreateNoWindow         = $true
    }

    $proc = [System.Diagnostics.Process]::Start($pinfo)

    if ($FromStdin) {
        [Console]::OpenStandardInput().CopyTo($proc.StandardInput.BaseStream)
        $proc.StandardInput.Close()
    }

    $out = $proc.StandardOutput.ReadToEnd()
    $err = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()

    if ($proc.ExitCode -ne 0) { throw "Foundry OCR failed ($($proc.ExitCode)): $err" }

    return $out | ConvertFrom-Json
}
Export-ModuleMember -Function Invoke-FoundryOcr
