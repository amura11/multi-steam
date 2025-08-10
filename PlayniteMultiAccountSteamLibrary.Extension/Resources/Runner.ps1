param(
    [Parameter(Mandatory = $true)]
    [string]$steamId,

    [Parameter(Mandatory = $true)]
    [string]$gameId,

    [Parameter(Mandatory = $true)]
    [string]$switcherPath,

    [int]$timeoutSeconds = 30
)

function Get-SteamInstallPath {
    $regPath = "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam"
    try {
        return (Get-ItemProperty -Path $regPath -Name "InstallPath").InstallPath
    } catch {
        Write-Error "Steam install path not found."
        return $null
    }
}

function Get-CurrentSteamUserId {
    param ([string]$steamPath)
    $loginUsersPath = Join-Path $steamPath "config\loginusers.vdf"

    if (-not (Test-Path $loginUsersPath)) {
        return $null
    }

    $fileContent = Get-Content $loginUsersPath -Raw
    $userPattern = '"(\d{17})"\s*\{[^}]*"MostRecent"\s*"1"'

    if ($fileContent -match $userPattern) {
        return $matches[1]
    }

    return $null
}

function Wait-ForSteamUserSwitch {
    param (
        [string]$expectedSteamId,
        [string]$loginUsersPath,
        [int]$timeoutSeconds
    )

    $initialSteamId = Get-CurrentSteamUserId -steamPath (Split-Path $loginUsersPath -Parent)
    Write-Host "Waiting for Steam user to switch..."

    $lastWrite = (Get-Item $loginUsersPath).LastWriteTimeUtc
    $elapsed = 0

    while ($elapsed -lt $timeoutSeconds) {
        Start-Sleep -Seconds 1
        $elapsed++

        $newWrite = (Get-Item $loginUsersPath).LastWriteTimeUtc
        if ($newWrite -ne $lastWrite) {
            $lastWrite = $newWrite
            $newSteamId = Get-CurrentSteamUserId -steamPath (Split-Path $loginUsersPath -Parent)
            
            if ($newSteamId -eq $expectedSteamId) {
                Write-Host "✅ Steam user switched to $expectedSteamId"
                return $true
            } else {
                Write-Host "⚠️  Detected Steam user switch to $newSteamId, waiting for $expectedSteamId"
            }
        }
    }

    Write-Warning "⏰ Timeout waiting for Steam account switch."
    return $false
}

function Wait-ForSteam {
    param (
        [int]$timeoutSeconds
    )

    Write-Host "Waiting for Steam process to be running..."
    $elapsed = 0
    
    while ($elapsed -lt $timeoutSeconds) {
        if (Get-Process -Name "steam" -ErrorAction SilentlyContinue) {
            Write-Host "✅ Steam is running."
            return $true
        }
        
        Start-Sleep -Seconds 1
        $elapsed++
    }

    Write-Warning "Steam did not start within $timeoutSeconds seconds."
    return $false
}

function Launch-SteamGame {
    param (
        [string]$appId
    )
    
    Write-Host "🎮 Launching game $appId..."
    Start-Process "steam://run/$appId"
}

# --- Main Execution ---
$steamInstallPath = Get-SteamInstallPath

if (-not $steamInstallPath) { exit 1 }

$loginUsersPath = Join-Path $steamInstallPath "config\loginusers.vdf"
$currentSteamId = Get-CurrentSteamUserId -steamPath $steamInstallPath

if ($currentSteamId -eq $steamId) {
    Write-Host "✅ Current Steam user matches expected ($steamId)."

    if (Wait-ForSteam -timeoutSeconds $timeoutSeconds) {
        Launch-SteamGame -appId $gameId
    }
} else {
    Write-Host "🔁 Current Steam user ($currentSteamId) does not match expected ($steamId). Launching switcher..."
    Start-Process -FilePath $switcherPath

    if (Wait-ForSteamUserSwitch -expectedSteamId $steamId -loginUsersPath $loginUsersPath -timeoutSeconds $timeoutSeconds) {
        if (Wait-ForSteam -timeoutSeconds $timeoutSeconds) {
            Launch-SteamGame -appId $gameId
        }
    } else {
        Write-Error "❌ Account switch failed or timed out."
    }
}
