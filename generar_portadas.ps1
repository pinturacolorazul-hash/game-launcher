Add-Type -AssemblyName System.Drawing

$covers = "D:\documentos\juegosprueba\GameLauncher\covers"

$games = @{
    "Minecraft.png" = @("#4C9A2A", "#2D6B12")
    "Cyberpunk2077.png" = @("#FFD700", "#1A1A2E")
    "Witcher3.png" = @("#8B0000", "#1A1A1A")
    "StardewValley.png" = @("#5C8A3C", "#3D6B2E")
    "Portal2.png" = @("#FF6600", "#1A1A2E")
}

foreach ($g in $games.Keys) {
    $bm = New-Object System.Drawing.Bitmap 200, 280
    $gr = [System.Drawing.Graphics]::FromImage($bm)
    $gr.SmoothingMode = 'HighQuality'

    $rect = New-Object System.Drawing.Rectangle 0, 0, 200, 280
    $c1 = [System.Drawing.Color]::FromArgb(220, [System.Drawing.ColorTranslator]::FromHtml($games[$g][0]))
    $c2 = [System.Drawing.Color]::FromArgb(220, [System.Drawing.ColorTranslator]::FromHtml($games[$g][1]))
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $c1, $c2, 45
    $gr.FillRectangle($brush, $rect)
    $brush.Dispose()

    $name = [System.IO.Path]::GetFileNameWithoutExtension($g)
    $initial = $name[0].ToString().ToUpper()
    $font = New-Object System.Drawing.Font("Segoe UI", 72, [System.Drawing.FontStyle]::Bold)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = 'Center'
    $sf.LineAlignment = 'Center'

    $pt = New-Object System.Drawing.PointF 100, 140
    $gr.DrawString($initial, $font, [System.Drawing.Brushes]::WhiteSmoke, $pt, $sf)

    $font.Dispose()
    $sf.Dispose()
    $gr.Dispose()
    $bm.Save([System.IO.Path]::Combine($covers, $g), [System.Drawing.Imaging.ImageFormat]::Png)
    $bm.Dispose()
    Write-Host "Creado: $g"
}
