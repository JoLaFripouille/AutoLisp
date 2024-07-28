# Obtenir le chemin du répertoire du script PowerShell en cours d'exécution
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

# Définir le chemin du fichier LISP en utilisant le répertoire du script
$lispFilePath = Join-Path -Path $scriptDirectory -ChildPath "DevTole.lsp"

# Définir le chemin du fichier DCL en utilisant le répertoire du script
$dclFilePath = Join-Path -Path $scriptDirectory -ChildPath "DevTole.dcl"

# Échapper les barres obliques inverses pour AutoLISP
$escapedDclFilePath = $dclFilePath -replace "\\", "/"

# Lire le contenu du fichier LISP
$lispContent = Get-Content -Path $lispFilePath

# Rechercher la ligne à modifier ou ajouter une nouvelle ligne
$modifiedContent = @()
$lineToAdd = '  (setq dcl_path "' + $escapedDclFilePath + '")'
$variableFound = $false

foreach ($line in $lispContent) {
    if ($line -match "^\s{2}\(setq dcl_path ") {
        # Remplacer la ligne existante par la nouvelle valeur
        $modifiedContent += $lineToAdd
        $variableFound = $true
    } else {
        # Ajouter les autres lignes sans modification
        $modifiedContent += $line
    }
}

# Si la variable n'a pas été trouvée, l'ajouter à la fin du fichier
if (-not $variableFound) {
    $modifiedContent += $lineToAdd
}

# Enregistrer les modifications dans le fichier LISP
$modifiedContent | Set-Content -Path $lispFilePath

# Afficher la valeur de dcl_path dans la console
Write-Host "La variable dcl_path a été définie avec la valeur suivante : $escapedDclFilePath"

Write-Host "Le fichier LISP a été modifié et sauvegardé avec succès."
