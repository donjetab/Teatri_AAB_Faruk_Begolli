param(
    [string]$SqlServer = "localhost\SQLEXPRESS",
    [string]$Database = "TheatreAab",
    [string]$OutputRoot = ".\backups",
    [string]$UploadRoot = ".\backend\wwwroot\uploads"
)

$ErrorActionPreference = "Stop"
if ($Database -notmatch '^[A-Za-z0-9_]+$') { throw "Database name contains unsupported characters." }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw "sqlcmd is required. Install Microsoft SQL Server command-line utilities first." }

$workspaceRoot = (Resolve-Path -LiteralPath ".").Path
$outputBase = [IO.Path]::GetFullPath((Join-Path $workspaceRoot $OutputRoot))
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupDirectory = Join-Path $outputBase $stamp
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null

$databaseBackup = Join-Path $backupDirectory "$Database.bak"
$escapedBackup = $databaseBackup.Replace("'", "''")
sqlcmd -S $SqlServer -E -b -Q "BACKUP DATABASE [$Database] TO DISK = N'$escapedBackup' WITH COPY_ONLY, CHECKSUM, INIT"
sqlcmd -S $SqlServer -E -b -Q "RESTORE VERIFYONLY FROM DISK = N'$escapedBackup' WITH CHECKSUM"

$resolvedUploads = Resolve-Path -LiteralPath $UploadRoot
$mediaArchive = Join-Path $backupDirectory "uploads.zip"
Compress-Archive -LiteralPath $resolvedUploads.Path -DestinationPath $mediaArchive -CompressionLevel Optimal

$manifest = @(
    Get-FileHash -Algorithm SHA256 -LiteralPath $databaseBackup
    Get-FileHash -Algorithm SHA256 -LiteralPath $mediaArchive
) | Select-Object Path, Algorithm, Hash
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backupDirectory "manifest.json") -Encoding UTF8

Write-Host "Verified backup created at: $backupDirectory"
Write-Host "Copy this directory to encrypted storage outside the web server."
