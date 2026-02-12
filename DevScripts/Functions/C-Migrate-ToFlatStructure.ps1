function C-Migrate-ToFlatStructure {
   <#
   .SYNOPSIS
   Migrates from nested project structure to standard flat .NET layout.
   
   .DESCRIPTION
   Moves all Compze library projects to flat directories under src/ and all test projects
   to flat directories under test/. Uses git mv to preserve history. Updates all
   ProjectReference paths, the solution file, Directory.Build.props hierarchy, and
   removes the now-unnecessary Compile Remove exclusions for child projects.

   .PARAMETER DryRun
   If specified, shows what would be done without making changes.

   .EXAMPLE
   C-Migrate-ToFlatStructure -DryRun
   C-Migrate-ToFlatStructure
   #>
   [CmdletBinding()]
   [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
   param(
      [switch]$DryRun
   )

   $solutionPath = $script:CompzeSolutionPath
   $srcDir = $script:CompzeSrcRoot               # c:\Dev\Compze\src
   $repoRoot = $script:CompzeRoot                 # c:\Dev\Compze
   $testDir = Join-Path $repoRoot "test"

   if (-not (Test-Path $solutionPath)) {
      Write-Error "Solution file not found: $solutionPath"
      return
   }

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 0: Define the migration plan — current path → target path
   # ═══════════════════════════════════════════════════════════════════════════
   
   # Each entry: ProjectName, CurrentDirRelativeToSrc, TargetDirAbsolute, IsTestProject
   # Order matters: leaf (deepest nested) projects FIRST, parents LAST
   $migrationPlan = @(
      # ── Leaf library projects (no children) ──
      @{ Name = "Compze.Utilities.DependencyInjection.Microsoft"; IsTest = $false }
      @{ Name = "Compze.Utilities.DependencyInjection.SimpleInjector"; IsTest = $false }
      @{ Name = "Compze.Utilities.Logging.Serilog"; IsTest = $false }
      @{ Name = "Compze.Utilities.Testing.DbPool"; IsTest = $false }
      @{ Name = "Compze.Utilities.Testing.Must"; IsTest = $false }
      @{ Name = "Compze.Utilities.Testing.XUnit.Tests"; IsTest = $true }
      @{ Name = "Compze.Utilities.Testing.XUnit"; IsTest = $false }
      @{ Name = "Compze.Utilities.Tests"; IsTest = $true }
      @{ Name = "Compze.Tessaging.Hosting.AspNetCore"; IsTest = $false }
      @{ Name = "Compze.Tessaging.Hosting.Testing"; IsTest = $false }
      @{ Name = "Compze.Tessaging.Teventive.TeventStore"; IsTest = $false }
      @{ Name = "Compze.Serialization.Newtonsoft"; IsTest = $false }
      @{ Name = "Compze.Sql.Common"; IsTest = $false }
      @{ Name = "Compze.Sql.MicrosoftSql"; IsTest = $false }
      @{ Name = "Compze.Sql.MySql"; IsTest = $false }
      @{ Name = "Compze.Sql.PostgreSql"; IsTest = $false }
      @{ Name = "Compze.Sql.Sqlite"; IsTest = $false }

      # ── Test projects under src/Tests/ ──
      @{ Name = "Compze.Tests.Unit.Internals"; IsTest = $true }
      @{ Name = "Compze.Tests.Performance.Internals"; IsTest = $true }
      @{ Name = "Compze.Tests.Unit"; IsTest = $true }
      @{ Name = "Compze.Tests.Integration"; IsTest = $true }
      @{ Name = "Compze.Tests.Common"; IsTest = $true }
      @{ Name = "Compze.Tests.Infrastructure"; IsTest = $true }
      @{ Name = "Compze.Tests.ScratchPad"; IsTest = $true }
      @{ Name = "Compze.Tests.CodePolicies"; IsTest = $true }

      # ── Parent library projects (must move AFTER children are extracted) ──
      @{ Name = "Compze.Tessaging"; IsTest = $false }
      @{ Name = "Compze.Utilities"; IsTest = $false }
      @{ Name = "Compze.Core"; IsTest = $false }
   )

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 1: Discover current locations of all projects
   # ═══════════════════════════════════════════════════════════════════════════

   $moves = @()
   foreach ($entry in $migrationPlan) {
      $projectName = $entry.Name
      $csprojFileName = "$projectName.csproj"

      # Find the current location
      $projectFile = Find-ProjectFile -SolutionPath $solutionPath -ProjectName $projectName
      if (-not $projectFile) {
         Write-Warning "Could not find project: $projectName — skipping"
         continue
      }

      $currentDir = $projectFile.Directory.FullName

      # Calculate target directory
      if ($entry.IsTest) {
         $targetDir = Join-Path $testDir $projectName
      } else {
         $targetDir = Join-Path $srcDir $projectName
      }

      # Skip if already at target
      if ($currentDir.TrimEnd('\') -eq $targetDir.TrimEnd('\')) {
         continue
      }

      $moves += @{
         Name          = $projectName
         CsprojFile    = $csprojFileName
         CurrentDir    = $currentDir
         TargetDir     = $targetDir
         IsTest        = $entry.IsTest
      }
   }

   if ($moves.Count -eq 0) {
      Write-Host "All projects are already at their target locations."
      return
   }

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 1.5: Show the plan
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host ""
   Write-Host "Migration Plan: $($moves.Count) projects to move" -ForegroundColor Cyan
   Write-Host ("=" * 70)
   foreach ($move in $moves) {
      $fromRel = $move.CurrentDir.Substring($repoRoot.Length + 1)
      $toRel = $move.TargetDir.Substring($repoRoot.Length + 1)
      Write-Host "  $($move.Name)" -ForegroundColor Yellow
      Write-Host "    FROM: $fromRel"
      Write-Host "    TO:   $toRel"
   }
   Write-Host ""

   if ($DryRun) {
      Write-Host "[DRY RUN] No changes made." -ForegroundColor Green
      return
   }

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 2: Create test/ directory
   # ═══════════════════════════════════════════════════════════════════════════

   if (-not (Test-Path $testDir)) {
      New-Item -ItemType Directory -Path $testDir -Force | Out-Null
   }

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 3: Move projects (leaf-first order, using git mv)
   # ═══════════════════════════════════════════════════════════════════════════

   Push-Location $repoRoot
   try {
      foreach ($move in $moves) {
         $currentDir = $move.CurrentDir
         $targetDir = $move.TargetDir

         # Clean bin/obj in the source before moving
         $binDir = Join-Path $currentDir "bin"
         $objDir = Join-Path $currentDir "obj"
         if (Test-Path $binDir) { Remove-Item $binDir -Recurse -Force }
         if (Test-Path $objDir) { Remove-Item $objDir -Recurse -Force }

         # Ensure target parent exists
         $targetParent = Split-Path -Parent $targetDir
         if (-not (Test-Path $targetParent)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
         }

         # Check if target is inside source (shouldn't happen for flatten, but be safe)
         $normalizedCurrent = $currentDir.TrimEnd('\') + '\'
         $normalizedTarget = $targetDir.TrimEnd('\') + '\'
         if ($normalizedTarget.StartsWith($normalizedCurrent, [StringComparison]::OrdinalIgnoreCase)) {
            # Target is a subdirectory of source — use temp dir
            $tempDir = Join-Path $srcDir ("_temp_migrate_" + [Guid]::NewGuid().ToString().Substring(0, 8))
            git mv $currentDir $tempDir 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
               Write-Error "git mv failed for $($move.Name) (to temp)"
               return
            }
            git mv $tempDir $targetDir 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
               Write-Error "git mv failed for $($move.Name) (from temp)"
               return
            }
         } else {
            # If target already exists as an empty dir, remove it
            if (Test-Path $targetDir) {
               $existingFiles = Get-ChildItem -Path $targetDir -Recurse -File
               if ($existingFiles.Count -gt 0) {
                  Write-Error "Target directory is not empty: $targetDir"
                  return
               }
               Remove-Item $targetDir -Recurse -Force
            }
            git mv $currentDir $targetDir 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
               # git mv might fail if there are untracked files - try file-by-file
               Write-Host "  git mv directory failed for $($move.Name), trying file-by-file..." -ForegroundColor Yellow
               if (-not (Test-Path $targetDir)) {
                  New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
               }
               Move-ProjectContents -SourceDir $currentDir -TargetDir $targetDir
            }
         }
         Write-Host "  Moved: $($move.Name)" -ForegroundColor Green
      }
   } finally {
      Pop-Location
   }

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 4: Update ALL ProjectReference paths in ALL csproj files
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host ""
   Write-Host "Updating ProjectReference paths..." -ForegroundColor Cyan

   # Build a lookup: csproj filename → new absolute path
   $projectLocations = @{}
   $allCsprojFiles = @()
   $allCsprojFiles += Get-ChildItem -Path $srcDir -Filter "*.csproj" -Recurse | Where-Object { $_.FullName -notmatch '\\nCrunchTemp' }
   $allCsprojFiles += Get-ChildItem -Path $testDir -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\nCrunchTemp' }

   foreach ($f in $allCsprojFiles) {
      $projectLocations[$f.Name] = $f.FullName
   }

   foreach ($csprojFile in $allCsprojFiles) {
      $content = Get-Content $csprojFile.FullName -Raw
      $csprojDir = $csprojFile.Directory.FullName
      $modified = $false

      $referencePattern = '(<ProjectReference\s+Include=")([^"]+)(")'
      $newContent = [regex]::Replace($content, $referencePattern, {
         param($match)
         $prefix = $match.Groups[1].Value
         $oldPath = $match.Groups[2].Value
         $suffix = $match.Groups[3].Value

         # Extract the csproj filename from the path
         $referencedFileName = Split-Path -Leaf $oldPath

         if ($projectLocations.ContainsKey($referencedFileName)) {
            $absoluteTarget = $projectLocations[$referencedFileName]
            $newRelPath = [System.IO.Path]::GetRelativePath($csprojDir, $absoluteTarget)
            if ($newRelPath -ne $oldPath) {
               $script:modified = $true
            }
            return "$prefix$newRelPath$suffix"
         }
         return $match.Value
      })

      if ($newContent -ne $content) {
         Set-Content -Path $csprojFile.FullName -Value $newContent -NoNewline -Encoding UTF8
      }
   }

   Write-Host "  ProjectReference paths updated." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 5: Remove auto-generated Compile Remove sections for child projects
   #         (keep _docs sections)
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Removing child-project exclusion sections..." -ForegroundColor Cyan

   foreach ($csprojFile in $allCsprojFiles) {
      $content = Get-Content $csprojFile.FullName -Raw

      # Remove the "Exclude subdirectories" comment + ItemGroup blocks
      $pattern = '(?s)\s*<!-- Exclude subdirectories that have their own \.csproj files.*?</ItemGroup>'
      $newContent = [regex]::Replace($content, $pattern, '')

      if ($newContent -ne $content) {
         Set-Content -Path $csprojFile.FullName -Value $newContent -NoNewline -Encoding UTF8
         Write-Host "  Cleaned: $($csprojFile.Name)" -ForegroundColor Green
      }
   }

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 6: Update the solution file (Compze.slnx)
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Updating solution file..." -ForegroundColor Cyan

   [xml]$slnx = Get-Content $solutionPath
   $solutionDir = Split-Path -Parent $solutionPath  # src/

   # Update Project Path attributes
   $projectElements = $slnx.SelectNodes("//Project[@Path]")
   foreach ($elem in $projectElements) {
      $oldPath = $elem.GetAttribute("Path")
      $csprojFileName = Split-Path -Leaf $oldPath

      if ($projectLocations.ContainsKey($csprojFileName)) {
         $absolutePath = $projectLocations[$csprojFileName]
         $newRelPath = [System.IO.Path]::GetRelativePath($solutionDir, $absolutePath) -replace '\\', '/'
         $elem.SetAttribute("Path", $newRelPath)
      }
   }

   # Reorganize solution folders:
   # Library projects → /Compze/ or /Compze/SubGroup/
   # Test projects → /_Tests/
   # Remove old folder structure and rebuild

   # First, collect all project elements and remove them from their current parents
   $projectElements = @($slnx.SelectNodes("//Project[@Path]"))
   foreach ($elem in $projectElements) {
      $elem.ParentNode.RemoveChild($elem) | Out-Null
   }

   # Remove all now-empty Folder elements (but preserve _Samples, _Solution Items, ~Solution Structure, _Websites, etc.)
   $foldersToRemove = @()
   $folderElements = @($slnx.SelectNodes("//Folder[@Name]"))
   foreach ($folder in $folderElements) {
      $name = $folder.GetAttribute("Name")
      if ($name -match '^/Compze' -or $name -eq '/_Tests/') {
         $foldersToRemove += $folder
      }
   }
   foreach ($folder in $foldersToRemove) {
      $folder.ParentNode.RemoveChild($folder) | Out-Null
   }

   # Create new folder structure and place projects
   foreach ($elem in $projectElements) {
      $path = $elem.GetAttribute("Path")
      $csprojName = [System.IO.Path]::GetFileNameWithoutExtension($path)

      # Determine the solution folder
      $solutionFolder = Get-SolutionFolder -ProjectName $csprojName -ProjectPath $path

      # Find or create the folder
      $folderElem = $slnx.SelectSingleNode("//Folder[@Name='$solutionFolder']")
      if (-not $folderElem) {
         $folderElem = $slnx.CreateElement("Folder")
         $folderElem.SetAttribute("Name", $solutionFolder)
         $slnx.DocumentElement.AppendChild($folderElem) | Out-Null
      }

      $folderElem.AppendChild($elem) | Out-Null
   }

   $slnx.Save($solutionPath)
   Write-Host "  Solution file updated." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 7: Create test/Directory.Build.props
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Creating test/Directory.Build.props..." -ForegroundColor Cyan

   $testDirBuildProps = @"
<Project>
`t<!-- Import src/Directory.Build.props for shared settings -->
`t<Import Project="`$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '`$(MSBuildThisFileDirectory)../'))" />

`t<PropertyGroup>
`t`t<IsTestProject>true</IsTestProject>
`t</PropertyGroup>
</Project>
"@

   $testDirBuildPropsPath = Join-Path $testDir "Directory.Build.props"
   Set-Content -Path $testDirBuildPropsPath -Value $testDirBuildProps -NoNewline -Encoding UTF8

   # git add the new file
   Push-Location $repoRoot
   git add $testDirBuildPropsPath 2>&1 | Out-Null
   Pop-Location

   Write-Host "  Created test/Directory.Build.props" -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 8: Merge src/Compze/Directory.Build.props into src/Directory.Build.props
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Restructuring Directory.Build.props..." -ForegroundColor Cyan

   $srcDirBuildProps = Join-Path $srcDir "Directory.Build.props"
   $compzeDirBuildProps = Join-Path $srcDir "Compze" "Directory.Build.props"

   # Read existing src/Directory.Build.props
   $srcProps = Get-Content $srcDirBuildProps -Raw

   # Read src/Compze/Directory.Build.props for the NuGet metadata and InternalsVisibleTo
   if (Test-Path $compzeDirBuildProps) {
      $compzeProps = Get-Content $compzeDirBuildProps -Raw

      # Extract the PropertyGroup with NuGet metadata
      $nugetPropGroup = ""
      if ($compzeProps -match '(?s)(<PropertyGroup>\s*<Authors>.*?</PropertyGroup>)') {
         $nugetPropGroup = $Matches[1]
      }

      # Extract the InternalsVisibleTo ItemGroup
      $internalsItemGroup = ""
      if ($compzeProps -match '(?s)(<ItemGroup>\s*<!-- InternalsVisibleTo.*?</ItemGroup>)') {
         $internalsItemGroup = $Matches[1]
      }

      # Extract the SourceLink and README ItemGroups
      $sourcelinkItemGroup = ""
      if ($compzeProps -match '(?s)(<ItemGroup>\s*<PackageReference Include="Microsoft\.SourceLink\.GitHub".*?</ItemGroup>)') {
         $sourcelinkItemGroup = $Matches[1]
      }

      $readmeItemGroup = ""
      if ($compzeProps -match '(?s)(<ItemGroup>\s*<None Include="\$\(MSBuildProjectDirectory\)/README\.md".*?</ItemGroup>)') {
         $readmeItemGroup = $Matches[1]
      }

      # Now update src/Directory.Build.props: Remove the old IsTestProject detection and add
      # the library project properties (conditioned on NOT being a test project)
      $newSrcProps = @"
<Project>
`t<!-- Enable package validation for all projects -->
`t<Import Project="msbuild\EnablePackageValidation.props" />
`t
`t<!-- Ensure test configuration file exists before build -->
`t<Import Project="msbuild\EnsurePluggableComponentsConfigExists.props" />

`t<!-- Enable latest analyzer rules for all projects -->
`t<PropertyGroup>
`t`t<AnalysisLevel>latest-all</AnalysisLevel>
`t`t<RunAnalyzersDuringLiveAnalysis>False</RunAnalyzersDuringLiveAnalysis>
`t`t<NuGetAuditMode>direct</NuGetAuditMode>
`t</PropertyGroup>

`t<Import Project="msbuild\IncludeTestConfigurationFiles.props" Condition="'`$(IsTestProject)' == 'true'" />
`t<Import Project="msbuild\IncludeTestAppSettings.props" Condition="'`$(IsTestProject)' == 'true'" />

`t<!-- NuGet package metadata for library projects -->
`t<PropertyGroup Condition="'`$(IsTestProject)' != 'true' AND '`$(IsPackable)' != 'false'">
`t`t<Authors>Lidbom Solutions AB</Authors>
`t`t<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
`t`t<PackageProjectUrl>https://github.com/mlidbom/Compze</PackageProjectUrl>
`t`t<RepositoryUrl>https://github.com/mlidbom/Compze.git</RepositoryUrl>
`t`t<RepositoryType>git</RepositoryType>
`t`t<PackageTags>compze;messaging;event-sourcing;cqrs</PackageTags>
`t`t<Copyright>Copyright (c) Lidbom Solutions AB. All rights reserved.</Copyright>
`t`t<PublishRepositoryUrl>true</PublishRepositoryUrl>
`t`t<EmbedUntrackedSources>true</EmbedUntrackedSources>
`t`t<IncludeSymbols>true</IncludeSymbols>
`t`t<SymbolPackageFormat>snupkg</SymbolPackageFormat>
`t`t<PackageReadmeFile>README.md</PackageReadmeFile>
`t</PropertyGroup>

`t<ItemGroup Condition="'`$(IsTestProject)' != 'true' AND '`$(IsPackable)' != 'false'">
`t`t<PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
`t</ItemGroup>

`t<ItemGroup Condition="'`$(IsTestProject)' != 'true' AND '`$(IsPackable)' != 'false'">
`t`t<None Include="`$(MSBuildProjectDirectory)/README.md" Pack="true" PackagePath="" />
`t</ItemGroup>

`t$internalsItemGroup
</Project>
"@

      Set-Content -Path $srcDirBuildProps -Value $newSrcProps -NoNewline -Encoding UTF8

      # Remove the old src/Compze/Directory.Build.props
      Push-Location $repoRoot
      git rm $compzeDirBuildProps 2>&1 | Out-Null
      Pop-Location
   }

   Write-Host "  Directory.Build.props restructured." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 9: Move test-common-appsettings.json to test/
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Moving test-common-appsettings.json..." -ForegroundColor Cyan

   $oldAppSettings = Join-Path $srcDir "Compze" "test-common-appsettings.json"
   $newAppSettings = Join-Path $testDir "test-common-appsettings.json"
   if (Test-Path $oldAppSettings) {
      Push-Location $repoRoot
      if (-not (Test-Path $testDir)) { New-Item -ItemType Directory -Path $testDir -Force | Out-Null }
      git mv $oldAppSettings $newAppSettings 2>&1 | Out-Null
      Pop-Location
   }

   # Update IncludeTestAppSettings.props to point to test/
   $includeTestAppSettingsPath = Join-Path $srcDir "msbuild" "IncludeTestAppSettings.props"
   if (Test-Path $includeTestAppSettingsPath) {
      $content = Get-Content $includeTestAppSettingsPath -Raw
      $content = $content -replace '\$\(MSBuildThisFileDirectory\)\.\.\\Compze\\test-common-appsettings\.json', '$(MSBuildThisFileDirectory)..\..\test\test-common-appsettings.json'
      Set-Content -Path $includeTestAppSettingsPath -Value $content -NoNewline -Encoding UTF8
   }

   Write-Host "  test-common-appsettings.json moved and refs updated." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 10: Move Tests/.editorconfig to test/
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Moving Tests/.editorconfig..." -ForegroundColor Cyan

   $oldEditorConfig = Join-Path $srcDir "Tests" ".editorconfig"
   $newEditorConfig = Join-Path $testDir ".editorconfig"
   if (Test-Path $oldEditorConfig) {
      Push-Location $repoRoot
      git mv $oldEditorConfig $newEditorConfig 2>&1 | Out-Null
      Pop-Location
   }

   Write-Host "  .editorconfig moved." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 11: Update IncludeTestConfigurationFiles.props
   # ═══════════════════════════════════════════════════════════════════════════

   # This path uses $(MSBuildThisFileDirectory)..\ which points to src/ — that's still correct
   # since TestUsingPluggableComponentCombinations stays in src/. No change needed.

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 12: Clean up empty directories left behind
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Cleaning up empty directories..." -ForegroundColor Cyan

   # Directories to clean (may be empty after moves)
   $dirsToClean = @(
      (Join-Path $srcDir "Compze" "Utilities" "Testing")
      (Join-Path $srcDir "Compze" "Utilities" "DependencyInjection")
      (Join-Path $srcDir "Compze" "Utilities" "Logging")
      (Join-Path $srcDir "Compze" "Utilities" "Tests")
      (Join-Path $srcDir "Compze" "Utilities")
      (Join-Path $srcDir "Compze" "Tessaging" "Hosting")
      (Join-Path $srcDir "Compze" "Tessaging" "Teventive")
      (Join-Path $srcDir "Compze" "Tessaging")
      (Join-Path $srcDir "Compze" "Serialization")
      (Join-Path $srcDir "Compze" "Sql")
      (Join-Path $srcDir "Compze" "Abstractions")
      (Join-Path $srcDir "Compze")
      (Join-Path $srcDir "Tests" "Unit" "Internals")
      (Join-Path $srcDir "Tests" "Performance" "Internals")
      (Join-Path $srcDir "Tests" "Performance")
      (Join-Path $srcDir "Tests" "Unit")
      (Join-Path $srcDir "Tests" "Integration")
      (Join-Path $srcDir "Tests" "Common")
      (Join-Path $srcDir "Tests" "Infrastructure")
      (Join-Path $srcDir "Tests" "ScratchPad")
      (Join-Path $srcDir "Tests" "Compze.Tests.CodePolicies")
      (Join-Path $srcDir "Tests")
   )

   foreach ($dir in $dirsToClean) {
      Remove-EmptyDirTree -Path $dir
   }

   Write-Host "  Empty directories cleaned." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # STEP 13: Update solution file references to non-project files
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host "Updating solution file references..." -ForegroundColor Cyan

   [xml]$slnx = Get-Content $solutionPath
   
   # Fix File paths in _Solution Items that reference moved config files
   $fileElements = $slnx.SelectNodes("//File[@Path]")
   foreach ($elem in $fileElements) {
      $path = $elem.GetAttribute("Path")
      if ($path -eq "Compze/test-common-appsettings.json") {
         $elem.SetAttribute("Path", "../test/test-common-appsettings.json")
      }
   }

   $slnx.Save($solutionPath)
   Write-Host "  Solution file references updated." -ForegroundColor Green

   # ═══════════════════════════════════════════════════════════════════════════
   # DONE
   # ═══════════════════════════════════════════════════════════════════════════

   Write-Host ""
   Write-Host "Migration complete!" -ForegroundColor Green
   Write-Host ""
   Write-Host "Next steps:" -ForegroundColor Yellow
   Write-Host "  1. Run: dotnet build src/Compze.slnx" -ForegroundColor Yellow
   Write-Host "  2. If build succeeds, run: C-Test" -ForegroundColor Yellow
   Write-Host "  3. Review the git diff" -ForegroundColor Yellow
   Write-Host ""
   Write-Host "Manual follow-ups (if needed):" -ForegroundColor Yellow
   Write-Host "  - Update Website.csproj _docs globs if Website build fails" -ForegroundColor Yellow
   Write-Host "  - Update DevScripts C-Create-Project, C-Relocate-Project conventions" -ForegroundColor Yellow
   Write-Host "  - Update copilot-instructions.md and solution-file-structure.README.md" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════════════════════
# Helper functions
# ═══════════════════════════════════════════════════════════════════════════════

function Move-ProjectContents {
   <# 
   .SYNOPSIS
   Moves a project directory's contents using git mv for tracked files and Move-Item for untracked.
   #>
   param(
      [string]$SourceDir,
      [string]$TargetDir
   )

   if (-not (Test-Path $TargetDir)) {
      New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
   }

   # Get all items in source dir
   $items = Get-ChildItem -Path $SourceDir -Force
   foreach ($item in $items) {
      $targetPath = Join-Path $TargetDir $item.Name
      if ($item.PSIsContainer) {
         # For directories, use git mv
         git mv $item.FullName $targetPath 2>&1 | Out-Null
         if ($LASTEXITCODE -ne 0) {
            # Fallback: regular move
            Move-Item -Path $item.FullName -Destination $targetPath -Force
         }
      } else {
         git mv $item.FullName $targetPath 2>&1 | Out-Null
         if ($LASTEXITCODE -ne 0) {
            Move-Item -Path $item.FullName -Destination $targetPath -Force
         }
      }
   }

   # Remove empty source directory
   if (Test-Path $SourceDir) {
      $remaining = Get-ChildItem -Path $SourceDir -Force
      if ($remaining.Count -eq 0) {
         Remove-Item $SourceDir -Force
      }
   }
}

function Remove-EmptyDirTree {
   <#
   .SYNOPSIS
   Recursively removes a directory tree if it contains no files (only empty subdirs).
   #>
   param([string]$Path)

   if (-not (Test-Path $Path)) { return }

   # Check for any files (recursive)
   $files = Get-ChildItem -Path $Path -Recurse -File -Force -ErrorAction SilentlyContinue
   if ($files.Count -eq 0) {
      Remove-Item $Path -Recurse -Force -ErrorAction SilentlyContinue
   }
}

function Get-SolutionFolder {
   <#
   .SYNOPSIS
   Determines the solution folder for a project based on its name and new path.
   #>
   param(
      [string]$ProjectName,
      [string]$ProjectPath
   )

   # Test projects go to /_Tests/
   if ($ProjectPath -match '^\.\.[\\/]test[\\/]') {
      return "/_Tests/"
   }

   # Sample projects stay in their folders
   if ($ProjectPath -match 'Samples') {
      if ($ProjectPath -match 'Tests|UnitTests|PerformanceTests') {
         return "/_Samples/AccountManagement/Tests/"
      }
      return "/_Samples/AccountManagement/"
   }

   # Website
   if ($ProjectPath -match 'Websites') {
      return "/_Websites/"
   }

   # Solution structure projects
   if ($ProjectPath -match 'msbuild|SolutionStructure') {
      return "/~Solution Structure/"
   }

   # DevScripts
   if ($ProjectPath -match 'DevScripts') {
      # DevScripts project is at solution root, no folder
      return $null
   }

   # Library projects: group by first component after "Compze."
   # Compze.Core → /Compze/
   # Compze.Utilities.* → /Compze/Utilities/
   # Compze.Tessaging.* → /Compze/Tessaging/
   # Compze.Sql.* → /Compze/Sql/
   # Compze.Serialization.* → /Compze/Serialization/
   $parts = $ProjectName -split '\.'
   if ($parts.Count -le 2) {
      return "/Compze/"
   }

   # Use first two segments as folder: /Compze/Utilities/, /Compze/Tessaging/, etc.
   return "/Compze/$($parts[1])/"
}
