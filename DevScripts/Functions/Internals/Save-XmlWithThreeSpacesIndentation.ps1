# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Save-XmlWithThreeSpacesIndentation {
    <#
    .SYNOPSIS
    Saves an XML document with three-space indentation
    
    .DESCRIPTION
    Saves an XmlDocument to a file with three-space indentation to match the project's formatting standards.
    Uses XmlWriterSettings to configure the indentation directly, avoiding the need for post-processing
    and preventing file locking issues with Visual Studio.
    
    .PARAMETER Xml
    The XmlDocument to save
    
    .PARAMETER Path
    Path to the file where the XML should be saved
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlDocument]$Xml,
        
        [Parameter(Mandatory = $true)]
        [string]$Path
    )
    
    # Configure XML writer settings for three-space indentation
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "   "  # Three spaces
    $settings.Encoding = [System.Text.Encoding]::UTF8
    $settings.OmitXmlDeclaration = $true  # .csproj files don't have XML declarations
    
    # Create the XML writer and save
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $Xml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}
