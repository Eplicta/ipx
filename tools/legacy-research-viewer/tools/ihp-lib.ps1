# Shared helpers for the ihp-reference tools. Dot-source: . "$PSScriptRoot\ihp-lib.ps1"
# Targets Windows PowerShell 5.1 and PowerShell 7.

Set-StrictMode -Version 2.0
$script:IhpRoot = Split-Path -Parent $PSScriptRoot
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

function Read-IhpJson {
    param([Parameter(Mandatory)][string]$Path)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    return $text | ConvertFrom-Json
}

function Write-IhpJson {
    param([Parameter(Mandatory)]$Object, [Parameter(Mandatory)][string]$Path, [int]$Depth = 20)
    $json = $Object | ConvertTo-Json -Depth $Depth
    [System.IO.File]::WriteAllText($Path, $json, (New-Object System.Text.UTF8Encoding($false)))
}

function Get-IhpProp {
    # Safe property read under strict mode: returns $Default when the property is absent or null.
    param($Object, [string]$Name, $Default = $null)
    if ($null -eq $Object) { return $Default }
    $p = $Object.PSObject.Properties[$Name]
    if ($null -eq $p -or $null -eq $p.Value) { return $Default }
    return $p.Value
}

function New-IhpRetention {
    param([string]$Disposition = 'unclear', $Months = $null, [string]$Trigger = 'unspecified', [string]$Note = '')
    return [ordered]@{ disposition = $Disposition; months = $Months; trigger = $Trigger; note = $Note }
}

function ConvertTo-IhpRetentionFromKey {
    # FortDocs retention rule key -> normalised retention
    param([string]$Key)
    switch ($Key) {
        'retain'          { return New-IhpRetention 'retain' $null 'none' }
        'dispose-2y'      { return New-IhpRetention 'dispose' 24 'unspecified' }
        'dispose-5y'      { return New-IhpRetention 'dispose' 60 'unspecified' }
        'dispose-10y'     { return New-IhpRetention 'dispose' 120 'unspecified' }
        'dispose-obsolete'{ return New-IhpRetention 'dispose' $null 'obsolete' }
        default           { return $null }
    }
}

function ConvertTo-IhpRetentionFromText {
    # Conservative Swedish retention phrase -> normalised retention. Anything complex becomes 'unclear'.
    param([string]$Text)
    $t = ($Text + '').Trim()
    if ($t -eq '') { return New-IhpRetention 'unclear' $null 'unspecified' '' }
    $low = $t.ToLowerInvariant()
    $nums = [regex]::Matches($low, '(\d+)\s*(år|mån|månader|manader)')
    if ($nums.Count -gt 1 -or $low -match ';' -or ($low -match 'bevaras' -and $nums.Count -ge 1)) {
        return New-IhpRetention 'unclear' $null 'unspecified' $t
    }
    if ($nums.Count -eq 1) {
        $n = [int]$nums[0].Groups[1].Value
        $months = if ($nums[0].Groups[2].Value -like 'år') { $n * 12 } else { $n }
        $trigger = 'unspecified'
        if     ($low -match 'avslutad placering|avslutade placering|utskrivning|slutar|avslutad skolgång|avslutat skolgång|avslutad utbildning|avgång|lämnat skolan|avslutad gymnasie') { $trigger = 'enrolment-end' }
        elseif ($low -match 'årets slut|kalenderårets|verksamhetsårets|räkenskapsårets|årsskiftet') { $trigger = 'year-end' }
        elseif ($low -match 'läsårets|läsår') { $trigger = 'school-year-end' }
        elseif ($low -match 'avslutat ärende|ärendets avslut|efter avslut|avslutats|avslutad') { $trigger = 'case-closed' }
        elseif ($low -match 'beslut') { $trigger = 'decision-date' }
        elseif ($low -match 'upprättande|inkomst|skapad|upprättad') { $trigger = 'from-creation' }
        elseif ($low -match 'ersatt|ny version') { $trigger = 'superseded' }
        return New-IhpRetention 'dispose' $months $trigger $t
    }
    if ($low -match '^bevaras|^bevarande') { return New-IhpRetention 'retain' $null 'none' $t }
    if ($low -match 'inaktualitet|inaktuell|inte längre behövs|vid behov') { return New-IhpRetention 'dispose' $null 'obsolete' $t }
    if ($low -match 'utskrivning|utskriven|slutar') { return New-IhpRetention 'dispose' 0 'enrolment-end' $t }
    if ($low -match 'överlämnas|överlämning') { return New-IhpRetention 'handover' $null 'other' $t }
    return New-IhpRetention 'unclear' $null 'unspecified' $t
}

