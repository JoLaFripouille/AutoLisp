# API compacte JHRCADRE, schéma 1

Transport : commande **JHRCADREAPI**, suivie du chemin d’un fichier JSON.
Réponse : même chemin + `.response.json`. Alternative AutoLISP :
`(jhr:cadre "JSON")`. Appels séquentiels, sans interface, avec transaction .NET.

Python : `Tools/agent_client.py requete.json --tool frame --drawing CHEMIN_DWG`.
Ce transport refuse un autre dessin actif et ajoute son nom exact à la requête.
Ne pas appeler la commande pendant qu’un éditeur/une autre commande est ouvert.

Réponse : `{"ok":true,"result":...}` ou `{"ok":false,"error":"..."}`.
Ne pas relancer une écriture après un délai d’attente sans lire l’état actuel.

## Bibliothèque locale

```json
{"op":"catalogue","family":"Tubes","query":"40x40","limit":20}
```

Familles : `Tubes`, `Laminés`, `Jansen`. Résultats paginés (`offset`, `limit`
1–200), id stable, nom, largeur et profondeur. Aucun parcours du dessin requis.
Une section doit être importée avant de créer un nouveau cadre :

```json
{"op":"importTemplate","path":"CHEMIN_GABARIT.dwt"}
{"op":"importProfile","path":"SECTION_ISOLEE.dwg"}
{"op":"discover","path":"DOSSIER_JANSEN"}
```

`discover` indexe les fichiers pour l’interface ; utiliser `importProfile` pour
ajouter la section choisie au catalogue chargé. Les imports écrivent seulement
le cache local, jamais le fichier de bibliothèque ni le dessin.

## Dessin

```json
{"op":"list","offset":0,"limit":20}
{"op":"get","handle":"HANDLE_CADRE"}
```

`get` renvoie une révision, les handles des vues et les paramètres, sans détailler
la géométrie des profils. Conserver `revision` pour toute écriture suivante.

```json
{
 "op":"create","drawing":"CHEMIN_DWG_EXACT","position":[0,0,0],
 "settings":{
  "tool":"Tubes","profileId":"ID_DU_CATALOGUE",
  "width":1200,"height":2000,"miter":true,
  "mullions":[600],"transoms":[1000],
  "cuts":[{"id":"A","vertical":true,"station":300,"reverse":false},
          {"id":"B","vertical":false,"station":500,"reverse":false}]
 }
}
```

Outils : `Tubes`, `Joinery`, `Rolled`, `SheetDoor`. Autres paramètres :
`turns` (quarts de tour), `mirror`, `labelHeight` (mm modèle), `gap`, `sheet`,
`twoSheets`, `leafProfileId` (nécessaire pour la porte tôlée).
Les dimensions et stations sont locales, en mm ; la position est XYZ monde.

```json
{"op":"check","handle":"HANDLE","settings":{"width":1500}}
{"op":"update","drawing":"CHEMIN_DWG_EXACT","handle":"HANDLE",
 "expectedRevision":"REVISION_RETOURNEE","settings":{"width":1500}}
```

`check` calcule sans écrire. `update` exige une révision et conserve les placements
des vues. La révision tient compte des paramètres, de la définition, du placement
et du calque du cadre. Les copies ne modifient pas l’original. Un changement
de type/section s’effectue par patch, puis recalcule l’élévation et les coupes.
`profile`/`leafProfile`/`schema` sont protégés ; utiliser les ids de catalogue.
Les contours sont stockés dans le cadre : pas d’écriture dans le gabarit.

Limites : voir [CADRES.md](CADRES.md), notamment cadres rectangulaires,
menuiserie sans ouvrant automatique et refus des coupes de coins à 45°.
