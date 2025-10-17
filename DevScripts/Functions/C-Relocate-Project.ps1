function C-Relocate-Project {
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        [string]$SolutionPath
    )
    
    if (-not $SolutionPath) { $SolutionPath = $script:CompzeSolutionPath }
    if (-not (Test-Path $SolutionPath)) { Write-Error "Solution file not found: $SolutionPath"; return }
    
    $solutionDir = Split-Path -Parent $SolutionPath
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "PROJECT RELOCATION OPERATION" -ForegroundColor Cyan
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "Project: $ProjectName" -ForegroundColor Yellow
    Write-Host ""
    
    # Step 1: Find project
    $projectFileName = "$ProjectName.csproj"
    $projectFile = Get-ChildItem -Path $solutionDir -Filter $projectFileName -Recurse | Select-Object -First 1
    if (-not $projectFile) { Write-Error "Project file not found"; return }
    
    $currentProjectDir = Split-Path -Parent $projectFile.FullName
    Write-Host "Current location: $($currentProjectDir.Substring($solutionDir.Length + 1))" -ForegroundColor Yellow
    
    # Step 2: Calculate target
    $targetRelativePath = $ProjectName -replace '\.', '\'
    $targetProjectDir = Join-Path $solutionDir $targetRelativePath
    $targetSolutionPath = $ProjectName -replace '\.', '/'
    $targetSolutionFolder = "/" + ($ProjectName -replace '\.', '/')
    $targetSolutionFolder = $targetSolutionFolder.Substring(0, $targetSolutionFolder.LastIndexOf('/'))
    
    Write-Host "Target location: $targetRelativePath" -ForegroundColor Green
    Write-Host ""
    
    if ($currentProjectDir -eq $targetProjectDir) {
        Write-Host "Already in correct location!" -ForegroundColor Green
        return
    }
    
    # Step 3: Move directory
    if (Test-Path $targetProjectDir) { Write-Error "Target already exists"; return }
    $targetParentDir = Split-Path -Parent $targetProjectDir
    if (-not (Test-Path $targetParentDir)) {
        New-Item -ItemType Directory -Path $targetParentDir -Force | Out-Null
    }
    Move-Item -Path $currentProjectDir -Destination $targetProjectDir
    Write-Host "Moved to: $targetRelativePath" -ForegroundColor Green
    Write-Host ""
    
    # Step 4: Update csproj references
    $updated = 0
    Get-ChildItem -Path $solutionDir -Filter "*.csproj" -Recurse | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $pattern = '(<ProjectReference\s+Include=")([^"]*[/\\]' + [regex]::Escape($ProjectName) + '\.csproj)(")'
        if ($content -match $pattern) {
            $csprojDir = Split-Path -Parent $_.FullName
            $newPath = [System.IO.Path]::GetRelativePath($csprojDir, (Join-Path $targetProjectDir $projectFileName))
            $newContent = $content -replace $pattern, ('$1' + $newPath + '$3')
            if ($content -ne $newContent) {
                Set-Content -Path $_.FullName -Value $newContent -NoNewline -Encoding UTF8
                $updated++
                Write-Host "Updated: $($_.Name)" -ForegroundColor Green
            }
        }
    }
    Write-Host "Updated $updated ProjectReference(s)" -ForegroundColor Green
    Write-Host ""
    
    # Step 5: Update solution paths
    $slnUpdated = 0
    Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $pattern = '(<Project\s+Path=")([^"]*[/\\]' + [regex]::Escape($ProjectName) + '\.csproj)(")'
        if ($content -match $pattern) {
            $newPath = $targetSolutionPath + '/' + $projectFileName
            $newContent = $content -replace $pattern, ('$1' + $newPath + '$3')
            if ($content -ne $newContent) {
                Set-Content -Path $_.FullName -Value $newContent -NoNewline -Encoding UTF8
                $slnUpdated++
                Write-Host "Updated solution path: $($_.Name)" -ForegroundColor Green
            }
        }
    }
    Write-Host "Updated $slnUpdated solution file(s)" -ForegroundColor Green
    Write-Host ""
    
    # Step 6: Update solution folders
    $folderUpdated = 0
    Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse | ForEach-Object {
        [xml]$xml = Get-Content $_.FullName
        $modified = $false
        $xml.SelectNodes("//Project[@Path]") | ForEach-Object {
            if ($_.Path -like "*$ProjectName.csproj") {
                $currentFolder = $_.ParentNode
                if ($currentFolder.Name -eq "Folder") {
                    $currentFolderName = $currentFolder.GetAttribute("Name")
                    if ($currentFolderName -ne $targetSolutionFolder) {
                        $currentFolder.RemoveChild($_) | Out-Null
                        $targetFolder = $xml.SelectSingleNode("//Folder[@Name='$targetSolutionFolder']")
                        if (-not $targetFolder) {
                            $targetFolder = $xml.CreateElement("Folder")
                            $targetFolder.SetAttribute("Name", $targetSolutionFolder)
                            $xml.DocumentElement.AppendChild($targetFolder) | Out-Null
                        }
                        $targetFolder.AppendChild($_) | Out-Null
                        $modified = $true
                    }
                }
            }
        }
        if ($modified) {
            $xml.Save($_.FullName)
            $folderUpdated++
            Write-Host "Updated solution folder: $($_.Name)" -ForegroundColor Green
        }
    }
    Write-Host "Updated $folderUpdated solution folder(s)" -ForegroundColor Green
    Write-Host ""
    Write-Host "COMPLETED" -ForegroundColor Green
}
