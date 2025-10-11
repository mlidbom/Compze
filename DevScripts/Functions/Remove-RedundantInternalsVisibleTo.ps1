function Remove-RedundantInternalsVisibleTo {
    <#
    .SYNOPSIS
    Removes redundant InternalsVisibleTo attributes
    
    .DESCRIPTION
    Runs the Remove-RedundantInternalsVisibleTo.ps1 script from any directory
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    & "$PSScriptRoot\..\Remove-RedundantInternalsVisibleTo.ps1" @args
}
