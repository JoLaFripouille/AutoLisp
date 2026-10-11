# Développement

AutoCAD / Advance Steel 2027, Windows x64, .NET 10. SDK : acmgd, acdbmgd, accoremgd
de l’AutoCAD local, jamais redistribués. Le catalogue Jakob est une ressource embarquée.

Entrées ciblées : PanelPlugin (commandes/double-clic), PanelEditor (aperçu temporaire),
PanelApi (JSON), PanelStore (transactions/métadonnées), PanelData (schema 1).
UI : MeshForm + PatternPage, réglages distincts par outil.
GeometryCompiled + GeometryRuntime contiennent les algorithmes mathématiques compilés en C#.
FastExpanded remplace le découpage du déployé par des opérations C# directes plus rapides.
GeometrySource/approved-algorithms.lsp est le snapshot historique de maintenance, pas un
script chargé par la DLL. Tools/translate_geometry.py régénère GeometryCompiled.cs.

PanelGeometry.Prepare utilise les offsets natifs sur le thread AutoCAD sans écrire au DWG.
Calculate est pure .NET, déporté sur un worker dans la fenêtre, avec annulation.
La fenêtre temporise les saisies de 300 ms et ignore tout résultat périmé. Apply réutilise
le calcul courant lorsqu’il correspond exactement aux paramètres ; Cancel restaure l’objet.
Les surbrillances/masques d’aperçu sont transitoires, aucune recoloration permanente du cadre.
Les appels API sont synchrones et courts ; ne pas les paralléliser dans le même AutoCAD.

Le bloc est une géométrie 2D ordinaire portable. L’Xrecord JHR_PANEL_V1 de la référence
stocke le JSON par fragments de 200 caractères, contours locaux inclus. Un update crée
une nouvelle définition unique et réaffecte uniquement la référence sélectionnée.
Le double-clic intercepte l’action native seulement pour un panneau identifié ; BEDIT
sur les autres blocs conserve son comportement. Aucun réglage DBLCLKEDIT permanent.

Compilation : `dotnet build JhrMailleUi.csproj -c Release`.
Tests interface : `dotnet run --project Tests/Tests.csproj -c Release -- dossier_sortie`.
Changer le nom de DLL lorsqu’elle est déjà chargée : AutoCAD ne décharge pas une assembly.
Après changement d’API/métadonnées, tester annulation, copie indépendante, UNDO, lot atomique,
et révision périmée dans un DWG de test. Ne jamais fermer un plan client non sauvegardé.
Autoload : paquet standard .bundle ; le redémarrage reste à contrôler séparément du NETLOAD.

## Cadres 1.5

`Frames/FrameModel.cs` : modèle indépendant d’AutoCAD, contours exacts à bulges,
orientation, élévation et intersections de coupes. `FrameDialog.cs` : aperçu
WinForms et lignes déplaçables. `ProfileLibrary.cs` : lecture des DWG/DWT en base
temporaire, extraction de visibilité, cache local et index Jansen à la demande.
`FrameStore.cs` : transactions, blocs de composant et vues séparées, Xrecord
JHR_FRAME_V1, propriété des vues, indépendance des copies. `FrameEditor.cs` et
`FrameApi.cs` : édition humaine et JSON compact. Le routeur de double-clic
reconnaît panneaux et cadres, avec la même garde contre BEDIT.

Le cache de profils ne doit pas être inclus dans un dépôt ou un paquet public.
La définition d’un cadre embarque les sections choisies sans chemin source.
Les sections de test proviennent d’un gabarit local fourni en argument ; les
tests `Tests/Frames` ne redistribuent pas les bibliothèques.
