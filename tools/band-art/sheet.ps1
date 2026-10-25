param([string]$Dir, [string]$Out)
# Gera a prancha com os 4 músicos e os nomes dos estilos.
Add-Type -AssemblyName System.Drawing
$sheet = New-Object System.Drawing.Bitmap 1560, 600
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(58, 54, 82))
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$title = New-Object System.Drawing.Font "Segoe UI Semibold", 22
$sub = New-Object System.Drawing.Font "Segoe UI", 15
$subBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(190, 186, 215))
$names = @(@("Punk rock", "guitarra"), @("Jazz", "voz"), @("Reggae", "baixo"), @("Hip-hop", "bateria"))
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$i = 0
foreach ($f in Get-ChildItem $Dir -Filter *.png | Sort-Object Name) {
    $img = [System.Drawing.Image]::FromFile($f.FullName)
    $g.DrawImage($img, 5 + $i * 390, 4)
    $img.Dispose()
    $g.DrawString($names[$i][0], $title, [System.Drawing.Brushes]::White, (197 + $i * 390), 522, $fmt)
    $g.DrawString($names[$i][1], $sub, $subBrush, (197 + $i * 390), 560, $fmt)
    $i++
}
$sheet.Save($Out)
