function Fix-CsprojExclusions {
    <#
    .SYNOPSIS
    Fixes .csproj exclusions for Compze projects
    
    .DESCRIPTION
    Runs the Fix-CsprojExclusions.ps1 script from any directory
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    & "$PSScriptRoot\..\Fix-CsprojExclusions.ps1" @args
}