$script:TriggerShort = @{
    'from-creation' = 'skap'; 'year-end' = 'årsslut'; 'school-year-end' = 'läsår'; 'case-closed' = 'avslut'
    'enrolment-end' = 'utskr'; 'decision-date' = 'beslut'; 'superseded' = 'ersatt'; 'obsolete' = 'inakt'
    'other' = 'annan'; 'unspecified' = ''; 'none' = ''
}

function Format-IhpRetention {
    # Short, comparable display text for a retention object, e.g. "3 år/utskr", "Bevaras", "inakt".
    param($Retention)
    if ($null -eq $Retention) { return '-' }
    $disp = Get-IhpProp $Retention 'disposition' 'unclear'
    $months = Get-IhpProp $Retention 'months'
    $trig = Get-IhpProp $Retention 'trigger' 'unspecified'
    switch ($disp) {
        'retain'   { return 'Bevaras' }
        'handover' { return 'överlämnas' }
        'unclear'  { return '?' }
        'dispose'  {
            $short = $script:TriggerShort[$trig]
            if ($null -eq $short) { $short = '' }
            if ($null -eq $months) {
                if ($trig -eq 'obsolete') { return 'inakt' }
                if ($short -eq '') { return 'gallras' }
                return "vid/$short"
            }
            $m = [int]$months
            $span = if ($m -eq 0) { 'vid' } elseif ($m % 12 -eq 0) { '{0} år' -f ($m / 12) } else { '{0} mån' -f $m }
            if ($short -ne '') { return "$span/$short" } else { return $span }
        }
        default { return '?' }
    }
}

function Get-IhpRetentionRank {
    # Longer-keeping sorts higher. retain highest. Used only for colouring and strictness, never to convert triggers.
    param($Retention)
    if ($null -eq $Retention) { return -1 }
    switch (Get-IhpProp $Retention 'disposition' 'unclear') {
        'retain'  { return 100000 }
        'dispose' { $m = Get-IhpProp $Retention 'months'; if ($null -eq $m) { return 0 } else { return [int]$m } }
        default   { return -1 }
    }
}

function Get-IhpCityFiles {
    Get-ChildItem -LiteralPath (Join-Path $script:IhpRoot 'cities') -Filter '*.json' -File | Sort-Object Name
}

function Find-IhpHolder {
    # Resolve a name (fortdocs, klassa, kommunkod, or slug fragment) to a loaded holder object.
    param([Parameter(Mandatory)][string]$Name)
    $n = $Name.Trim().ToLowerInvariant()
    if ($n -match '^\d{1,4}$') { $n = $n.PadLeft(4, '0') }
    if ($n -in @('fortdocs', 'klassa', 'skr')) {
        $p = Join-Path $script:IhpRoot "baselines\$n.json"
        if (Test-Path -LiteralPath $p) { return Read-IhpJson $p }
        throw "Baseline '$n' not found at $p"
    }
    $hits = @(Get-IhpCityFiles | Where-Object { $_.BaseName -eq $n -or $_.BaseName -like "$n-*" -or $_.BaseName -like "*-$n" })
    if ($hits.Count -eq 0) { throw "No city file matches '$Name'." }
    if ($hits.Count -gt 1) { throw "'$Name' matches several: $($hits.BaseName -join ', ')" }
    return Read-IhpJson $hits[0].FullName
}
