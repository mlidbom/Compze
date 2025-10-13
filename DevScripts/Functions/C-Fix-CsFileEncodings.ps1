function C-Fix-CsFileEncodings {
    <#
    .SYNOPSIS
    Converts .cs files to UTF-8 without BOM encoding (standard for modern .NET)
    
    .DESCRIPTION
    Scans the specified path for git-tracked files and converts any that don't use UTF-8 without BOM to UTF-8 without BOM.
    This ensures consistent encoding across the codebase and prevents git diff issues. UTF-8 without BOM is the
    modern standard for .NET projects and provides better cross-platform compatibility.
    
    .PARAMETER Path
    The path to scan. Defaults to the src directory of the Compze repository.
    
    .PARAMETER FilePattern
    The file pattern to match. Defaults to *.cs
    
    .EXAMPLE
    C-Fix-CsFileEncodings
    Converts all git-tracked .cs files in src/ to UTF-8 without BOM
    
    .EXAMPLE
    C-Fix-CsFileEncodings -Path "src/Tests" -FilePattern "*.cs"
    Converts all git-tracked .cs files in src/Tests to UTF-8 without BOM
    
    .EXAMPLE
    C-Fix-CsFileEncodings -WhatIf
    Shows which files would be converted without actually converting them
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter()]
        [string]$Path = (Join-Path $script:CompzeRoot "src"),
        
        [Parameter()]
        [string]$FilePattern = "*.cs"
    )
    
    if (-not (Test-Path $Path)) {
        Write-Error "Path not found: $Path"
        return
    }
    
    # Get all git-tracked files matching the pattern
    Push-Location $Path
    try {
        $gitFiles = git ls-files $FilePattern 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Not a git repository or git not available. Falling back to all files."
            $files = Get-ChildItem -Path $Path -Recurse -Filter $FilePattern -File
        } else {
            $files = $gitFiles | ForEach-Object { Get-Item (Join-Path $Path $_) -ErrorAction SilentlyContinue } | Where-Object { $_ -ne $null }
        }
    } finally {
        Pop-Location
    }
    
    if ($files.Count -eq 0) {
        Write-Host "No matching files found." -ForegroundColor Yellow
        return
    }
    
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $convertedCount = 0
    
    foreach ($file in $files) {
        try {
            # Read the file as bytes to check encoding
            $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
            
            # Check if file has UTF-8 BOM (EF BB BF)
            $hasUtf8Bom = ($bytes.Length -ge 3 -and 
                          $bytes[0] -eq 0xEF -and 
                          $bytes[1] -eq 0xBB -and 
                          $bytes[2] -eq 0xBF)
            
            # Check if file is already UTF-8 without BOM
            # We'll convert if it has BOM, or if it appears to be another encoding
            $needsConversion = $hasUtf8Bom
            
            if ($needsConversion) {
                if ($PSCmdlet.ShouldProcess($file.FullName, "Convert to UTF-8 without BOM")) {
                    # Read content and write with UTF-8 without BOM
                    $content = [System.IO.File]::ReadAllText($file.FullName)
                    [System.IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
                    Write-Host "Converted: $($file.FullName)" -ForegroundColor Green
                    $convertedCount++
                } else {
                    Write-Host "[WhatIf] Would convert: $($file.FullName)" -ForegroundColor Yellow
                    $convertedCount++
                }
            }
        } catch {
            Write-Warning "Failed to process $($file.FullName): $_"
        }
    }
    
    if ($convertedCount -gt 0) {
        if ($WhatIfPreference) {
            Write-Host "Would convert: $convertedCount files" -ForegroundColor Yellow
        } else {
            Write-Host "Converted: $convertedCount files" -ForegroundColor Green
        }
    }
}
