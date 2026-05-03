# Coverage: HTML chỉ BusinessLogicLayer + WebAPI.Controllers (loại DAL, Program, test).
# Cobertura vẫn có thể chứa DAL nếu test gọi trực tiếp — bước ReportGenerator lọc hiển thị.
$ErrorActionPreference = "Stop"
$Root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent  # backend/
$UnitTests = Join-Path $Root "UnitTests"
$Results = Join-Path $UnitTests "TestResults\coverage-bll-controllers"
$HtmlOut = Join-Path $UnitTests "TestResults\coverage-html-bll-controllers"

Push-Location $Root
try {
    dotnet test "UnitTests\UnitTests.csproj" `
        --collect:"XPlat Code Coverage" `
        --settings "UnitTests\coverage-targeted.runsettings" `
        --results-directory $Results

    $cobertura = Get-ChildItem -Path $Results -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1
    if (-not $cobertura) { throw "Không tìm thấy coverage.cobertura.xml trong $Results" }

    if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
        dotnet tool install dotnet-reportgenerator-globaltool --global
    }

    New-Item -ItemType Directory -Force -Path $HtmlOut | Out-Null
    reportgenerator `
        "-reports:$($cobertura.FullName)" `
        "-targetdir:$HtmlOut" `
        "-reporttypes:Html" `
        "-assemblyfilters:+BusinessLogicLayer*;+WebAPI*;-DataAccessLayer*;-UnitTests*;-xunit*" `
        "-classfilters:+BusinessLogicLayer*;+WebAPI.Controllers*"

    Write-Host "HTML (BLL + Controllers): $(Join-Path $HtmlOut 'index.html')"
}
finally {
    Pop-Location
}
