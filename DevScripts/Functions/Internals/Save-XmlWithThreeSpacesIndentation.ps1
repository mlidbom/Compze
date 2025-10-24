# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Save-XmlWithThreeSpacesIndentation {
    <#
    .SYNOPSIS
    Saves an XML document with three-space indentation
    
    .DESCRIPTION
    Saves an XmlDocument to a file and then converts all two-space indentation
    to three-space indentation to match the project's formatting standards.
    
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
    
    # Save the XML document with explicit writer disposal
    $writer = [System.IO.StreamWriter]::new($Path, $false, [System.Text.Encoding]::UTF8)
    try {
        $Xml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
    
    # Read the content
    $content = Get-Content $Path -Raw
    
    # Replace two-space indentation with three-space indentation
    # This regex matches lines that start with whitespace (two spaces repeated)
    $lines = $content -split "`r`n"
    $convertedLines = foreach ($line in $lines) {
        if ($line -match '^( {2})+') {
            # Count how many two-space indents there are
            $leadingSpaces = $matches[0]
            $indentLevel = $leadingSpaces.Length / 2
            $newIndent = '   ' * $indentLevel  # Three spaces per indent level
            $line -replace '^( {2})+', $newIndent
        } else {
            $line
        }
    }
    
    # Join back and save
    $newContent = $convertedLines -join "`r`n"
    Set-Content -Path $Path -Value $newContent -NoNewline -Encoding UTF8
}
