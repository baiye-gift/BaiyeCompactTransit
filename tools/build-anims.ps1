param([string]$Kanimal = 'C:\Users\54492\AppData\Local\Temp\kanimal-SE-1.3.31\kanimal-cli.exe', [string]$Python = 'python')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$name = 'baiye_compact_transit'
$layout = Get-Content -LiteralPath (Join-Path $root 'art/station-layout.json') -Raw | ConvertFrom-Json
$source = Join-Path $root "art/source/$name"
$dest = Join-Path $root "anim/assets/$name"
New-Item -ItemType Directory -Force $source,$dest,(Join-Path $root 'dist/verification') | Out-Null
Add-Type -AssemblyName System.Drawing
if(-not ('BaiyeSpritePacking' -as [type])) {
    Add-Type -ReferencedAssemblies @('System.Drawing.Common','System.Drawing.Primitives','System.Runtime') -TypeDefinition @'
using System.Drawing;
public static class BaiyeSpritePacking {
    public static Rectangle Bounds(Bitmap b) {
        int left=b.Width, top=b.Height, right=0, bottom=0;
        for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) if(b.GetPixel(x,y).A>24) {
            if(x<left)left=x; if(y<top)top=y; if(x>right)right=x; if(y>bottom)bottom=y;
        }
        return Rectangle.FromLTRB(left,top,right+1,bottom+1);
    }
}
'@
}
function New-Sprite([int]$w,[int]$h) {
    return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}
function Save-Sprite($bitmap,[string]$file) { $bitmap.Save((Join-Path $source $file),[System.Drawing.Imaging.ImageFormat]::Png); $bitmap.Dispose() }

# Mechanical sprite import/packing only; the generated source remains untouched.
$original = [System.Drawing.Bitmap]::new((Join-Path $root ('art/source/' + $layout.source)))
$bounds = [BaiyeSpritePacking]::Bounds($original)
$body = New-Sprite 256 512
$g = [System.Drawing.Graphics]::FromImage($body)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($original,[System.Drawing.Rectangle]::new(0,0,256,512),$bounds,[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $original.Dispose()
$body.Save((Join-Path $source 'body_0.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$icon = New-Sprite 128 128; $g=[System.Drawing.Graphics]::FromImage($icon)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($body,28,6,72,116); $g.Dispose(); Save-Sprite $icon 'ui_0.png'; $body.Dispose()

# Only light parts are separate. Pipe walls, couplings and platform are one body.
foreach($part in @('pole')) {
    $settings=$layout.$part
    $im=New-Sprite $settings.width $settings.height; $g=[System.Drawing.Graphics]::FromImage($im)
    $light=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(220,86,226,211))
    $g.FillRectangle($light,0,0,$settings.width,$settings.height)
    $g.Dispose(); $light.Dispose(); Save-Sprite $im "$($part)_glow_0.png"
}
foreach($file in @('door_0.png','foot_glow_0.png')) {
    $obsolete=Join-Path $source $file
    if(Test-Path -LiteralPath $obsolete){Remove-Item -LiteralPath $obsolete}
}
$fx=New-Sprite $layout.field.width $layout.field.height; $g=[System.Drawing.Graphics]::FromImage($fx)
$g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$pen=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(200,86,240,226),2)
$g.DrawLine($pen,3,3,($layout.field.width/2),($layout.field.height-3))
$g.Dispose(); $pen.Dispose(); Save-Sprite $fx 'flux_0.png'
foreach($direction in @('up','down')) {
    $im=New-Sprite $layout.screen.width $layout.screen.height; $g=[System.Drawing.Graphics]::FromImage($im)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $pen=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(245,86,226,211),3)
    $tip=if($direction -eq 'up'){3}else{$layout.screen.height-3}
    $tail=if($direction -eq 'up'){$layout.screen.height-3}else{3}
    $g.DrawLine($pen,3,$tail,($layout.screen.width/2),$tip)
    $g.DrawLine($pen,($layout.screen.width/2),$tip,($layout.screen.width-3),$tail)
    $g.Dispose(); $pen.Dispose(); Save-Sprite $im "arrow_$($direction)_0.png"
}
foreach($target in @('meter_target_0.png','wax_meter_target_0.png')) {
    $im=New-Sprite 2 2; $im.SetPixel(0,0,[System.Drawing.Color]::FromArgb(1,255,255,255)); Save-Sprite $im $target
}
foreach($kind in @('meter','wax_meter')) {
    $settings=$layout.$kind; $w=$settings.width; $h=$settings.height
    for($i=0;$i -le 50;$i++) {
        $im=New-Sprite $w $h; $g=[System.Drawing.Graphics]::FromImage($im)
        $color=if($kind -eq 'meter'){[System.Drawing.Color]::FromArgb(245,86,226,211)}else{[System.Drawing.Color]::FromArgb(235,227,188,91)}
        $brush=[System.Drawing.SolidBrush]::new($color)
        if($settings.axis -eq 'horizontal') {
            $filled=[int][math]::Round($w*$i/50.0)
            if($filled -gt 0){$g.FillRectangle($brush,0,0,$filled,$h)}
        } else {
            $filled=[int][math]::Round($h*$i/50.0)
            if($filled -gt 0){$g.FillRectangle($brush,0,($h-$filled),$w,$filled)}
        }
        $brush.Dispose(); $g.Dispose(); Save-Sprite $im "$($kind)_$i.png"
    }
}
& $Python (Join-Path $PSScriptRoot 'write-scml.py')
if($LASTEXITCODE -ne 0){throw 'SCML generation failed'}
$env:DOTNET_ROLL_FORWARD='Major'
& $Kanimal kanim (Join-Path $source "$name.scml") -o $dest
if($LASTEXITCODE -ne 0){throw 'KAnim conversion failed'}
& (Join-Path $PSScriptRoot 'preview-anims.ps1')
& (Join-Path $PSScriptRoot 'preview-layout.ps1')
Write-Output "Built $dest"
