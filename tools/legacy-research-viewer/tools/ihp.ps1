# Console viewer for the ihp-reference data.
#   ihp.ps1 list
#   ihp.ps1 city <city> [-Form <form>] [-Full]
#   ihp.ps1 compare -With <city>,<city> [-Base fortdocs] [-Form <form>] [-OnlyDiffs] [-NoKlassa]
#   ihp.ps1 coverage
#   ihp.ps1 missing [-City <city>] [-NextStep contact-city|recheck-homepage|ask-archive|retry-later]
#   ihp.ps1 gaps -With <city>,<city> [-Form <form>]
#   ihp.ps1 validate [-Path <city file>]
# Add -Plain to drop colours (for copying the output).
[CmdletBinding()]
param(
    [Parameter(Position = 0)][string]$Command = 'help',
    [Parameter(Position = 1)][string]$City,
    [string[]]$With,
    [string]$Base = 'fortdocs',
    [string]$Form,
    [string]$NextStep,
    [string]$Path,
    [switch]$Full,
    [switch]$OnlyDiffs,
    [switch]$Expand,
    [string]$Item,
    [string]$Status,
    [string]$Via,
    [string]$Note,
    [string]$OutDir,
    [switch]$NoKlassa,
    [switch]$Plain,
    [int]$TitleWidth = 44,
    [int]$ColWidth = 17
)
. "$PSScriptRoot\ihp-lib.ps1"

