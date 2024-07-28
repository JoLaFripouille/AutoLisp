# Obtenir le chemin du répertoire du script PowerShell en cours d'exécution
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

# Définir le chemin du fichier LISP en utilisant le répertoire du script
$lispFilePath = Join-Path -Path $scriptDirectory -ChildPath "example.lsp"

# Échapper les barres obliques inverses pour AutoLISP
$escapedLispFilePath = $lispFilePath -replace "\\", "\\"

# Lire le contenu du fichier LISP
$lispContent = Get-Content -Path $lispFilePath

# Rechercher la ligne à modifier ou ajouter une nouvelle ligne
$modifiedContent = @()
$lineToAdd = '(setq *new-variable* "' + $escapedLispFilePath + '")'
$variableFound = $false

foreach ($line in $lispContent) {
    if ($line -match "^\(setq \*new-variable\* ") {
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

# Afficher la valeur de new-variable dans la console
Write-Host "La variable new-variable a été définie avec la valeur suivante : $escapedLispFilePath"

Write-Host "Le fichier LISP a été modifié et sauvegardé avec succès."
