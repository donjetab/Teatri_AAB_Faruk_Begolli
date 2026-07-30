param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [string]$OutputPath = "news-translation-template.json"
)

$ErrorActionPreference = "Stop"

$articles = Invoke-RestMethod -Uri "$ApiBaseUrl/api/sq/news"
$template = foreach ($article in ($articles | Sort-Object id)) {
    $encodedSlug = [Uri]::EscapeDataString($article.slug)
    $detail = Invoke-RestMethod -Uri "$ApiBaseUrl/api/sq/news/$encodedSlug"

    [ordered]@{
        id = $detail.id
        albanian = [ordered]@{
            title = $detail.title
            summary = $detail.summary
            body = $detail.content
        }
        english = [ordered]@{
            title = ""
            summary = ""
            body = ""
        }
    }
}

$resolvedOutput = Join-Path (Get-Location) $OutputPath
$template |
    ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $resolvedOutput -Encoding utf8

Write-Output "Created $resolvedOutput with $($template.Count) articles."
