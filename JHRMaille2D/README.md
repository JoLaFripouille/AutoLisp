# JHRMaille2D — filet inox détaillé en 2D

Commande AutoLISP pour représenter une maille en losanges avec ses douilles et son laçage autour d’un cadre. Deux polylignes fermées décrivent la face intérieure et la face extérieure du cadre ; leur géométrie sert à adapter les raccordements et les courbes de laçage.

## Installation et utilisation

1. Télécharger [JHR_Maille_2D.lsp](JHR_Maille_2D.lsp).
2. Dans AutoCAD pour Windows, charger le fichier avec `APPLOAD`, selon les règles de chargement de votre installation.
3. Travailler en millimètres, à taille réelle, dans l’espace objet. Dessiner deux **polylignes 2D fermées (LWPOLYLINE)** dans le plan XY, à la même élévation. Le contour extérieur doit entourer strictement le contour intérieur sans le croiser ni le toucher.
4. Lancer `JHRMAILLEPARAM` si les réglages par défaut doivent être adaptés.
5. Choisir le calque du panneau, puis lancer `JHRMAILLE`.
6. Sélectionner d’abord le contour **intérieur** du cadre, puis son contour **extérieur**.

La commande crée un bloc `JHR_MAILLE_2D_1`, puis incrémente le nom pour les panneaux suivants. La référence de bloc est placée sur le calque courant ; son contenu et les composants réutilisés sont sur le calque **0**. Les contours sources sont conservés. Avec l’annulation AutoCAD activée, l’opération est regroupée pour être annulée en une étape.

## Réglages

Les paramètres sont mémorisés pour la session AutoCAD en cours.

| Paramètre de `JHRMAILLEPARAM` | Valeur initiale | Signification |
|---|---:|---|
| Petite diagonale | 60 mm | Distance de centre à centre |
| Grande diagonale | 104 mm | Distance de centre à centre |
| Diamètre du câble de maille | 1,5 mm | Largeur géométrique des traits de câble |
| Diamètre du câble de laçage | 2 mm | Largeur géométrique du laçage |
| Retrait filet / face intérieure | 20 mm | Distance entre le filet et l’intérieur du cadre |
| Longueur de douille dessinée | 5,5 mm | Représentation de la douille |
| Largeur de douille dessinée | 5,7 mm | Représentation de la douille |
| Direction de la grande diagonale | 0° | Orientation de la maille |
| Pas des petites butées de rive | 12 | Nombre de raccords entre occurrences, pas une distance en mm |
| Raccords d angle | Auto | Transition automatique entre une rive latérale et une rive haute ou basse |

Ces valeurs sont des réglages de dessin, à remplacer par les dimensions du filet et des accessoires retenus pour le projet. La diagonale entre centres n’est pas l’ouverture libre entre câbles.

## Représentation obtenue

- Losanges complets, douilles aux nœuds et raccords de rive.
- Laçage supérieur et inférieur passant dans les boucles.
- Boucles avec détails de sertissage sur les rives latérales.
- Courbes de raccordement adaptées à l’écartement des deux contours.
- Petits composants définis comme blocs réutilisables dans le dessin.

Le panneau généré est un **bloc statique**. Pour modifier sa maille ou ses diamètres, régler les paramètres et régénérer un panneau à partir des contours conservés.

## Formes et limites

Le calcul utilise les contours sélectionnés, pas seulement leur boîte rectangulaire : les contours inclinés et trapézoïdaux sont pris en compte. Contrôler visuellement les losanges et les raccords, particulièrement dans les angles aigus. Les segments courbes sont échantillonnés pour le calcul.

Un retrait qui divise le filet en plusieurs panneaux est refusé : traiter chaque panneau séparément. Les contours croisés, un cadre trop petit ou un retrait impossible sont également refusés. La limite de calcul initiale est de 15 000 nœuds pour éviter une génération excessive.

Le fichier utilise Visual LISP / ActiveX et vise **AutoCAD pour Windows**. Une utilisation sur ARES, AutoCAD pour Mac ou un autre moteur LISP doit être vérifiée séparément.

Les douilles, œillets et butées sont des **représentations graphiques**, pas des gabarits de fabrication ou de sertissage. Certains noms techniques internes historiques contiennent `WEBNET` ; ils ne constituent pas une certification ni une identification du fournisseur du projet.

## Raccords automatiques aux angles

Dans `JHRMAILLEPARAM`, le choix **Raccords d angle [Auto/Classique]** vaut **Auto** par défaut. L'utilisation reste la même : sélectionner le contour intérieur fermé, puis le contour extérieur fermé avec `JHRMAILLE`.

Le script recherche un seul angle convexe de plus de 30 degrés entre deux attaches consécutives, lorsque la rive passe d'un œillet latéral à une boucle supérieure ou inférieure. Il remplace alors le retour supplémentaire au coin par une transition du laçage d'une face du cadre à l'autre. La largeur locale mesurée entre les deux contours détermine le passage et la petite portion visible sur la face horizontale ; les parties derrière le cadre sont masquées dans la représentation 2D.

Les losanges et les accessoires restent identiques. Les rives droites, les courbes échantillonnées, les angles rentrants et les transitions entre attaches du même type conservent leur raccord classique. Si le passage calculé ne tient pas dans la section du cadre, le script conserve également le retour classique. Le message final indique le nombre de transitions d'angle effectivement appliquées.

Pour revenir au dessin précédent, choisir **Classique** puis générer un nouveau panneau. Recharger le LISP avec `APPLOAD` après une mise à jour. Les blocs déjà créés sont statiques : ils restent inchangés ; recréer leur remplissage à partir des deux contours conservés pour utiliser le nouveau raccord. Aucun remplacement de panneau existant n'est effectué automatiquement.

Ce raccord est une représentation graphique du laçage ; il ne modifie pas le mode de fixation ni les prescriptions de sertissage du fabricant.

Contrôles natifs du 2 octobre 2026 : cadres de 20, 50 et 80 mm, trapèze et cadre tourné, quatre transitions par cas ; contour courbe et mode Classique, zéro transition. Les rectangles conservent leurs 213 douilles, 24 œillets et 18 boucles. Les câbles de maille et les accessoires sont géométriquement identiques entre Auto et Classique. Un essai de la commande complète valide aussi l'offset et la conservation exacte des deux contours. Exemple enregistré : `Exemple_Maille_2D_angles_automatiques.dwg` ; comparaison visuelle : `Comparaison_angles_automatiques.png`.