if ($With) { $With = @($With | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }

$script:Vocab = Read-IhpJson (Join-Path $script:IhpRoot 'structure\vocab.json')
$script:NodesDoc = Read-IhpJson (Join-Path $script:IhpRoot 'structure\nodes.json')
$script:Nodes = @($script:NodesDoc.nodes)
$script:KlassaSeen = @{}
$script:NodeById = @{}
foreach ($n in $script:Nodes) { $script:NodeById[$n.id] = $n }

function W {
    param([string]$Text = '', [string]$Color = '', [switch]$NoNewLine)
    if ($Plain -or $Color -eq '') { Write-Host $Text -NoNewline:$NoNewLine }
    else { Write-Host $Text -ForegroundColor $Color -NoNewline:$NoNewLine }
}

function Fit([string]$s, [int]$w) {
    if ($null -eq $s) { $s = '' }
    $s = ($s -replace '\s+', ' ')
    if ($s.Length -gt $w) { return $s.Substring(0, [Math]::Max(0, $w - 1)) + '…' }
    return $s.PadRight($w)
}

function Get-Rules($holder) { @(Get-IhpProp $holder 'rules' @()) }

function Get-FormName([string]$key) {
    $v = $script:Vocab.forms.PSObject.Properties[$key]
    if ($v) { return $v.Value } else { return $key }
}

function Get-Children([string]$id) { @($script:Nodes | Where-Object { (Get-IhpProp $_ 'parent') -eq $id }) }

function Get-LeafIds([string]$id) {
    $n = $script:NodeById[$id]
    if ((Get-IhpProp $n 'kind') -eq 'leaf') { return , @($id) }
    $out = @()
    foreach ($c in (Get-Children $id)) { $out += @(Get-LeafIds $c.id) }
    return , $out
}

function New-Index($holder) {
    # nodeId -> @{ rules = @(...); rels = @(...) }
    $idx = @{}
    foreach ($r in (Get-Rules $holder)) {
        foreach ($m in @(Get-IhpProp $r 'nodes' @())) {
            $nid = $m.node
            if (-not $idx.ContainsKey($nid)) { $idx[$nid] = New-Object System.Collections.ArrayList }
            [void]$idx[$nid].Add([pscustomobject]@{ Rule = $r; Relation = (Get-IhpProp $m 'relation' 'exact') })
        }
    }
    return $idx
}

function Get-Cell($idx, [string]$nodeId) {
    $inherited = $false
    if (-not $idx.ContainsKey($nodeId)) {
        $node = $script:NodeById[$nodeId]
        $par = Get-IhpProp $node 'parent'
        $parNode = $(if ($par -and $script:NodeById.ContainsKey($par)) { $script:NodeById[$par] } else { $null })
        if ($parNode -and (Get-IhpProp $parNode 'parent') -and $idx.ContainsKey($par)) { $nodeId = $par; $inherited = $true }
        else { return [pscustomobject]@{ Text = '-'; Retention = $null; Marks = ''; Has = $false } }
    }
    $notRetention = @('instruction', 'promise', 'placeholder', 'general-principle', 'recommendation-quote')
    $hits = @($idx[$nodeId] | Where-Object { (Get-IhpProp $_.Rule 'current' $true) -ne $false -and (Get-IhpProp $_.Rule 'ruleType' 'plan-rule') -notin $notRetention })
    if ($hits.Count -eq 0) { return [pscustomobject]@{ Text = '-'; Retention = $null; Marks = ''; Has = $false } }
    $prio = @{ exact = 0; covers = 1; part = 2; similar = 3 }
    $tier = ($hits | ForEach-Object { $prio[$_.Relation] } | Measure-Object -Minimum).Minimum
    $hits = @($hits | Where-Object { $prio[$_.Relation] -eq $tier })
    $best = $hits | Sort-Object { Get-IhpRetentionRank (Get-IhpProp $_.Rule 'retention') } -Descending | Select-Object -First 1
    $ret = Get-IhpProp $best.Rule 'retention'
    $marks = ''
    if ($hits.Count -gt 1) { $marks += '+' }
    if ($best.Relation -eq 'covers') { $marks += '~' }
    if ($best.Relation -eq 'part') { $marks += '/' }
    if ($best.Relation -eq 'similar') { $marks += '≈' }
    if ((Get-IhpProp $best.Rule 'ruleType' 'plan-rule') -in @('one-off-decision', 'gdpr-limit', 'platform-statement')) { $marks += '°' }
    $text = Format-IhpRetention $ret
    if ($hits.Count -gt 1) {
        $ranked = @($hits | Where-Object { (Get-IhpRetentionRank (Get-IhpProp $_.Rule 'retention')) -ge 0 } | Sort-Object { Get-IhpRetentionRank (Get-IhpProp $_.Rule 'retention') })
        if ($ranked.Count -gt 1) {
            $lo = Format-IhpRetention (Get-IhpProp $ranked[0].Rule 'retention')
            $hi = Format-IhpRetention (Get-IhpProp $ranked[-1].Rule 'retention')
            if ($lo -ne $hi) { $text = "$lo…$hi" }
        }
    }
    if ($inherited) { $marks += '^' }
    return [pscustomobject]@{ Text = ($text + $marks); Retention = $ret; Marks = $marks; Has = $true }
}

function Get-Delta($base, $other) {
    if (-not $other.Has) { return 'norule' }
    if (-not $base.Has) { return 'nobase' }
    $a = $base.Retention; $b = $other.Retention
    $da = Get-IhpProp $a 'disposition' 'unclear'; $db = Get-IhpProp $b 'disposition' 'unclear'
    if ($da -eq 'unclear' -or $db -eq 'unclear') { return 'unclear' }
    if ($da -eq 'handover' -or $db -eq 'handover') { return $(if ($da -eq $db) { 'same' } else { 'differs' }) }
    if ($da -eq 'retain' -and $db -eq 'retain') { return 'same' }
    if ($da -eq 'retain') { return 'shorter' }
    if ($db -eq 'retain') { return 'longer' }
    $ma = Get-IhpProp $a 'months'; $mb = Get-IhpProp $b 'months'
    $ta = Get-IhpProp $a 'trigger' 'unspecified'; $tb = Get-IhpProp $b 'trigger' 'unspecified'
    if ($null -eq $ma -and $null -eq $mb) { return $(if ($ta -eq $tb) { 'same' } else { 'notcomparable' }) }
    if ($null -eq $ma -or $null -eq $mb) { return 'notcomparable' }
    if ($ta -ne $tb -and $ta -ne 'unspecified' -and $tb -ne 'unspecified') { return 'notcomparable' }
    if ([int]$ma -eq [int]$mb) { return 'same' }
    if ([int]$mb -lt [int]$ma) { return 'shorter' } else { return 'longer' }
}

$script:DeltaColor = @{ same = 'Green'; shorter = 'Yellow'; longer = 'Magenta'; notcomparable = 'DarkYellow'; unclear = 'Red'; differs = 'Red'; norule = 'DarkGray'; nobase = 'Gray' }

function Get-RetentionColor($ret) {
    if ($null -eq $ret) { return 'DarkGray' }
    switch (Get-IhpProp $ret 'disposition' 'unclear') {
        'retain' { return 'Magenta' }
        'unclear' { return 'Red' }
        'handover' { return 'Cyan' }
        default {
            $m = Get-IhpProp $ret 'months'
            if ($null -eq $m) { return 'DarkYellow' }
            if ([int]$m -ge 84) { return 'Yellow' }
            if ([int]$m -ge 36) { return 'Green' }
            return 'Cyan'
        }
    }
}

function Get-KlassaCodes([string]$nodeId) {
    $k = Find-IhpHolder 'klassa'
    $codes = @()
    foreach ($m in @(Get-IhpProp $k 'nodeMap' @())) {
        if ($nodeId -match $m.pattern) { $codes += @($m.klassa) }
    }
    return ($codes | Select-Object -Unique) -join ','
}

function Get-CityLabel($holder) {
    $mun = Get-IhpProp $holder 'municipality'
    if ($mun) { return ('{0}' -f $mun.name) }
    return (Get-IhpProp $holder 'id' '?')
}

function Show-Header([string]$title) { W ''; W $title 'Cyan'; W ('-' * [Math]::Min(110, $title.Length + 4)) 'DarkGray' }

# ---------------------------------------------------------------- list
function Invoke-List {
    Show-Header 'Cities'
    $rows = foreach ($f in Get-IhpCityFiles) {
        $c = Read-IhpJson $f.FullName
        $plans = @(Get-IhpProp $c 'plans' @())
        $src = @(Get-IhpProp $c 'sources' @())
        $newest = ($plans | ForEach-Object { Get-IhpProp $_ 'lastKnownPlanDate' } | Where-Object { $_ } | Sort-Object -Descending | Select-Object -First 1)
        $fetched = ($src | ForEach-Object { Get-IhpProp $_ 'lastFetchedAt' } | Where-Object { $_ } | Sort-Object -Descending | Select-Object -First 1)
        [pscustomobject][ordered]@{
            Code = $c.municipality.code; City = $c.municipality.name; Coverage = (Get-IhpProp $c 'coverage' '?')
            Plans = $plans.Count; Status = ((@($plans | ForEach-Object { Get-IhpProp $_ 'status' '?' }) | Select-Object -Unique) -join ',')
            LastPlanDate = $newest; LastFetched = $fetched
            Rules = @(Get-IhpProp $c 'rules' @()).Count; Unmapped = @(Get-IhpProp $c 'unmappedRules' @()).Count; Missing = @(Get-IhpProp $c 'missing' @()).Count
        }
    }
    if ($Plain) { $rows | Format-Table -AutoSize | Out-String -Width 200 | Write-Host } else { $rows | Format-Table -AutoSize }
}

# ---------------------------------------------------------------- city
function Invoke-City {
    if (-not $City) { throw 'Usage: ihp.ps1 city <city>' }
    $c = Find-IhpHolder $City
    $mun = Get-IhpProp $c 'municipality'
    if (-not $mun) { $mun = [pscustomobject]@{ name = (Get-IhpProp $c 'name' (Get-IhpProp $c 'id' '?')); code = (Get-IhpProp $c 'id' ''); county = '' } }
    Show-Header ('{0} ({1}) {2}' -f $mun.name.ToUpper(), $mun.code, (Get-IhpProp $mun 'county' ''))
    W ('coverage: {0}   updated: {1}' -f (Get-IhpProp $c 'coverage' '?'), (Get-IhpProp $c 'updatedOn' '?'))
    foreach ($o in @(Get-IhpProp $c 'operators' @())) { W ('  runs {0}: {1}' -f (@($o.forms | ForEach-Object { Get-FormName $_ }) -join ', '), (Get-IhpProp $o 'body' '?')) }
    W ''
    W 'PLANS' 'Cyan'
    foreach ($p in @(Get-IhpProp $c 'plans' @())) {
        W ('  {0}  {1}' -f $p.id, $p.name) 'White'
        W ('      body: {0}  scope: {1}  status: {2}  structure: {3}' -f (Get-IhpProp $p 'body' '?'), (Get-IhpProp $p 'scope' '?'), (Get-IhpProp $p 'status' '?'), (Get-IhpProp $p 'structure' '?'))
        W ('      decided: {0} {1}  published: {2} ({3})  last known: {4}' -f (Get-IhpProp $p 'decidedDate' '?'), (Get-IhpProp $p 'decisionRef' ''), (Get-IhpProp $p 'publishedDate' '?'), (Get-IhpProp $p 'publishedDateBasis' '?'), (Get-IhpProp $p 'lastKnownPlanDate' '?'))
        W ('      forms: {0}' -f ((@(Get-IhpProp $p 'forms' @()) | ForEach-Object { Get-FormName $_ }) -join ', '))
        $in = Get-IhpProp $p 'inaktualitet'
        if ($in) { W ('      vid inaktualitet defined: {0} {1}' -f (Get-IhpProp $in 'defined' '?'), (Get-IhpProp $in 'text' '')) }
        $as = Get-IhpProp $p 'assessment'
        if ($as) { W ('      assessment: {0} - {1}' -f (Get-IhpProp $as 'level' '?'), (Get-IhpProp $as 'note' '')) }
        foreach ($d in @(Get-IhpProp $p 'delegatesTo' @())) { W ('      delegates to {0}: {1}' -f $d.planId, ((@(Get-IhpProp $d 'topics' @())) -join ', ')) }
    }
    W ''
    W 'SOURCES' 'Cyan'
    foreach ($s in @(Get-IhpProp $c 'sources' @())) {
        W ('  {0}  {1}' -f $s.id, (Get-IhpProp $s 'title' '')) 'White'
        W ('      published {0} ({1})   fetched {2} ({3})' -f (Get-IhpProp $s 'publishedDate' '?'), (Get-IhpProp $s 'publishedDateBasis' '?'), (Get-IhpProp $s 'lastFetchedAt' '?'), (Get-IhpProp $s 'fetchedBasis' '?'))
        W ('      {0}' -f (Get-IhpProp $s 'uri' '(no uri)')) 'DarkGray'
    }

    $rules = @(Get-IhpProp $c 'rules' @())
    $forms = @($script:Vocab.forms.PSObject.Properties.Name)
    foreach ($f in $forms) {
        if ($Form -and $f -ne $Form) { continue }
        $fr = @($rules | Where-Object { @(Get-IhpProp $_ 'forms' @()) -contains $f })
        if ($fr.Count -eq 0) { continue }
        Show-Header ('{0}  ({1} rules)' -f (Get-FormName $f), $fr.Count)
        foreach ($r in $fr) {
            $aw = Get-IhpProp $r 'asWritten'
            $ret = Get-IhpProp $r 'retention'
            $nodeIds = @(Get-IhpProp $r 'nodes' @()) | ForEach-Object { $_.node }
            W (Fit (Get-IhpProp $aw 'code' '') 8) 'DarkGray' -NoNewLine
            W (Fit (Get-IhpProp $aw 'heading' '') ($TitleWidth + 6)) '' -NoNewLine
            W (Fit (Format-IhpRetention $ret) 16) (Get-RetentionColor $ret) -NoNewLine
            $short = ($nodeIds | ForEach-Object { ($_ -split '\.', 2)[-1] }) -join ', '
            W (Fit $short 50) 'DarkGray'
            if ($Full) {
                W ('        "{0}"' -f (Get-IhpProp $aw 'text' '')) 'Gray'
                $src = Get-IhpProp $r 'source'
                if ($src) { W ('        source {0} p.{1}  confidence {2}' -f (Get-IhpProp $src 'ref' '?'), (Get-IhpProp $src 'page' '?'), (Get-IhpProp $r 'confidence' '?')) 'DarkGray' }
            }
        }
    }
    $um = @(Get-IhpProp $c 'unmappedRules' @())
    if ($um.Count -gt 0 -and -not $Form) {
        Show-Header ('Unmapped rules ({0}): fit no node yet' -f $um.Count)
        foreach ($r in $um) {
            $aw = Get-IhpProp $r 'asWritten'; $ret = Get-IhpProp $r 'retention'; $sn = Get-IhpProp $r 'suggestedNode'
            W (Fit (Get-IhpProp $aw 'code' '') 8) 'DarkGray' -NoNewLine
            W (Fit (Get-IhpProp $aw 'heading' '') ($TitleWidth + 6)) '' -NoNewLine
            W (Fit (Format-IhpRetention $ret) 16) (Get-RetentionColor $ret) -NoNewLine
            W (Fit ('suggest: ' + (Get-IhpProp $sn 'id' '?')) 50) 'DarkGray'
        }
    }
    Show-MissingOf $c
}

function Show-MissingOf($c) {
    $m = @(Get-IhpProp $c 'missing' @())
    Show-Header ('Missing ({0})' -f $m.Count)
    foreach ($x in $m) {
        if ($NextStep -and (Get-IhpProp $x 'nextStep') -ne $NextStep) { continue }
        W ('  [{0}] {1}  forms: {2}' -f (Get-IhpProp $x 'nextStep' '?'), $x.kind, ((@(Get-IhpProp $x 'forms' @())) -join ',')) 'Yellow'
        W ('      {0}' -f (Get-IhpProp $x 'detail' '')) ''
        $ct = Get-IhpProp $x 'contact'
        if ($ct) { W ('      contact: {0} {1}' -f (Get-IhpProp $ct 'role' ''), (Get-IhpProp $ct 'address' '')) 'DarkGray' }
    }
}

# ---------------------------------------------------------------- compare
function Get-FormGroupOrder {
    $forms = @($script:Vocab.forms.PSObject.Properties.Name)
    return $forms
}

function Get-RuleHeading($r) {
    $aw = Get-IhpProp $r 'asWritten'
    $text = [string](Get-IhpProp $aw 'text' '')
    $m = [regex]::Match($text, 'Handlingstyp:\s*([^;]+)')
    if ($m.Success) { return $m.Groups[1].Value.Trim() }
    return ([string](Get-IhpProp $aw 'heading' '') -replace '\s+', ' ')
}

function Get-CellRules($idx, [string]$nodeId) {
    $inherited = $false
    if (-not $idx.ContainsKey($nodeId)) {
        $node = $script:NodeById[$nodeId]
        $par = Get-IhpProp $node 'parent'
        $parNode = $(if ($par -and $script:NodeById.ContainsKey($par)) { $script:NodeById[$par] } else { $null })
        if ($parNode -and (Get-IhpProp $parNode 'parent') -and $idx.ContainsKey($par)) { $nodeId = $par; $inherited = $true }
        else { return @() }
    }
    return @($idx[$nodeId] | ForEach-Object { [pscustomobject]@{ Rule = $_.Rule; Relation = $_.Relation; Inherited = $inherited } })
}

function New-CompareLine($node, [int]$indent, $holders) {
    $cells = @()
    foreach ($h in $holders) { $cells += , (Get-Cell $h.Index $node.id) }
    $deltas = @('base')
    for ($i = 1; $i -lt $cells.Count; $i++) { $deltas += (Get-Delta $cells[0] $cells[$i]) }
    $anyDiff = $false
    for ($i = 1; $i -lt $deltas.Count; $i++) { if ($deltas[$i] -notin @('same')) { $anyDiff = $true } }
    if ($indent -gt 0 -and @($cells | Select-Object -Skip 1 | Where-Object { $_.Has }).Count -eq 0) { return $null }
    if ($OnlyDiffs -and -not $anyDiff) { return $null }
    if ($OnlyDiffs -and (@($cells | Where-Object { $_.Has }).Count -eq 0)) { return $null }
    return [pscustomobject]@{ IsHeader = $false; Indent = $indent; Title = (Get-IhpProp (Get-IhpProp $node 'title') 'sv' $node.id); Cells = $cells; Deltas = $deltas; Node = $node.id }
}

function Invoke-Compare {
    if (-not $With -or $With.Count -eq 0) { throw 'Usage: ihp.ps1 compare -With <city>,<city> [-Base fortdocs] [-Form <form>]' }
    $holders = @()
    $baseHolder = Find-IhpHolder $Base
    $holders += [pscustomobject]@{ Label = 'BASE ' + (Get-CityLabel $baseHolder); Holder = $baseHolder; Index = (New-Index $baseHolder) }
    foreach ($w in $With) {
        $h = Find-IhpHolder $w
        $holders += [pscustomobject]@{ Label = (Get-CityLabel $h); Holder = $h; Index = (New-Index $h) }
    }
    $showKlassa = -not $NoKlassa
    W ''
    W 'Legend: ' '' -NoNewLine
    W 'same ' 'Green' -NoNewLine; W 'shorter ' 'Yellow' -NoNewLine; W 'longer ' 'Magenta' -NoNewLine; W 'not comparable ' 'DarkYellow' -NoNewLine; W 'unclear ' 'Red' -NoNewLine; W 'no rule ' 'DarkGray'
    W '  (shorter/longer = city versus base. + several rules, ~ covered by a broader rule, / covers only part, ≈ only similar, ° not a standing plan rule (one-off decision, GDPR limit), ^ inherited from the parent node. KLASSA shows the mapped code.)' 'DarkGray'
    W 'Retention shows the period/what it counts from, e.g. 3 år/utskr = 3 years after the child leaves. Different triggers are not compared.' 'DarkGray'

    $header = (Fit 'Node' $TitleWidth)
    foreach ($h in $holders) { $header += ' ' + (Fit $h.Label $ColWidth) }
    if ($showKlassa) { $header += ' ' + (Fit 'KLASSA' 9) }

    foreach ($f in (Get-FormGroupOrder)) {
        if ($Form -and $f -ne $Form) { continue }
        $leaves = @($script:Nodes | Where-Object { $_.form -eq $f -and $_.kind -eq 'leaf' })
        if ($leaves.Count -eq 0) { continue }
        $rootIds = @($script:Nodes | Where-Object { $_.form -eq $f -and $_.kind -eq 'group' -and -not (Get-IhpProp $_ 'parent') } | ForEach-Object { $_.id })
        $tops = @($script:Nodes | Where-Object { $_.form -eq $f -and ((Get-IhpProp $_ 'parent') -in $rootIds) })
        $lines = New-Object System.Collections.Generic.List[object]
        foreach ($top in $tops) {
            if ($top.kind -eq 'leaf') {
                $l = New-CompareLine $top 0 $holders
                if ($l) { $lines.Add($l) }
            } else {
                $kids = @($script:Nodes | Where-Object { (Get-IhpProp $_ 'parent') -eq $top.id -and $_.kind -eq 'leaf' })
                $kidLines = @()
                foreach ($k in $kids) { $l = New-CompareLine $k 1 $holders; if ($l) { $kidLines += $l } }
                if ($kidLines.Count -gt 0) {
                    $lines.Add([pscustomobject]@{ IsHeader = $true; Title = ('▸ ' + (Get-IhpProp (Get-IhpProp $top 'title') 'sv' $top.id)); Node = $top.id })
                    foreach ($l in $kidLines) { $lines.Add($l) }
                }
            }
        }
        # Collapse the whole form into one line when every leaf has the same value in every column.
        Show-Header ('{0}  ({1} nodes{2})' -f (Get-FormName $f), $leaves.Count, $(if ($OnlyDiffs) { ", showing $($lines.Count) that differ" } else { '' }))
        if ($lines.Count -eq 0) { W '  (nothing to show)' 'DarkGray'; continue }
        W $header 'DarkGray'
        $dataLines = @($lines | Where-Object { -not (Get-IhpProp $_ 'IsHeader' $false) })
        $sig = @($dataLines | ForEach-Object { ($_.Cells | ForEach-Object { $_.Text }) -join '|' } | Select-Object -Unique)
        if ($lines.Count -gt 3 -and $sig.Count -eq 1 -and $dataLines.Count -eq $lines.Count) {
            $l = $lines[0]
            W (Fit ('(all {0} nodes alike)' -f $lines.Count) $TitleWidth) '' -NoNewLine
            for ($i = 0; $i -lt $l.Cells.Count; $i++) { W (' ' + (Fit $l.Cells[$i].Text $ColWidth)) $(if ($i -eq 0) { Get-RetentionColor $l.Cells[$i].Retention } else { $script:DeltaColor[$l.Deltas[$i]] }) -NoNewLine }
            W ''
            continue
        }
        foreach ($l in $lines) {
            if (Get-IhpProp $l 'IsHeader' $false) { W (Fit $l.Title $TitleWidth) 'White'; continue }
            W (Fit (('  ' * $l.Indent) + $l.Title) $TitleWidth) '' -NoNewLine
            for ($i = 0; $i -lt $l.Cells.Count; $i++) {
                $color = if ($i -eq 0) { Get-RetentionColor $l.Cells[$i].Retention } else { $script:DeltaColor[$l.Deltas[$i]] }
                W (' ' + (Fit $l.Cells[$i].Text $ColWidth)) $color -NoNewLine
            }
            if ($showKlassa) {
                $kc = Get-KlassaCodes $l.Node
                foreach ($code in ($kc -split ',')) { if ($code) { $script:KlassaSeen[$code] = $true } }
                W (' ' + (Fit $kc 9)) 'Cyan' -NoNewLine
            }
            W ''
            if ($Expand) {
                for ($i = 0; $i -lt $holders.Count; $i++) {
                    $seen = [ordered]@{}
                    foreach ($hit in @(Get-CellRules $holders[$i].Index $l.Node)) {
                        $r = $hit.Rule
                        $aw = Get-IhpProp $r 'asWritten'
                        $src = Get-IhpProp $r 'source'
                        $head = Get-RuleHeading $r
                        $cur = if ((Get-IhpProp $r 'current' $true) -eq $false) { ' [ej gällande]' } else { '' }
                        $inh = if ($hit.Inherited) { ' [ärvd från överordnad nod]' } else { '' }
                        $val = Format-IhpRetention (Get-IhpProp $r 'retention')
                        $pg = Get-IhpProp $src 'page'
                        $pgText = $(if ($null -eq $pg) { '' } else { "p$pg" })
                        $line = '{0,-9} {1,-7} {2,-18} {3,-5} {4}{5}{6}' -f (Fit $holders[$i].Label 9).Trim(), $hit.Relation, $val, $pgText, (Fit $head 78).TrimEnd(), $cur, $inh
                        $k = $line + '|' + (Get-RetentionColor (Get-IhpProp $r 'retention'))
                        if ($seen.Contains($k)) { $seen[$k]++ } else { $seen[$k] = 1 }
                    }
                    foreach ($k in $seen.Keys) {
                        $parts = $k -split '\|(?=[A-Za-z]+$)'
                        $suffix = if ($seen[$k] -gt 1) { ('  x' + $seen[$k]) } else { '' }
                        W ('      ' + $parts[0] + $suffix) $parts[1]
                    }
                }
                W ''
            }
        }
    }
    if ($showKlassa -and $script:KlassaSeen.Count -gt 0) {
        $kl = Find-IhpHolder 'klassa'
        W ''
        W 'KLASSA key (process codes, not version numbers; 3.5.x are from SKR/Riksarkivet, 1.x-2.x are illustrative):' 'Cyan'
        foreach ($s in @($kl.structure | Where-Object { $script:KlassaSeen.ContainsKey($_.code) } | Sort-Object { [version]($_.code + '.0') })) {
            W ('  {0,-6} {1}' -f $s.code, $s.title) 'DarkGray'
        }
    }
    # Unmapped rules per city: rules the common structure does not hold yet.
    W ''
    foreach ($h in $holders[1..($holders.Count - 1)]) {
        $um = @(Get-IhpProp $h.Holder 'unmappedRules' @())
        $code = Get-IhpProp (Get-IhpProp $h.Holder 'municipality') 'code' $h.Label
        if ($um.Count -gt 0) { W ('{0}: {1} rules fit no node yet (see: ihp.ps1 city {2})' -f $h.Label, $um.Count, $code) 'DarkGray' }
    }
}

# ---------------------------------------------------------------- coverage
function Invoke-Coverage {
    Show-Header 'Coverage: leaf nodes that have at least one rule, per school form'
    $forms = @($script:Vocab.forms.PSObject.Properties.Name | Where-Object { $_ -ne 'other-education' })
    $cities = @(Get-IhpCityFiles | ForEach-Object { Read-IhpJson $_.FullName })
    $skrPath = Join-Path $script:IhpRoot 'baselines\skr.json'
    if (Test-Path -LiteralPath $skrPath) { $cities += Read-IhpJson $skrPath }
    $rows = foreach ($f in $forms) {
        $leaves = @($script:Nodes | Where-Object { $_.form -eq $f -and $_.kind -eq 'leaf' })
        $o = [ordered]@{ Form = (Get-FormName $f); Nodes = $leaves.Count }
        foreach ($c in $cities) {
            $idx = New-Index $c
            $have = @($leaves | Where-Object { $idx.ContainsKey($_.id) }).Count
            $um = @(Get-IhpProp $c 'unmappedRules' @() | Where-Object { @(Get-IhpProp (Get-IhpProp $_ 'suggestedNode') 'form') -contains $f }).Count
            $lbl = Get-CityLabel $c
            $o[$lbl] = if ($leaves.Count -gt 0) { '{0}/{1}' -f $have, $leaves.Count } else { '-' }
            if ($um -gt 0) { $o[$lbl] += ' +' + $um + 'u' }
        }
        [pscustomobject]$o
    }
    if ($Plain) { $rows | Format-Table -AutoSize | Out-String -Width 200 | Write-Host } else { $rows | Format-Table -AutoSize }
    W 'n/m = nodes with a rule / nodes in the structure.  +Nu = N rules that fit no node yet (unmapped).' 'DarkGray'
}

# ---------------------------------------------------------------- missing
function Invoke-Missing {
    $files = @(Get-IhpCityFiles)
    if ($City) { $files = @($files | Where-Object { $_.BaseName -like "*$($City.ToLowerInvariant())*" }) }
    $rows = foreach ($f in $files) {
        $c = Read-IhpJson $f.FullName
        foreach ($x in @(Get-IhpProp $c 'missing' @())) {
            if ($NextStep -and (Get-IhpProp $x 'nextStep') -ne $NextStep) { continue }
            $ct = Get-IhpProp $x 'contact'
            [pscustomobject][ordered]@{
                City = $c.municipality.name; Kind = $x.kind; Forms = ((@(Get-IhpProp $x 'forms' @())) -join ','); Next = (Get-IhpProp $x 'nextStep' '?')
                Detail = (Fit (Get-IhpProp $x 'detail' '') 90).TrimEnd(); Contact = $(if ($ct) { Get-IhpProp $ct 'address' (Get-IhpProp $ct 'role' '') } else { '' })
            }
        }
    }
    Show-Header 'Missing, by city (next step per item)'
    if ($Plain) { $rows | Format-Table -AutoSize -Wrap | Out-String -Width 250 | Write-Host } else { $rows | Format-Table -AutoSize -Wrap }
    W 'Totals by next step:' 'Cyan'
    $rows | Group-Object Next | Sort-Object Count -Descending | ForEach-Object { W ('  {0,-18} {1}' -f $_.Name, $_.Count) }
}

# ---------------------------------------------------------------- gaps
function Invoke-Gaps {
    if (-not $With) { throw 'Usage: ihp.ps1 gaps -With <city>,<city> [-Form <form>]' }
    $hs = @($With | ForEach-Object { $h = Find-IhpHolder $_; [pscustomobject]@{ Label = (Get-CityLabel $h); Index = (New-Index $h) } })
    $base = Find-IhpHolder 'fortdocs'; $bi = New-Index $base
    Show-Header 'Nodes where the cities split a rule over several rules, or disagree with each other'
    foreach ($leaf in @($script:Nodes | Where-Object { $_.kind -eq 'leaf' -and (-not $Form -or $_.form -eq $Form) })) {
        $cells = @($hs | ForEach-Object { Get-Cell $_.Index $leaf.id })
        $has = @($cells | Where-Object { $_.Has })
        if ($has.Count -lt 2) { continue }
        $distinct = @($has | ForEach-Object { $_.Text } | Select-Object -Unique)
        $split = @($hs | Where-Object { $_.Index.ContainsKey($leaf.id) -and @($_.Index[$leaf.id]).Count -gt 1 })
        if ($distinct.Count -lt 2 -and $split.Count -eq 0) { continue }
        W (Fit $leaf.id 62) '' -NoNewLine
        W ('base ' + (Fit (Get-Cell $bi $leaf.id).Text 12)) 'Gray' -NoNewLine
        W (($hs | ForEach-Object -Begin { $i = 0 } -Process { '{0}={1}' -f $_.Label, $cells[$i].Text; $i++ }) -join '  ') 'Yellow'
    }
}

# ---------------------------------------------------------------- validate
function Invoke-Validate {
    $files = if ($Path) { @(Get-Item -LiteralPath $Path) } else { @(Get-IhpCityFiles) }
    $dateRx = '^\d{4}-\d{2}-\d{2}'
    $bad = 0
    foreach ($f in $files) {
        $errs = New-Object System.Collections.Generic.List[string]
        $warn = New-Object System.Collections.Generic.List[string]
        try { $c = Read-IhpJson $f.FullName } catch { W ('{0}: INVALID JSON: {1}' -f $f.Name, $_.Exception.Message) 'Red'; $bad++; continue }
        $isBase = (Get-IhpProp $c 'kind') -eq 'baseline'
        $mun = Get-IhpProp $c 'municipality'
        if ($isBase) { }
        elseif (-not $mun -or (Get-IhpProp $mun 'code' '') -notmatch '^\d{4}$') { $errs.Add('municipality.code must be 4 digits') }
        elseif (-not $f.BaseName.StartsWith($mun.code + '-')) { $warn.Add("file name should start with the kommunkod '$($mun.code)-'") }
        $planIds = @(Get-IhpProp $c 'plans' @() | ForEach-Object { $_.id })
        $srcIds = @(Get-IhpProp $c 'sources' @() | ForEach-Object { $_.id })
        foreach ($p in @(Get-IhpProp $c 'plans' @())) {
            if (-not $script:Vocab.planStatus.PSObject.Properties[(Get-IhpProp $p 'status' '')]) { $errs.Add("plan $($p.id): bad status '$(Get-IhpProp $p 'status' '')'") }
            foreach ($fm in @(Get-IhpProp $p 'forms' @())) { if (-not $script:Vocab.forms.PSObject.Properties[$fm]) { $errs.Add("plan $($p.id): unknown form '$fm'") } }
            foreach ($d in 'decidedDate', 'publishedDate', 'lastKnownPlanDate') { $v = Get-IhpProp $p $d; if ($v -and $v -notmatch $dateRx) { $errs.Add("plan $($p.id): $d '$v' is not an ISO date") } }
            foreach ($sid in @(Get-IhpProp $p 'sourceIds' @())) { if ($sid -notin $srcIds) { $errs.Add("plan $($p.id): unknown source '$sid'") } }
        }
        foreach ($s in @(Get-IhpProp $c 'sources' @())) {
            if (-not (Get-IhpProp $s 'uri')) { $warn.Add("source $($s.id): no uri") }
            if (-not (Get-IhpProp $s 'lastFetchedAt')) { $warn.Add("source $($s.id): no lastFetchedAt") }
            foreach ($d in 'publishedDate', 'lastFetchedAt') { $v = Get-IhpProp $s $d; if ($v -and $v -notmatch $dateRx) { $errs.Add("source $($s.id): $d '$v' is not an ISO date") } }
        }
        $seen = @{}
        foreach ($set in 'rules', 'unmappedRules') {
            foreach ($r in @(Get-IhpProp $c $set @())) {
                $rid = Get-IhpProp $r 'id' '?'
                if ($seen.ContainsKey($rid)) { $errs.Add("duplicate rule id $rid") }; $seen[$rid] = $true
                $aw = Get-IhpProp $r 'asWritten'
                if (-not (Get-IhpProp $aw 'text')) { $errs.Add("$set $rid`: asWritten.text is empty") }
                $ret = Get-IhpProp $r 'retention'
                if (-not $ret) { $errs.Add("$set $rid`: no retention") }
                else {
                    $disp = Get-IhpProp $ret 'disposition' ''
                    $trig = Get-IhpProp $ret 'trigger' ''
                    if (-not $trig -and $disp -eq 'unclear') { $trig = 'unspecified' }
                    if (-not $script:Vocab.dispositions.PSObject.Properties[$disp]) { $errs.Add("$set $rid`: bad disposition '$disp'") }
                    if (-not $script:Vocab.triggers.PSObject.Properties[$trig]) { $errs.Add("$set $rid`: bad trigger '$trig'") }
                    $mo = Get-IhpProp $ret 'months'
                    if ($null -ne $mo -and $mo -isnot [int] -and $mo -isnot [long] -and $mo -isnot [double]) { $errs.Add("$set $rid`: months must be a number") }
                    if ($disp -eq 'retain' -and $null -ne $mo) { $warn.Add("$set $rid`: retain with months") }
                    if ($disp -eq 'dispose' -and $null -eq $mo -and $trig -in @('unspecified', 'none', 'from-creation')) { $warn.Add("$set $rid`: dispose with no months and trigger '$trig'") }
                    $rt = Get-IhpProp $r 'ruleType'
                    if ($rt -and -not $script:Vocab.ruleTypes.PSObject.Properties[$rt]) { $errs.Add("$set $rid`: bad ruleType '$rt'") }
                    $oc = Get-IhpProp $r 'outcome'
                    if ($oc -and $oc -notin @('grant', 'refusal')) { $errs.Add("$set $rid`: bad outcome '$oc'") }
                    $ql = Get-IhpProp $ret 'qualifier'
                    if ($ql -and -not $script:Vocab.qualifiers.PSObject.Properties[$ql]) { $errs.Add("$set $rid`: bad qualifier '$ql'") }
                }
                foreach ($fm in @(Get-IhpProp $r 'forms' @())) { if (-not $script:Vocab.forms.PSObject.Properties[$fm]) { $errs.Add("$set $rid`: unknown form '$fm'") } }
                $pid_ = Get-IhpProp $r 'planId'; if ($pid_ -and $pid_ -notin $planIds) { $errs.Add("$set $rid`: unknown planId '$pid_'") }
                $src = Get-IhpProp $r 'source'
                if (-not $src) { if (-not ($isBase -and $srcIds.Count -eq 0)) { $warn.Add("$set $rid`: no source") } } elseif ((Get-IhpProp $src 'ref') -notin $srcIds) { $errs.Add("$set $rid`: unknown source ref '$(Get-IhpProp $src 'ref')'") }
                if ($set -eq 'rules') {
                    $ns = @(Get-IhpProp $r 'nodes' @())
                    if ($ns.Count -eq 0) { $errs.Add("rules $rid`: no nodes (use unmappedRules)") }
                    foreach ($m in $ns) {
                        if (-not $script:NodeById.ContainsKey($m.node)) { $errs.Add("rules $rid`: unknown node '$($m.node)'") }
                        elseif ($script:NodeById[$m.node].kind -ne 'leaf' -and -not $isBase) { $warn.Add("rules $rid`: node '$($m.node)' is a group") }
                        if (-not $script:Vocab.relations.PSObject.Properties[(Get-IhpProp $m 'relation' 'exact')]) { $errs.Add("rules $rid`: bad relation '$(Get-IhpProp $m 'relation' '')'") }
                    }
                } else {
                    if (-not (Get-IhpProp $r 'suggestedNode')) { $warn.Add("unmappedRules $rid`: no suggestedNode") }
                }
            }
        }
        foreach ($m in @(Get-IhpProp $c 'missing' @())) {
            if (-not $script:Vocab.missingKinds.PSObject.Properties[(Get-IhpProp $m 'kind' '')]) { $errs.Add("missing $($m.id): bad kind '$(Get-IhpProp $m 'kind' '')'") }
            $ns = Get-IhpProp $m 'nextStep'
            if ($ns -and -not $script:Vocab.nextSteps.PSObject.Properties[$ns]) { $errs.Add("missing $($m.id): bad nextStep '$ns'") }
            foreach ($fm in @(Get-IhpProp $m 'forms' @())) { if (-not $script:Vocab.forms.PSObject.Properties[$fm]) { $errs.Add("missing $($m.id): unknown form '$fm'") } }
            $ct = Get-IhpProp $m 'contact'
            if ($ct -and (Get-IhpProp $ct 'address' '') -match '^[a-zåäö]+\.[a-zåäö]+@' -and (Get-IhpProp $ct 'address' '') -notmatch '(?i)(registrator|kommun|info|kontakt|namnd|nämnd|forvaltning|förvaltning|utbildning|bildning|barn|skola|forskola|arkiv|diarie|kansli|dataskydd|kultur|fritid|stod|stöd|webb|servicecenter|vux)') { $warn.Add("missing $($m.id): contact address looks personal ('$($ct.address)')") }
        }
        $cov = Get-IhpProp $c 'coverage' ''
        if (-not $isBase -and $cov -notin @('complete', 'partial', 'none')) { $errs.Add("coverage must be complete|partial|none (is '$cov')") }
        $col = if ($errs.Count -gt 0) { 'Red' } elseif ($warn.Count -gt 0) { 'Yellow' } else { 'Green' }
        W ('{0}: {1} errors, {2} warnings' -f $f.Name, $errs.Count, $warn.Count) $col
        foreach ($e in $errs) { W "   ERROR   $e" 'Red' }
        foreach ($w in ($warn | Select-Object -First 25)) { W "   warning $w" 'Yellow' }
        if ($warn.Count -gt 25) { W ("   ... {0} more warnings" -f ($warn.Count - 25)) 'Yellow' }
        if ($errs.Count -gt 0) { $bad++ }
    }
    if ($bad -gt 0) { exit 1 }
}

# ---------------------------------------------------------------- report
function Get-OutreachLog {
    $p = Join-Path $script:IhpRoot 'outreach.json'
    if (-not (Test-Path -LiteralPath $p)) { return @() }
    $raw = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8).Trim()
    if ($raw -eq '' -or $raw -eq '[]') { return @() }
    $parsed = ConvertFrom-Json -InputObject $raw
    $out = @()
    foreach ($entry in $parsed) { $out += $entry }
    return $out
}

