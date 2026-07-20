# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Create-Project {
    <#
    .SYNOPSIS
    Creates a new project with the proper directory structure and adds it to the solution
    
    .DESCRIPTION
    Creates a new C# project following the Compze flat-layout conventions:
    - Library projects: src/<ProjectName>/<ProjectName>.csproj
    - Test projects: test/<ProjectName>/<ProjectName>.csproj
    - Creates a basic .csproj file
    - Adds the project to the .slnx solution file in the appropriate folder structure
    
    Test projects are detected by name: contains ".Tests." or ends with ".Tests", "Specifications" (which covers ".Specifications" and ".InternalSpecifications")
    
    .PARAMETER ProjectName
    The full name of the project to create (e.g., "Compze.Wiring" or "Compze.Tests.MyFeature")
    
    .EXAMPLE
    C-Create-Project -ProjectName Compze.Wiring
    Creates a new project at src/Compze.Wiring/Compze.Wiring.csproj
    
    .EXAMPLE
    C-Create-Project -ProjectName Compze.Tests.MyFeature
    Creates a new project at test/Compze.Tests.MyFeature/Compze.Tests.MyFeature.csproj
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )
    
    $SolutionPath = $script:CompzeSolutionPath
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }
    
    # Step 1: Determine if test or library project, and calculate directory path
    $isTest = ($ProjectName -match '\.Tests\.' -or $ProjectName -match '\.Tests$' -or $ProjectName -match 'Specifications$')
    
    if ($isTest) {
        $projectDir = Join-Path $script:CompzeRoot "test" $ProjectName
    } else {
        $projectDir = Join-Path $script:CompzeSrcRoot $ProjectName
    }
    
    # Check if directory already exists
    if (Test-Path $projectDir) {
        $existingCsproj = Get-ChildItem -Path $projectDir -Filter "*.csproj" | Select-Object -First 1
        if ($existingCsproj) {
            Write-Error "Project directory already exists with a .csproj file: $projectDir"
            return
        }
    }
    
    # Step 2: Create the directory
    if (-not (Test-Path $projectDir)) {
        New-Item -ItemType Directory -Path $projectDir -Force | Out-Null
    }
    
    # Step 3: Create the .csproj file
    $csprojPath = Join-Path $projectDir "$ProjectName.csproj"
    if (Test-Path $csprojPath) {
        Write-Error "Project file already exists: $csprojPath"
        return
    }
    
    $csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<Nullable>enable</Nullable>
	</PropertyGroup>

</Project>
"@
    
    Set-Content -Path $csprojPath -Value $csprojContent -NoNewline -Encoding UTF8
    
    # Step 4: Add project to solution (at solution root — arrange in folders manually)
    Ensure-ProjectIsInSolution -ProjectName $ProjectName -SolutionPath $SolutionPath
}
