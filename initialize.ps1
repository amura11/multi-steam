# PowerShell script to download and extract the latest Playnite release

# GitHub API endpoint for the latest release
$apiUrl = "https://api.github.com/repos/JosefNemec/Playnite/releases/latest"

# Custom User-Agent header is required by GitHub API
$headers = @{ "User-Agent" = "PowerShell" }

# Get the latest release info
Write-Host "Fetching latest release info..."
$release = Invoke-RestMethod -Uri $apiUrl -Headers $headers

# Find the first asset that ends with .zip
$asset = $release.assets | Where-Object { $_.browser_download_url -like "*.zip" } | Select-Object -First 1

if (-not $asset) {
    Write-Error "No ZIP asset found in the latest release."
    exit 1
}

$zipUrl = $asset.browser_download_url
$zipFile = Join-Path $PSScriptRoot "Playnite.zip"
$extractPath = Join-Path $PSScriptRoot "Playnite"

Write-Host "Downloading latest Playnite from $zipUrl..."
Invoke-WebRequest -Uri $zipUrl -OutFile $zipFile

# Ensure the extract path exists
if (-Not (Test-Path $extractPath)) {
    New-Item -ItemType Directory -Path $extractPath | Out-Null
}

Write-Host "Extracting ZIP to $extractPath..."
Expand-Archive -Path $zipFile -DestinationPath $extractPath -Force

# Clean up the downloaded ZIP
Remove-Item $zipFile

Write-Host "Playnite has been downloaded and extracted to $extractPath"
