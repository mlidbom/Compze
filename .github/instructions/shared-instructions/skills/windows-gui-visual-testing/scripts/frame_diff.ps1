# Compare two captured frames to detect/localize change — the test for "is this actually live, or frozen?".
# Prints the count of changed samples and their bounding box; optionally saves a crop of the changed region.
# Capture two frames a second or two apart, then diff. A localized cluster of change = that area animates;
# near-zero = static. Look at WHERE it changed (bbox), not just the count (a clock/caret will register too).
#
#   pwsh scripts/frame_diff.ps1 -A frame1.png -B frame2.png
#   pwsh scripts/frame_diff.ps1 -A frame1.png -B frame2.png -CropOut change.png   # crop bbox from B
param(
    [Parameter(Mandatory)][string]$A,
    [Parameter(Mandatory)][string]$B,
    [int]$Step = 3,            # sample every Nth pixel (speed vs precision)
    [int]$Threshold = 40,      # per-pixel sum-of-RGB-delta to count as "changed"
    [string]$CropOut
)
Add-Type -AssemblyName System.Drawing
# NOTE: locals must NOT be named $a/$b — PowerShell variable names are case-insensitive, so they would alias the
# [string]-typed params $A/$B and coerce the Bitmap to the text "System.Drawing.Bitmap".
$imgA = [System.Drawing.Bitmap]::FromFile($A)
$imgB = [System.Drawing.Bitmap]::FromFile($B)
if ($imgA.Width -ne $imgB.Width -or $imgA.Height -ne $imgB.Height) { Write-Error "frames differ in size"; exit 1 }
$cnt = 0; $minX = 1e9; $minY = 1e9; $maxX = -1; $maxY = -1
for ($y = 0; $y -lt $imgA.Height; $y += $Step) {
    for ($x = 0; $x -lt $imgA.Width; $x += $Step) {
        $pa = $imgA.GetPixel($x, $y); $pb = $imgB.GetPixel($x, $y)
        $d = [math]::Abs($pa.R - $pb.R) + [math]::Abs($pa.G - $pb.G) + [math]::Abs($pa.B - $pb.B)
        if ($d -gt $Threshold) {
            $cnt++
            if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
"dims: $($imgA.Width)x$($imgA.Height)  changed samples: $cnt  bbox: ($minX,$minY)-($maxX,$maxY)"
if ($CropOut -and $maxX -gt 0) {
    $bw = [int]($maxX - $minX); $bh = [int]($maxY - $minY)
    if ($bw -lt 1) { $bw = 1 }; if ($bh -lt 1) { $bh = 1 }
    $rect = New-Object System.Drawing.Rectangle($minX, $minY, $bw, $bh)
    $imgB.Clone($rect, $imgB.PixelFormat).Save($CropOut)
    "saved crop: $CropOut (${bw}x${bh})"
}
$imgA.Dispose(); $imgB.Dispose()