function Get-RequestText($c, $m) {
    $req = Get-IhpProp $m 'request'
    if ($req) { return ([string]$req -replace '\s+', ' ') }
    $plans = @(Get-IhpProp $c 'plans' @())
    $plan = $plans | Where-Object { (Get-IhpProp $_ 'status') -in @('not-public', 'draft-only', 'text-public') } | Select-Object -First 1
    if (-not $plan) { $plan = $plans | Select-Object -First 1 }
    $pname = $(if ($plan) { Get-IhpProp $plan 'name' 'gällande informationshanteringsplan' } else { 'gällande informationshanteringsplan' })
    $body = $(if ($plan) { Get-IhpProp $plan 'body' 'utbildningsnämnden' } else { 'utbildningsnämnden' })
    $date = $(if ($plan) { Get-IhpProp $plan 'decidedDate' '' } else { '' })
    $forms = ((@(Get-IhpProp $m 'forms' @()) | ForEach-Object { Get-FormName $_ }) -join ', ')
    switch (Get-IhpProp $m 'kind' '') {
        'plan-not-public' { return "Jag begär att få ta del av $pname för $body (gällande version och tidigare versioner), inklusive beslutet som antog planen och eventuella delegationsbeslut. Planen är en allmän handling; skicka den gärna elektroniskt." }
        'newer-version-possible' { return "Finns det en nyare version av $pname än den som beslutades ${date}? Om ja, jag begär att få ta del av den och av beslutet." }
        'delegated-plan-unpublished' { return "Jag begär att få ta del av den plan som $pname hänvisar till (kommunövergripande plan eller gallringsbeslut), eftersom den styr hur handlingar hanteras för skolverksamheten." }
        'form-not-covered' { return "Vilka handlingar och gallringsfrister gäller för $forms enligt gällande plan? Jag begär ut de delar av planen som avser dessa verksamheter." }
        'rows-not-extracted' { return "Vilka handlingar och gallringsfrister gäller för $forms enligt gällande plan? Jag begär ut de delar av planen som avser dessa verksamheter." }
        'text-unreadable' { return "Jag begär sökbara kopior (textbehandlade) av de inskannade handlingar som berör planen eller dess ändringar." }
        'rules-vague' { return "Hur definieras 'vid inaktualitet' i $pname, och finns det vägledning om när handlingar blir inaktuella? Jag begär ut sådan vägledning." }
        'plan-draft-only' { return "Jag begär ut den antagna, rensade versionen av $pname. Den publicerade filen är en ändringsmarkerad version eller ett utkast." }
        default { return '' }
    }
}

