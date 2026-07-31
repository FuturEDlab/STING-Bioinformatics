param(
    [Parameter(Mandatory = $true)][string]$In,
    [Parameter(Mandatory = $true)][string]$Out,
    [string]$Title = ''
)

$lines = [System.IO.File]::ReadAllLines($In)
if (-not $Title) { $Title = [System.IO.Path]::GetFileNameWithoutExtension($In) }

$codeStore = New-Object System.Collections.ArrayList

function Esc([string]$s) {
    $s = $s -replace '&', '&amp;'
    $s = $s -replace '<', '&lt;'
    $s = $s -replace '>', '&gt;'
    return $s
}

function Inline([string]$s) {
    # Pull code spans out first so ** and * inside them are left alone.
    $script:codeStore.Clear() | Out-Null
    $s = [regex]::Replace($s, '`([^`]+)`', {
            param($m)
            $i = $script:codeStore.Add((Esc $m.Groups[1].Value))
            "!!CODE$i!!"
        })
    $s = Esc $s
    $s = [regex]::Replace($s, '\*\*(.+?)\*\*', '<strong>$1</strong>')
    $s = [regex]::Replace($s, '(?<![\*\w])\*([^\*]+?)\*(?!\*)', '<em>$1</em>')
    $s = [regex]::Replace($s, '\[([^\]]+)\]\(([^)]+)\)', '<a href="$2">$1</a>')
    for ($i = 0; $i -lt $script:codeStore.Count; $i++) {
        $s = $s.Replace("!!CODE$i!!", "<code>$($script:codeStore[$i])</code>")
    }
    return $s
}

$sb = New-Object System.Text.StringBuilder
$i = 0
$inList = $false; $listTag = ''

function CloseList() {
    if ($script:inList) { [void]$script:sb.AppendLine("</$script:listTag>"); $script:inList = $false }
}

while ($i -lt $lines.Length) {
    $ln = $lines[$i]

    # fenced code
    if ($ln -match '^\s*```') {
        CloseList
        $i++
        [void]$sb.AppendLine('<pre><code>')
        while ($i -lt $lines.Length -and $lines[$i] -notmatch '^\s*```') {
            [void]$sb.AppendLine((Esc $lines[$i]))
            $i++
        }
        $i++
        [void]$sb.AppendLine('</code></pre>')
        continue
    }

    # table
    if ($ln -match '^\s*\|' -and ($i + 1) -lt $lines.Length -and $lines[$i + 1] -match '^\s*\|[\s:\-\|]+\|\s*$') {
        CloseList
        $cells = ($ln.Trim().Trim('|') -split '\|') | ForEach-Object { Inline $_.Trim() }
        [void]$sb.AppendLine('<table><thead><tr>')
        foreach ($c in $cells) { [void]$sb.AppendLine("<th>$c</th>") }
        [void]$sb.AppendLine('</tr></thead><tbody>')
        $i += 2
        while ($i -lt $lines.Length -and $lines[$i] -match '^\s*\|') {
            $row = ($lines[$i].Trim().Trim('|') -split '\|') | ForEach-Object { Inline $_.Trim() }
            [void]$sb.AppendLine('<tr>')
            foreach ($c in $row) { [void]$sb.AppendLine("<td>$c</td>") }
            [void]$sb.AppendLine('</tr>')
            $i++
        }
        [void]$sb.AppendLine('</tbody></table>')
        continue
    }

    # horizontal rule
    if ($ln -match '^\s*(---+|\*\*\*+)\s*$') { CloseList; [void]$sb.AppendLine('<hr/>'); $i++; continue }

    # heading
    if ($ln -match '^(#{1,6})\s+(.*)$') {
        CloseList
        $lvl = $Matches[1].Length
        [void]$sb.AppendLine("<h$lvl>$(Inline $Matches[2])</h$lvl>")
        $i++; continue
    }

    # blockquote
    if ($ln -match '^\s*>\s?(.*)$') {
        CloseList
        $buf = @()
        while ($i -lt $lines.Length -and $lines[$i] -match '^\s*>\s?(.*)$') { $buf += $Matches[1]; $i++ }
        [void]$sb.AppendLine("<blockquote><p>$(Inline ($buf -join ' '))</p></blockquote>")
        continue
    }

    # ordered list
    if ($ln -match '^\s*\d+\.\s+(.*)$') {
        if (-not $inList -or $listTag -ne 'ol') { CloseList; [void]$sb.AppendLine('<ol>'); $inList = $true; $listTag = 'ol' }
        [void]$sb.AppendLine("<li>$(Inline $Matches[1])</li>")
        $i++; continue
    }

    # unordered list
    if ($ln -match '^\s*[-\*]\s+(.*)$') {
        if (-not $inList -or $listTag -ne 'ul') { CloseList; [void]$sb.AppendLine('<ul>'); $inList = $true; $listTag = 'ul' }
        [void]$sb.AppendLine("<li>$(Inline $Matches[1])</li>")
        $i++; continue
    }

    # blank
    if ($ln -match '^\s*$') { CloseList; $i++; continue }

    # paragraph
    CloseList
    $buf = @()
    while ($i -lt $lines.Length -and $lines[$i] -notmatch '^\s*$' -and $lines[$i] -notmatch '^(#{1,6})\s' -and $lines[$i] -notmatch '^\s*\|' -and $lines[$i] -notmatch '^\s*```' -and $lines[$i] -notmatch '^\s*>' -and $lines[$i] -notmatch '^\s*[-\*]\s' -and $lines[$i] -notmatch '^\s*\d+\.\s' -and $lines[$i] -notmatch '^\s*(---+)\s*$') {
        $buf += $lines[$i]; $i++
    }
    if ($buf.Count) { [void]$sb.AppendLine("<p>$(Inline ($buf -join ' '))</p>") }
}
CloseList

