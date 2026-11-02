param([switch]$Staged)
$ErrorActionPreference = 'Stop'
$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0) { throw 'Ejecuta esta comprobacion dentro del repositorio.' }
Push-Location -LiteralPath $repoRoot
try {
    $problems = [System.Collections.Generic.List[string]]::new()
    $warnings = [System.Collections.Generic.List[string]]::new()
    function Read-RepoText([string]$rel) {
        if ($Staged) {
            $value = & git show ":$rel" 2>$null
            if ($LASTEXITCODE -ne 0) { return $null }
            return ($value -join "`n")
        }
        if (Test-Path -LiteralPath $rel -PathType Leaf) { return [IO.File]::ReadAllText((Join-Path $repoRoot $rel)) }
        return $null
    }
    if ($Staged) {
        $checkFiles = @(& git -c core.quotepath=false diff --cached --name-only --diff-filter=ACMR)
        $assetFiles = @(& git -c core.quotepath=false ls-files --cached -- Assets)
    } else {
        $assetFiles = @(Get-ChildItem -LiteralPath Assets -Recurse -File | ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1).Replace('\','/') })
        $checkFiles = @(& git -c core.quotepath=false ls-files --cached --others --exclude-standard)
    }
    foreach ($rel in $checkFiles) {
        if ($rel -match '^(Library|Temp|Obj|Logs|UserSettings|Build|Builds|\.vs|\.idea)/') { $problems.Add("Archivo generado incluido: $rel") }
        if ($Staged) {
            $bytes = [long](& git cat-file -s ":$rel")
        } elseif (Test-Path -LiteralPath $rel -PathType Leaf) { $bytes = (Get-Item -LiteralPath $rel).Length } else { continue }
        if ($bytes -ge 100MB) { $problems.Add("Archivo >=100 MiB: $rel. Evaluar Git LFS antes de incluirlo.") }
        elseif ($bytes -ge 50MB) { $warnings.Add("Archivo >=50 MiB: $rel. Evaluar Git LFS.") }
        if ($rel -match '\.(cs|unity|prefab|asset|meta|mat|controller|anim|json|yaml|yml|shader|hlsl|cginc|uss|uxml)$') {
            $content = Read-RepoText $rel
            if ($content -match '(?m)^(<<<<<<< .+|=======|>>>>>>> .+)\r?$') { $problems.Add("Marcadores de conflicto: $rel") }
        }
    }
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($rel in $assetFiles) { [void]$paths.Add($rel) }
    $guids = @{}
    foreach ($rel in $assetFiles) {
        if ($rel.EndsWith('.meta')) {
            $asset = $rel.Substring(0,$rel.Length-5)
            $isFolder = $false
            if ($Staged) { $isFolder = @($assetFiles | Where-Object { $_.StartsWith($asset + '/', [StringComparison]::Ordinal) }).Count -gt 0 }
            else { $isFolder = Test-Path -LiteralPath $asset -PathType Container }
            $metaText = Read-RepoText $rel
            if ($metaText -match '(?m)^folderAsset:\s*yes\s*$') { $isFolder = $true }
            if (!$paths.Contains($asset) -and !$isFolder) { $problems.Add("Meta sin su asset: $rel") }
            $text = Read-RepoText $rel
            if ($text -match '(?m)^guid:\s*([a-fA-F0-9]{32})\s*$') {
                $guid = $Matches[1].ToLowerInvariant()
                if ($guids.ContainsKey($guid)) { $problems.Add("GUID duplicado: $rel y $($guids[$guid])") } else { $guids[$guid]=$rel }
            } else { $problems.Add("Meta sin GUID valido: $rel") }
        } elseif (!$paths.Contains($rel + '.meta')) { $problems.Add("Falta incluir el .meta de: $rel") }
    }
    # Also detect case-only collisions that break on another operating system.
    $folded=@{}
    foreach ($rel in $assetFiles) {
        $key=$rel.ToLowerInvariant()
        if ($folded.ContainsKey($key) -and $folded[$key] -cne $rel) { $problems.Add("Colision de mayusculas: $rel / $($folded[$key])") }
        $folded[$key]=$rel
    }
    foreach ($rel in @('ProjectSettings/ProjectVersion.txt','Packages/manifest.json','Packages/packages-lock.json')) {
        if ($null -eq (Read-RepoText $rel)) { $problems.Add("Falta archivo de reproducibilidad: $rel") }
    }
    $editor = Read-RepoText 'ProjectSettings/EditorSettings.asset'
    if ($editor -notmatch '(?m)^\s*m_SerializationMode:\s*2\s*$') { $problems.Add('Activar Asset Serialization = Force Text.') }
    $vcs = Read-RepoText 'ProjectSettings/VersionControlSettings.asset'
    if ($vcs -notmatch 'm_Mode:\s*Visible Meta Files') { $problems.Add('Activar Version Control = Visible Meta Files.') }
    foreach ($rel in @('Packages/manifest.json','Packages/packages-lock.json')) {
        $json=Read-RepoText $rel
        if ($null -ne $json) { try { $null=ConvertFrom-Json -InputObject $json } catch { $problems.Add("JSON no valido: $rel") } }
    }
    foreach ($w in $warnings) { Write-Host "AVISO: $w" -ForegroundColor Yellow }
    foreach ($p in $problems) { Write-Host "ERROR: $p" -ForegroundColor Red }
    if ($problems.Count) { Write-Host "Comprobacion fallida: $($problems.Count) problema(s). No se modificaron archivos."; exit 1 }
    Write-Host "OK: metadatos, GUID, conflictos, archivos grandes y configuracion revisados. Modo staged: $Staged" -ForegroundColor Green
} finally { Pop-Location }
