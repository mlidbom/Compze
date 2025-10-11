function Validate-SolutionStructure {
    <#
    .SYNOPSIS
    Validates the Compze solution structure
    
    .DESCRIPTION
    Runs the Validate-SolutionStructure.ps1 script from any directory
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    & "$PSScriptRoot\..\Validate-SolutionStructure.ps1" @args
}
