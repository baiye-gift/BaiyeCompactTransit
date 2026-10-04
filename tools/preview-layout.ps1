$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$canvas = [System.Drawing.Bitmap]::new(1040,660)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(242,241,235))
$ink = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(42,49,53))
$tile = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(181,187,185))
$station = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(244,221,142))
$bridge = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(202,224,225))
$edge = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(42,49,53),2)
$pipe = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(56,136,148),12)
$walk = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(174,107,47),4)
$font = [System.Drawing.Font]::new('Microsoft YaHei',13)
$small = [System.Drawing.Font]::new('Microsoft YaHei',11)
$title = [System.Drawing.Font]::new('Microsoft YaHei',20,[System.Drawing.FontStyle]::Bold)
function Text-At([string]$text,[single]$x,[single]$y,$f=$font) { $g.DrawString($text,$f,$ink,$x,$y) }
function Walk-Arrow([int]$a,[int]$b,[int]$y) {
    $g.DrawLine($walk,$a,$y,$b,$y)
    $g.DrawLine($walk,$a,$y,($a+10),($y-8)); $g.DrawLine($walk,$a,$y,($a+10),($y+8))
    $g.DrawLine($walk,$b,$y,($b-10),($y-8)); $g.DrawLine($walk,$b,$y,($b-10),($y+8))
}
function Person([int]$x) {
    $g.DrawEllipse($edge,($x-13),265,26,26)
    $g.DrawLine($edge,$x,292,$x,346)
    $g.DrawLine($edge,$x,303,($x-24),327); $g.DrawLine($edge,$x,303,($x+24),327)
    $g.DrawLine($edge,$x,346,($x-18),390); $g.DrawLine($edge,$x,346,($x+18),390)
}
Text-At '一条直管道，每层一个悬空站台' 42 22 $title
Text-At '连接逻辑示意 · 站体 1×2，自带一格宽薄平台 · 非游戏截图' 44 65 $small
$g.DrawLine($pipe,525,112,525,190)
Text-At '上方运载管道' 592 128
$g.FillRectangle($station,475,190,100,200); $g.DrawRectangle($edge,475,190,100,200)
$g.DrawLine($edge,475,290,575,290)
Text-At '站体上格' 484 217 $small
Text-At "站体下格`n上车/下车" 484 303 $small
Person 310; Person 740
Walk-Arrow 365 467 339; Walk-Arrow 583 685 339
Text-At '左侧步行通道' 245 196
Text-At '右侧步行通道' 674 196
for($x=75;$x -lt 975;$x+=100) {
    if($x -eq 475){continue}
    $g.FillRectangle($tile,$x,390,100,100); $g.DrawRectangle($edge,$x,390,100,100)
}
$g.DrawLine($pipe,525,390,525,551)
$g.FillRectangle($station,475,383,100,7); $g.DrawRectangle($edge,475,383,100,7)
Text-At '自带平台' 590 393 $small
Text-At '地板' 245 425
Text-At '下方运载管道' 592 505
Text-At '站台下方直接接管，不用另铺地板、渡口或绕弯。' 42 574
Text-At '平台自带站立支撑；至少一侧连接可达的两格高通道。两侧无出口时只能经过。' 42 610 $small
$g.Dispose()
foreach($item in @($ink,$tile,$station,$bridge,$edge,$pipe,$walk,$font,$small,$title)) { $item.Dispose() }
$canvas.Save((Join-Path $root 'docs/connection-layout.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
