$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
$instance = 'BaiTH_' + [Guid]::NewGuid().ToString('N')
$created = $false
try {
    $localdb = (Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue).Source
    if (!$localdb) {
        $localdb = Get-ChildItem "${env:ProgramFiles}\Microsoft SQL Server\*\Tools\Binn\SqlLocalDB.exe" | Select-Object -Last 1 -ExpandProperty FullName
    }
    if (!$localdb) { throw 'SQL Server Express LocalDB is required.' }
    & $localdb create $instance -s
    if ($LASTEXITCODE -ne 0) { throw 'LocalDB creation failed.' }
    $created = $true
    dotnet run --project tools/ChupAnh/ChupAnh.csproj -c Release -- $instance
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot run failed.' }
    Write-Host '15 PNG saved to images.'
}
finally {
    if ($created) {
        & $localdb stop $instance -k
        & $localdb delete $instance
    }
    Pop-Location
}
