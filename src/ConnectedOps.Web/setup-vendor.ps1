$ErrorActionPreference = "Stop"

$vendorRoot = "$PSScriptRoot\wwwroot\vendor"
New-Item -ItemType Directory -Force -Path "$vendorRoot\adminlte\css" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\adminlte\js" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\bootstrap\css" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\bootstrap\js" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\bootstrap-icons\font\fonts" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\chartjs" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\tabulator\css" | Out-Null
New-Item -ItemType Directory -Force -Path "$vendorRoot\tabulator\js" | Out-Null

Write-Host "Downloading AdminLTE 4.9.1..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/admin-lte@4.9.1/dist/css/adminlte.min.css" -OutFile "$vendorRoot\adminlte\css\adminlte.min.css"
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/admin-lte@4.9.1/dist/js/adminlte.min.js" -OutFile "$vendorRoot\adminlte\js\adminlte.min.js"

Write-Host "Downloading Bootstrap 5.3.3..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" -OutFile "$vendorRoot\bootstrap\css\bootstrap.min.css"
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js" -OutFile "$vendorRoot\bootstrap\js\bootstrap.bundle.min.js"

Write-Host "Downloading Bootstrap Icons 1.11.3..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" -OutFile "$vendorRoot\bootstrap-icons\font\bootstrap-icons.min.css"
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/fonts/bootstrap-icons.woff2" -OutFile "$vendorRoot\bootstrap-icons\font\fonts\bootstrap-icons.woff2"
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/fonts/bootstrap-icons.woff" -OutFile "$vendorRoot\bootstrap-icons\font\fonts\bootstrap-icons.woff"

Write-Host "Downloading Chart.js 4.4.7..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/chart.js@4.4.7/dist/chart.umd.js" -OutFile "$vendorRoot\chartjs\chart.umd.js"

Write-Host "Downloading Tabulator 6.3.0..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/tabulator-tables@6.3.0/dist/css/tabulator_bootstrap5.min.css" -OutFile "$vendorRoot\tabulator\css\tabulator_bootstrap5.min.css"
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/tabulator-tables@6.3.0/dist/js/tabulator.min.js" -OutFile "$vendorRoot\tabulator\js\tabulator.min.js"

Write-Host "Downloading MapLibre GL JS 4.7.1..."
New-Item -ItemType Directory -Force -Path "$vendorRoot\maplibre-gl" | Out-Null
Invoke-WebRequest -Uri "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js" -OutFile "$vendorRoot\maplibre-gl\maplibre-gl.js"
Invoke-WebRequest -Uri "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css" -OutFile "$vendorRoot\maplibre-gl\maplibre-gl.css"

Write-Host "Vendor assets downloaded successfully."
