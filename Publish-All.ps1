$publishPath = "M:\PointlessWaymarksPublications\VibrationMonitorProject"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

dotnet publish .\VibrationMonitor\VibrationMonitor.csproj /p:PublishProfile=.\VibrationMonitor\Properties\PublishProfiles\FolderProfile.pubxml

dotnet publish .\VibrationMonitorApi\VibrationMonitorApi.csproj /p:PublishProfile=.\VibrationMonitorApi\Properties\PublishProfiles\FolderProfile.pubxml

$publishVersion = (Get-Date).ToString("yyyy-MM-dd-HH-mm")
$destinationZipFile = "M:\PointlessWaymarksPublications\VibrationMonitorProject-Zip--{0}.zip" -f $publishVersion

Compress-Archive -Path M:\PointlessWaymarksPublications\VibrationMonitorProject -DestinationPath $destinationZipFile

Write-Output "VibrationMonitorProject zipped to '$destinationZipFile'"

if ($lastexitcode -ne 0) {throw ("Exec: " + $errorMessage) }