function Get-ContactEntries($c, $openContactItems, $mainAddr) {
    $mergeKinds = @('plan-not-public', 'form-not-covered', 'rows-not-extracted')
    $groups = @($openContactItems | Group-Object { ([string](Get-IhpProp (Get-IhpProp $_.M 'contact') 'address' '')) + '|' + $(if ($_.M.kind -in $mergeKinds) { $_.M.kind } else { $_.M.id }) })
    $entries = @()
    foreach ($g in $groups) {
        $e = @($g.Group)
        $m = $e[0].M
        $ct = Get-IhpProp $m 'contact'
        $addr = [string](Get-IhpProp $ct 'address' '')
        $formKeys = @($e | ForEach-Object { @(Get-IhpProp $_.M 'forms' @()) } | Select-Object -Unique)
        $forms = ((@($formKeys) | ForEach-Object { Get-FormName $_ }) -join ', ')
        if (-not $forms) { $forms = '(no school form stated)' }
        $reqs = @($e | ForEach-Object { Get-RequestText $c $_.M } | Where-Object { $_ } | Sort-Object { $_.Length })
        $rt = $(if ($reqs.Count -gt 0) { $reqs[0] } else { '' })
        $rt = ($rt -replace '^\s*Begäran om allmän handling:\s*', '' -replace '^\s*Fråga\s+', 'Fråga ')
        if ($rt -and $e.Count -gt 1 -and $formKeys.Count -gt 1) { $rt += " Det gäller samtliga skolformer: $forms." }
        $detail = ([string](Get-IhpProp $m 'detail' '') -replace '\s+', ' ')
        if ($detail.Length -gt 600) { $detail = $detail.Substring(0, 600) + '…' }
        $entries += [pscustomobject]@{
            Address = $(if ($addr) { $addr } else { $mainAddr }); Named = [bool]$addr; Role = [string](Get-IhpProp $ct 'role' ''); Kind = $m.kind
            Ids = @($e | ForEach-Object { $_.M.id }); Forms = $forms; FormCount = $formKeys.Count; Detail = $detail; Request = $rt; Items = $e
        }
    }
    return $entries
}

