# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Open-XmlDocumentPreservingWhitespace {
    <#
    .SYNOPSIS
    Loads an XML document with whitespace preservation
    
    .DESCRIPTION
    Creates and loads an XmlDocument with PreserveWhitespace set to true,
    ensuring that the original formatting is maintained when the document is saved.
    
    .PARAMETER Path
    Path to the XML file to load
    
    .RETURNS
    System.Xml.XmlDocument with PreserveWhitespace enabled
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )
    
    if (-not (Test-Path $Path)) {
        Write-Error "File not found: $Path"
        return $null
    }
    
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($Path)
    
    return $xml
}
