$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $PSScriptRoot
$layout=Get-Content -LiteralPath (Join-Path $root 'art/station-layout.json') -Raw | ConvertFrom-Json
$folder=Join-Path $root 'art/source/baiye_compact_transit'
[xml]$scml=Get-Content -LiteralPath (Join-Path $folder 'baiye_compact_transit.scml') -Raw
$canvas=[System.Drawing.Bitmap]::new(1440,460)
$g=[System.Drawing.Graphics]::FromImage($canvas)
$g.Clear([System.Drawing.Color]::FromArgb(30,37,42))
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font=[System.Drawing.Font]::new('Microsoft YaHei',12)
$label=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230,235,237))
$cases=@(@('off',0,'充电 0%',0),@('on',0,'就绪 100%',50),@('working_pre_up',5,'上行启动',35),@('working_pre_down',5,'下行启动',35),@('working_loop',5,'发射中',25),@('working_pst',7,'启动复位',25))
for($c=0;$c -lt $cases.Count;$c++) {
    $test=$cases[$c]; $originX=120+$c*240; $originY=408
    $animation=@($scml.spriter_data.entity.animation | Where-Object {$_.name -eq $test[0]})[0]
    $main=@($animation.mainline.key)[$test[1]]
    foreach($reference in @($main.object_ref)) {
        $timeline=@($animation.timeline) | Where-Object {$_.id -eq $reference.timeline}
        $obj=@($timeline.key)[$reference.key].object
        $file=@($scml.spriter_data.folder.file)[$obj.file]
        if($file.name -like '*target*'){continue}
        $im=[System.Drawing.Image]::FromFile((Join-Path $folder $file.name))
        $sx=if($obj.scale_x){[double]$obj.scale_x}else{1};$sy=if($obj.scale_y){[double]$obj.scale_y}else{1}
        if($sy -lt 0){$im.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipY)}
        $w=$im.Width*[math]::Abs($sx)*1.6; $h=$im.Height*[math]::Abs($sy)*1.6
        $alpha=if($obj.a){[single]$obj.a}else{1}
        $matrix=[System.Drawing.Imaging.ColorMatrix]::new();$matrix.Matrix33=$alpha
        $attributes=[System.Drawing.Imaging.ImageAttributes]::new();$attributes.SetColorMatrix($matrix)
        $x=$originX+[double]$obj.x*1.6-$w*.5; $y=$originY-[double]$obj.y*1.6-$h*(1-[double]$file.pivot_y)
        $g.DrawImage($im,[System.Drawing.Rectangle]::new([int]$x,[int]$y,[int]$w,[int]$h),0,0,$im.Width,$im.Height,[System.Drawing.GraphicsUnit]::Pixel,$attributes)
        $im.Dispose();$attributes.Dispose()
    }
    $meter=[System.Drawing.Image]::FromFile((Join-Path $folder "meter_$($test[3]).png"))
    $meterW=$layout.meter.width*.390625*1.6; $meterH=$layout.meter.height*.390625*1.6
    $g.DrawImage($meter,[single]($originX+$layout.meter.x*1.6-$meterW*.5),[single]($originY-$layout.meter.y*1.6-$meterH),[single]$meterW,[single]$meterH); $meter.Dispose()
    $g.DrawString($test[2],$font,$label,[single]($originX-65),[single]20)
}
$g.DrawString('SCML 源动画预览 · 电量为演示值 · 非游戏实测',$font,$label,20,434)
$g.Dispose();$font.Dispose();$label.Dispose()
$canvas.Save((Join-Path $root 'dist/verification/station-animation-preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$canvas.Dispose()