function Invoke-Report {
    $outDir = $(if ($OutDir) { $OutDir } else { Join-Path $script:IhpRoot 'reports' })
    New-Item -ItemType Directory -Force $outDir | Out-Null
    $log = @(Get-OutreachLog)
    $files = @(Get-IhpCityFiles)
    if ($City) { $files = @($files | Where-Object { $_.BaseName -like "*$($City.ToLowerInvariant())*" }) }
    $today = (Get-Date).ToString('yyyy-MM-dd')
    $cities = @()
    foreach ($f in $files) {
        $c = Read-IhpJson $f.FullName
        $items = @()
        foreach ($m in @(Get-IhpProp $c 'missing' @())) {
            $ns = Get-IhpProp $m 'nextStep' ''
            $group = $(if ($ns -in @('contact-city', 'ask-archive')) { 'contact' } elseif ($ns -eq 'none') { 'none' } else { 'search' })
            $lg = $log | Where-Object { $_.city -eq $c.municipality.code -and $_.itemId -eq $m.id } | Select-Object -First 1
            $st = $(if ($lg) { Get-IhpProp $lg 'status' '' } else { '' })
            $items += [pscustomobject]@{ M = $m; Group = $group; Outreach = $lg; OutStatus = $st }
        }
        $open = @($items | Where-Object { $_.OutStatus -notin @('answered', 'closed') })
        $mainAddr = ($items | ForEach-Object { Get-IhpProp (Get-IhpProp $_.M 'contact') 'address' } | Where-Object { $_ } | Group-Object | Sort-Object Count -Descending | Select-Object -First 1).Name
        $contactItems = @($open | Where-Object { $_.Group -eq 'contact' })
        $entries = @(Get-ContactEntries $c $contactItems $mainAddr)
        $cities += [pscustomobject]@{ C = $c; Items = $items; Open = $open; MainAddr = $mainAddr; Entries = $entries
            Emails = @($entries | Group-Object Address).Count
            Questions = (@($entries | Group-Object Address | ForEach-Object { @($_.Group | ForEach-Object { $_.Request } | Where-Object { $_ } | Select-Object -Unique).Count } ) | Measure-Object -Sum).Sum
            Contact = $contactItems; Search = @($open | Where-Object { $_.Group -eq 'search' }); None = @($open | Where-Object { $_.Group -eq 'none' }) }
    }
    $cities = @($cities | Sort-Object { - $_.Emails }, { - $_.Entries.Count }, { $_.C.municipality.name })

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('# Missing data report')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("Generated $today from the city files in research/municipal-plans. $($cities.Count) cities are in the reference; the rest are listed at the end.")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('How to use it, one city at a time:')
    [void]$sb.AppendLine('1. **E-mails to send**: one draft per contact address, with the questions numbered. The texts are drafts for you to read and sign before anything is sent; nothing is sent from here.')
    [void]$sb.AppendLine('2. **Search the homepage**: things that can be looked up on the municipality''s own site; the sources already found are listed first.')
    [void]$sb.AppendLine('3. Log what you did: `ihp.cmd outreach <city> -Item <ids> -Status sent|answered|no-answer|closed -Via e-post -Note "..."` (the exact command is under each e-mail). Answered and closed items leave the next report.')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('## Overview')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| City | Code | Coverage | Plan status | Rules | E-mails | Questions | Search items | Main contact |')
    [void]$sb.AppendLine('|---|---|---|---|---|---|---|---|---|')
    foreach ($x in $cities) {
        $c = $x.C
        $st = ((@(Get-IhpProp $c 'plans' @()) | ForEach-Object { Get-IhpProp $_ 'status' '?' }) | Select-Object -Unique) -join ', '
        [void]$sb.AppendLine(('| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} |' -f $c.municipality.name, $c.municipality.code, (Get-IhpProp $c 'coverage' '?'), $st, @(Get-IhpProp $c 'rules' @()).Count, $x.Emails, $x.Questions, $x.Search.Count, $x.MainAddr))
    }
    $csv = New-Object System.Collections.Generic.List[string]
    $csv.Add('city;code;item;group;kind;forms;nextStep;contactRole;contactAddress;outreachStatus;outreachDate;request;detail')
    function Q([string]$s) { '"' + (($s + '') -replace '"', '""' -replace '[\r\n]+', ' ') + '"' }
    foreach ($x in $cities) {
        $c = $x.C
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine("## $($c.municipality.name) ($($c.municipality.code), $(Get-IhpProp $c.municipality 'county' ''))")
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine("Coverage **$(Get-IhpProp $c 'coverage' '?')**, $(@(Get-IhpProp $c 'rules' @()).Count) rules. Plans:")
        foreach ($p in @(Get-IhpProp $c 'plans' @())) {
            [void]$sb.AppendLine(('- {0} ({1}): status {2}, decided {3}, last known {4}' -f (Get-IhpProp $p 'name' '?'), (Get-IhpProp $p 'body' '?'), (Get-IhpProp $p 'status' '?'), (Get-IhpProp $p 'decidedDate' '?'), (Get-IhpProp $p 'lastKnownPlanDate' '?')))
        }
        $uris = @(@(Get-IhpProp $c 'sources' @()) | Where-Object { Get-IhpProp $_ 'uri' } | Select-Object -First 6)
        if ($uris.Count -gt 0) { [void]$sb.AppendLine(''); [void]$sb.AppendLine('Sources already found (start here):'); foreach ($s in $uris) { [void]$sb.AppendLine(('- {0}: {1}' -f (Get-IhpProp $s 'title' ''), $s.uri)) } }

        if ($x.Entries.Count -gt 0) {
            [void]$sb.AppendLine('')
            [void]$sb.AppendLine("### E-mails to send ($($x.Emails))")
            $n = 0
            foreach ($ag in @($x.Entries | Group-Object Address)) {
                $n++
                $ents = @($ag.Group)
                $role = ($ents | Where-Object { $_.Role } | Select-Object -First 1).Role
                $allIds = @($ents | ForEach-Object { $_.Ids })
                $stat = @($ents | ForEach-Object { $_.Items } | Where-Object { $_.Outreach } | ForEach-Object { "$($_.OutStatus) $(Get-IhpProp $_.Outreach 'on' '')" } | Select-Object -Unique)
                [void]$sb.AppendLine('')
                [void]$sb.AppendLine("**E-mail $n to $($ag.Name)**$(if ($role) { ' (' + $role + ')' })$(if ($stat.Count -gt 0) { ' *[' + ($stat -join '; ') + ']*' })")
                [void]$sb.AppendLine('')
                [void]$sb.AppendLine('> Ämne: Begäran om allmän handling: informationshanteringsplan för skolverksamheten')
                [void]$sb.AppendLine('>')
                [void]$sb.AppendLine('> Hej,')
                [void]$sb.AppendLine('>')
                [void]$sb.AppendLine('> jag begär att få ta del av följande allmänna handlingar och uppgifter. Svar önskas elektroniskt.')
                [void]$sb.AppendLine('>')
                $q = 0
                foreach ($rq in @($ents | ForEach-Object { $_.Request } | Where-Object { $_ } | Select-Object -Unique)) {
                    $q++
                    [void]$sb.AppendLine("> $q. $rq")
                }
                [void]$sb.AppendLine('>')
                [void]$sb.AppendLine('> Vänliga hälsningar')
                [void]$sb.AppendLine('')
                [void]$sb.AppendLine("Log it: ``ihp.cmd outreach $($c.municipality.code) -Item $($allIds -join ',') -Status sent -Via e-post``")
            }
            [void]$sb.AppendLine('')
            [void]$sb.AppendLine("#### Questions in detail ($($x.Entries.Count))")
            $i = 0
            foreach ($en in $x.Entries) {
                $i++
                [void]$sb.AppendLine('')
                [void]$sb.AppendLine("$i. **$($en.Kind)**. Forms: $($en.Forms). Items: $($en.Ids -join ', ')$(if (-not $en.Named) { ' (no contact named in the material; main contact used)' })$(if (-not $en.Request) { ' **[no ready question: write one from the text below, or search first]**' })")
                [void]$sb.AppendLine("   - Missing: $($en.Detail)")
                foreach ($it in $en.Items) {
                    $mm = $it.M
                    $cc2 = Get-IhpProp $mm 'contact'
                    $dd = ([string](Get-IhpProp $mm 'detail' '') -replace '\s+', ' ')
                    if ($dd.Length -gt 700) { $dd = $dd.Substring(0, 700) + '…' }
                    $fm = ((@(Get-IhpProp $mm 'forms' @()) | ForEach-Object { Get-FormName $_ }) -join ', ')
                    $csv.Add(((@($c.municipality.name, $c.municipality.code, $mm.id, 'Contact', $mm.kind, $fm, (Get-IhpProp $mm 'nextStep' ''), (Get-IhpProp $cc2 'role' ''), $(if (Get-IhpProp $cc2 'address' '') { $cc2.address } else { $x.MainAddr }),
                        $it.OutStatus, (Get-IhpProp $it.Outreach 'on' ''), (Get-RequestText $c $mm), $dd) | ForEach-Object { Q $_ }) -join ';'))
                }
            }
        }
        foreach ($grp in @(@('Search the homepage', $x.Search), @('No action needed', $x.None))) {
            $list = @($grp[1])
            if ($list.Count -eq 0) { continue }
            [void]$sb.AppendLine('')
            [void]$sb.AppendLine("### $($grp[0]) ($($list.Count))")
            $i = 0
            foreach ($it in $list) {
                $i++
                $m = $it.M
                $forms = ((@(Get-IhpProp $m 'forms' @()) | ForEach-Object { Get-FormName $_ }) -join ', ')
                $detail = ([string](Get-IhpProp $m 'detail' '') -replace '\s+', ' ')
                if ($detail.Length -gt 600) { $detail = $detail.Substring(0, 600) + '…' }
                $mark = $(if ($it.Outreach) { " *[$($it.OutStatus) $(Get-IhpProp $it.Outreach 'on' '')]*" } else { '' })
                [void]$sb.AppendLine('')
                [void]$sb.AppendLine("$i. **$($m.kind)** (item $($m.id))$mark. Forms: $forms")
                [void]$sb.AppendLine("   - Missing: $detail")
                $csv.Add(((@($c.municipality.name, $c.municipality.code, $m.id, $grp[0], $m.kind, $forms, (Get-IhpProp $m 'nextStep' ''), '', '', $it.OutStatus, (Get-IhpProp $it.Outreach 'on' ''), '', $detail) | ForEach-Object { Q $_ }) -join ';'))
            }
        }
    }
    # cities not in the reference yet
    $regPath = Join-Path $script:IhpRoot 'registry.json'
    if ((Test-Path -LiteralPath $regPath) -and -not $City) {
        $reg = Read-IhpJson $regPath
        $others = @($reg.cities | Where-Object { $_.status -ne 'in-reference' })
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine('## Cities not yet in the reference')
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine("$($others.Count) of $(@($reg.cities).Count) kommuner. $(@($others | Where-Object { $_.status -eq 'earlier-research' }).Count) have an earlier preschool-only report in the research folders (the plan text is often already downloaded, so extraction can start there); $(@($others | Where-Object { $_.status -eq 'not-researched' }).Count) have never been looked at. Full list with folders: ``cities-not-in-reference.csv``.")
        $cc = New-Object System.Collections.Generic.List[string]
        $cc.Add('code;name;county;status;earlierFolder;earlierReport;note')
        foreach ($o in $others) {
            $er = Get-IhpProp $o 'earlierResearch'
            $cc.Add(((@($o.code, $o.name, (Get-IhpProp $o 'county' ''), $o.status, (Get-IhpProp $er 'folder' ''), (Get-IhpProp $er 'report' ''), (Get-IhpProp $o 'note' '')) | ForEach-Object { Q $_ }) -join ';'))
        }
        $utf8bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllLines((Join-Path $outDir 'cities-not-in-reference.csv'), $cc, $utf8bom)
    }
    $utf8bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText((Join-Path $outDir 'missing-data-report.md'), $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
    [System.IO.File]::WriteAllLines((Join-Path $outDir 'missing-data-actions.csv'), $csv, $utf8bom)
    W ('Report written to {0}' -f $outDir) 'Green'
    W '  missing-data-report.md            read this, one city at a time'
    W '  missing-data-actions.csv          one row per action (open in Excel; filter on city and group)'
    W '  cities-not-in-reference.csv       the remaining kommuner and their earlier research'
    W ''
    W 'Open actions per city:' 'Cyan'
    foreach ($x in $cities) { W ('  {0,-14} e-mails {1,2}   questions {2,2}   search items {3,2}   no action {4,2}' -f $x.C.municipality.name, $x.Emails, $x.Questions, $x.Search.Count, $x.None.Count) }
}

function Invoke-Outreach {
    if (-not $City -or -not $Item -or -not $Status) { throw 'Usage: ihp.ps1 outreach <city> -Item <missing id> -Status sent|answered|no-answer|closed [-Via e-post|telefon|webb] [-Note "..."]' }
    if ($Status -notin @('sent', 'answered', 'no-answer', 'closed')) { throw "Status must be sent, answered, no-answer or closed." }
    $c = Find-IhpHolder $City
    $code = $c.municipality.code
    $ids = @($Item -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    foreach ($id in $ids) { if (-not (@(Get-IhpProp $c 'missing' @()) | Where-Object { $_.id -eq $id })) { throw "City $code has no missing item '$id'." } }
    $log = @(Get-OutreachLog | Where-Object { -not ($_.city -eq $code -and $_.itemId -in $ids) })
    foreach ($id in $ids) {
        $entry = [ordered]@{ city = $code; itemId = $id; on = (Get-Date).ToString('yyyy-MM-dd'); channel = $Via; status = $Status; note = $Note }
        $log += [pscustomobject]$entry
    }
    $json = ConvertTo-Json -InputObject @($log) -Depth 5
    [System.IO.File]::WriteAllText((Join-Path $script:IhpRoot 'outreach.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
    W ('Logged: {0} items {1} = {2} ({3})' -f $c.municipality.name, ($ids -join ','), $Status, $(if ($Via) { $Via } else { 'no channel' })) 'Green'
}

switch ($Command.ToLowerInvariant()) {
    'report' { Invoke-Report }
    'outreach' { Invoke-Outreach }
    'list' { Invoke-List }
    'city' { Invoke-City }
    'compare' { Invoke-Compare }
    'coverage' { Invoke-Coverage }
    'missing' { Invoke-Missing }
    'gaps' { Invoke-Gaps }
    'validate' { Invoke-Validate }
    default {
        W 'ihp.ps1 - viewer for the municipal retention plan reference' 'Cyan'
        W '  list                                        all cities, coverage, plan dates, rule and missing counts'
        W '  city <city> [-Form f] [-Full]               one city''s plans, sources (with dates), rules and what is missing'
        W '  compare -With a,b [-Base fortdocs] [-Form f] [-OnlyDiffs] [-NoKlassa]'
        W '                                              base plan against cities, with the KLASSA code per node'
        W '  coverage                                    nodes with a rule per school form and city'
        W '  missing [-City x] [-NextStep contact-city]  what is missing and what to do about it'
        W '  gaps -With a,b [-Form f]                    nodes where cities split or disagree (candidates to refine the structure)'
        W '  validate [-Path file]                       check city files against the vocabulary and the structure'
        W '  report [<city>] [-OutDir dir]               missing-data report (Markdown + CSV): what to search for and whom to contact, with request texts'
        W '  outreach <city> -Item <id> -Status sent|answered|no-answer|closed [-Via ...] [-Note ...]   log what you did; answered/closed items leave the report'
        W 'Forms: ' 'DarkGray' -NoNewLine; W (($script:Vocab.forms.PSObject.Properties.Name) -join ', ') 'DarkGray'
    }
}