$css = @'
@page { size: A4; margin: 16mm 14mm; }
* { box-sizing: border-box; }
body { font-family: "Segoe UI", Calibri, Arial, sans-serif; font-size: 10.2pt; line-height: 1.5;
       color: #1b1f24; margin: 0; }
h1 { font-size: 20pt; margin: 0 0 4pt; color: #0f2b46; border-bottom: 2.5pt solid #2c6fa8; padding-bottom: 5pt; }
h2 { font-size: 14pt; margin: 20pt 0 6pt; color: #14456e; border-bottom: 0.6pt solid #c8d4de; padding-bottom: 3pt;
     page-break-after: avoid; }
h3 { font-size: 11.4pt; margin: 13pt 0 4pt; color: #2c6fa8; page-break-after: avoid; }
p { margin: 5pt 0; }
ul, ol { margin: 5pt 0 5pt 16pt; padding: 0; }
li { margin: 2.5pt 0; }
code { font-family: Consolas, "Courier New", monospace; font-size: 8.9pt;
       background: #eef2f6; border: 0.4pt solid #d5dee6; border-radius: 2pt; padding: 0.5pt 3pt; color: #123; }
pre { background: #f6f8fa; border: 0.5pt solid #d5dee6; border-left: 2.5pt solid #2c6fa8;
      border-radius: 3pt; padding: 7pt 9pt; margin: 7pt 0; page-break-inside: avoid; }
pre code { background: none; border: none; padding: 0; font-size: 9pt; line-height: 1.45; }
table { border-collapse: collapse; width: 100%; margin: 7pt 0; font-size: 9.3pt; page-break-inside: auto; }
thead { display: table-header-group; }
tr { page-break-inside: avoid; }
th { background: #14456e; color: #fff; text-align: left; padding: 4.5pt 6pt; font-weight: 600; }
td { border: 0.4pt solid #cfd9e2; padding: 4.5pt 6pt; vertical-align: top; }
tbody tr:nth-child(even) td { background: #f5f8fa; }
td:first-child { white-space: nowrap; }
blockquote { margin: 7pt 0; padding: 6pt 10pt; background: #fff8e6;
             border-left: 2.5pt solid #d9a520; page-break-inside: avoid; }
blockquote p { margin: 0; }
hr { border: none; border-top: 0.5pt solid #d5dee6; margin: 13pt 0; }
strong { color: #0f2b46; }
a { color: #2c6fa8; text-decoration: none; }
'@

$html = @"
<!DOCTYPE html>
<html><head><meta charset="utf-8"/><title>$Title</title><style>
$css
</style></head><body>
$($sb.ToString())
</body></html>
"@

[System.IO.File]::WriteAllText($Out, $html, (New-Object System.Text.UTF8Encoding($false)))
Write-Output "wrote $Out"
