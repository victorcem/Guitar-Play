param([string]$Folder, [switch]$Recurse)
# Renderiza cada SVG em PNG com fundo transparente usando o Edge sem janela (tamanho lido do SVG).
$edge = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
$profile = Join-Path $env:TEMP ("edge-svg-render-" + [guid]::NewGuid().ToString("N").Substring(0, 8))
Get-ChildItem $Folder -Filter *.svg -Recurse:$Recurse | ForEach-Object {
    $text = [IO.File]::ReadAllText($_.FullName)
    $head = $text.Substring(0, [Math]::Min(400, $text.Length))
    $w = [int]([regex]::Match($head, 'width="(\d+)"').Groups[1].Value)
    $h = [int]([regex]::Match($head, 'height="(\d+)"').Groups[1].Value)
    $png = [IO.Path]::ChangeExtension($_.FullName, ".png")
    $url = "file:///" + ($_.FullName -replace '\\', '/')
    & $edge --headless=new --disable-gpu --hide-scrollbars --user-data-dir="$profile" `
        --default-background-color=00000000 --window-size="$w,$h" --screenshot="$png" $url 2>$null | Out-Null
}
