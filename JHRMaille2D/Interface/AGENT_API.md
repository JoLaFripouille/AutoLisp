# API courte pour agent IA — JHR Panneaux 1.4

API native synchrone : `PanelApi.Execute(json)` (contexte principal AutoCAD),
ou `(jhr:panneau "JSON")`. Le pont LISP transmet une chaîne ; il ne calcule pas la géométrie.
Sans pont : commande `JHRPANNEAUAPI`, chemin d’un fichier JSON ; résultat dans
`chemin.json.response.json`. Le client Python `Tools/agent_client.py` fait cet échange.
Il utilise l’AutoCAD déjà ouvert, refuse une commande en cours et ne change pas de dessin.
Il nécessite pywin32. La DLL doit être chargée ; aucune fenêtre de réglage n’est nécessaire.

## Travail économique

1. Identifier le DWG actif. Toujours renseigner `drawing` avec son chemin exact.
2. `list` paginé pour les handles, puis `get` uniquement sur le panneau visé.
3. `check` avec le petit patch prévu : aucun objet DWG modifié.
4. `update` avec `expectedRevision` obtenue par `get`, puis relire ce panneau.
5. Grouper les modifications autorisées dans un seul `batch` (20 maximum, atomique).

Pas de capture écran, pas d’inventaire géométrique global, pas de lecture des sources.
Les réponses sont `{ "ok": true, "result": ... }` ou `{ "ok": false, "error": ..., "type": ... }`.
Un échec refuse/annule l’opération ; ne pas le traiter comme un résultat partiel.
Fermer la fenêtre d’édition avant `create`/`update`/`batch` en écriture.
Les paramètres sont en mm, les angles en degrés, les handles en hexadécimal.

## Requêtes essentielles

```json
{"op":"list","drawing":"C:/Plans/Plan.dwg","limit":20,"offset":0}
{"op":"get","drawing":"C:/Plans/Plan.dwg","handle":"AB12"}
{"op":"check","drawing":"C:/Plans/Plan.dwg","handle":"AB12","settings":{"tool":2}}
{"op":"update","drawing":"C:/Plans/Plan.dwg","handle":"AB12","expectedRevision":"revision renvoyée","settings":{"tool":2,"expanded":{"a":40,"b":20,"wire":2}}}
```

Un patch conserve les champs et les réglages des autres outils. Les clés inconnues sont refusées.
`get` reste compact ; ajouter `contours:true` seulement si les sommets sont nécessaires.
La révision détecte un changement des réglages, du placement, de définition ou de calque.
`expectedEngine` est aussi disponible ; il ne remplace pas la révision.
Une mise à jour conserve le handle, le placement et les propriétés de la référence.
Les copies restent indépendantes. L’ancienne définition inutilisée n’est pas purgée automatiquement.

Créer depuis des polylignes existantes :

```json
{"op":"create","drawing":"C:/Plans/Plan.dwg","innerHandle":"10A","outerHandle":"10B","settings":{"tool":1,"square":{"a":50,"wire":3,"pattern":2},"squareMask":1}}
```

Un contour unique : omettre outerHandle, choisir tool=1 et square.pattern=0.
Les contours doivent être des polylignes fermées, simples, coplanaires XY.
Le contour extérieur entoure strictement l’intérieur. Les arcs par bulge sont acceptés.
Pour un trapèze, ajuster l’angle de trame si souhaité ; les sources ne sont pas déplacées.

Créer sans sélection : `data.inner` et `data.outer` = tableaux `{x,y,bulge:0}` locaux,
et `position:[x,y,z]`. Les contours sont stockés dans le panneau : supprimer ensuite les
polylignes sources ne détruit pas ses réglages. Les modifications de ces polylignes ne se
répercutent pas automatiquement ; utiliser « Resélectionner les contours » dans la fenêtre.

```json
{"op":"batch","drawing":"C:/Plans/Plan.dwg","requests":[{"op":"update","handle":"AB12","settings":{"tool":2}},{"op":"update","handle":"AB13","settings":{"tool":2}}]}
```

Pas de batch imbriqué. Si une requête échoue, toutes les écritures du lot sont annulées.
Pour préserver le travail entre lecture et lot, renseigner expectedRevision dans chaque update.

## Outils et paramètres

| tool | Réglages |
|---|---|
| 0 | mesh : width/height diagonales centre à centre, wire câble, lace laçage, gap retrait, angle, woven, autoCorners, sleeveLength/sleeveHeight, stops, productReference |
| 1 | square : a ouverture libre carrée ; wire diamètre ; gap ; angle ; pattern 0=limite unique/intérieure, 1=deux contours visibles, 2=partie sous cadre cachée |
| 2 | expanded : a=ML, b=MW, wire largeur projetée lanière, thickness matière, gap, angle, margin plat (0=auto), pattern 0=aplati, 1=relief |
| 3 | perforated : a trou Ø/côté, b entraxe, thickness, gap, margin bordure, angle, pattern 0=R/U, 1=R/T60, 2=C/U |

`squareMask` : 0=aucun, 1=uniquement sous les fils, 2=toute la surface du grillage.
Le mode 1 conserve les ouvertures transparentes, masque placé derrière les contours de fils.
Ne pas confondre diamètre de fil et épaisseur de trait : le grillage utilise deux bords fermés.
Pour une référence Jakob : patch `mesh.productReference`; ses dimensions/mode sont appliqués
automatiquement, en conservant les paramètres de montage. Chaîne vide = saisie libre.

`catalogue` : family, diameter, offset, limit (50 par défaut, maximum 200).
Exemple : `{"op":"catalogue","family":"Webnet sans douilles","diameter":1.5,"limit":10}`.
`help` renvoie la version, les valeurs par défaut et les opérations prises en charge.
Les limites géométriques refusent les panneaux trop denses ; diviser le panneau plutôt
que désactiver les garde-fous. Les modèles ne constituent pas un calcul de résistance.

Les anciens panneaux LISP peuvent être importés si leurs contours sources existent.
`legacyDefaults:true` signale les paramètres absents de l’ancien format : contrôler le retrait,
les accessoires et le montage avant de valider. Le premier update migre uniquement ce panneau.
Un panneau éclaté ou sans métadonnées ne peut pas être identifié par son nom seul.

## Conversion d’un panneau à contour unique vers un filet

L’interface demande le deuxième contour extérieur, en conservant l’intérieur.
L’API ne lance pas de sélection interactive : envoyer `outerHandle` (polyligne
extérieure du DWG actif) avec le patch `settings.tool: 0`. Ses sommets sont
ramenés dans le repère du panneau. `check` valide d’abord cette conversion.
Sans contour extérieur, la requête est refusée. Ne pas inventer un cadre.
