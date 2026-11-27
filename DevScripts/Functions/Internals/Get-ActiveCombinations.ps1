# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Get-ActiveCombinations {
    <#
    .SYNOPSIS
    The combinations of pluggable components that src/TestUsingPluggableComponentCombinations turns on: its lines that are neither empty nor commented out with #

    .DESCRIPTION
    Each is a line such as 'MySql:Microsoft:Newtonsoft:AspNetCore': persistence layer, DI container, serializer and transport.
    Nothing when the file isn't there, as before the first build creates it from its .defaults.

    .EXAMPLE
    Get-ActiveCombinations
    #>
    [CmdletBinding()]
    param()

    $path = Join-Path $script:CompzeSrcRoot 'TestUsingPluggableComponentCombinations'
    if (-not (Test-Path -LiteralPath $path)) { return }
    Get-Content -LiteralPath $path | ForEach-Object { $_.Trim() } | Where-Object { $_ -and -not $_.StartsWith('#') }
}
