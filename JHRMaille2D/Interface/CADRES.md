# JHRCADRE — première version 1.5.1

Créer une élévation de cadre rectangulaire et ses coupes horizontales/verticales,
en utilisant les sections réelles de votre bibliothèque locale. La commande est
incluse dans le même plugin .NET que JHRMAILLE.

## Utilisation

1. Lancer **JHRCADRE**.
2. Choisir **Cadres en tubes**, **Menuiserie acier**, **Profils laminés** ou **Portes tôlées**.
3. Sur un nouveau PC : **Importer le gabarit** pour les tubes/laminés, puis
   **Bibliothèque Jansen…** pour indiquer le dossier du fabricant.
4. Choisir le profil, ses dimensions hors tout, son orientation et les assemblages
   à 45° ou droits. Vérifier l’orientation de la section dans les coupes.
5. Ajouter éventuellement les positions des montants X et traverses Y :
   `600;1200`, en millimètres depuis le bas/gauche. Ce sont les axes des divisions.
6. Choisir **Coupe verticale** ou **Coupe horizontale**, puis cliquer dans
   l’élévation. Faire glisser une ligne existante pour la déplacer. Sa position
   exacte est aussi modifiable sous l’aperçu. **Inverser le regard** retourne la vue.
7. **Valider**, puis choisir le coin inférieur gauche dans le dessin.

L’élévation, les coupes et les repères sont créés en millimètres à **1:1**.
Les coupes verticales se placent à droite ; les horizontales en dessous.
Elles restent des blocs séparés, que vous pouvez déplacer pour organiser le plan.
Le repère A–A reste associé à sa coupe. La hauteur de ses textes se règle dans
**Repères de coupe** (millimètres modèle), en bas de la colonne de paramètres.

**JHRCADREEDIT**, ou double-clic sur le bloc principal, rouvre ses paramètres.
Valider remplace seulement ce cadre et ses vues associées. Le placement manuel
des vues est conservé. Une copie du cadre reçoit ses propres vues lors de sa
première modification. Annuler la fenêtre n’écrit rien dans le dessin.
Ne pas étirer/mettre à l’échelle le bloc avec les outils génériques : modifier
ses dimensions avec l’éditeur. Une référence hors échelle 1:1 est refusée.

## Bibliothèques et géométrie

Le gabarit est lu dans une base temporaire. Les états de visibilité des blocs
de tubes, cornières, tés et poutrelles sont extraits séparément ; les entités
invisibles et les axes sont écartés. Les dimensions obtenues sont comparées à
la désignation de l’état. Le fichier source n’est pas modifié.

La bibliothèque Jansen est indexée par noms de fichiers, puis chaque section
est chargée seulement lorsqu’elle est choisie. Les bibliothèques doivent être dessinées en millimètres : cette première version
ne convertit pas les unités des fichiers importés. Le DWG doit contenir une section
isolée, pas une planche de plusieurs assemblages. Les vues trop grandes ou
sans géométrie exploitable sont refusées. Vérifier la référence et sa fonction
dans le catalogue fabricant avant de choisir un dormant.

Les profils sont reproduits sans étirement. Lignes, arcs et polylignes gardent
leurs contours ; les ellipses sont discrétisées. Les montants opposés sont
réfléchis dans la largeur pour orienter la section vers l’intérieur du cadre.
Les coupes longitudinales utilisent les contours réels, en préservant les vides.
Les sections ouvertes qui ne permettent pas ce calcul sont signalées.

Le cache et le chemin de bibliothèque restent locaux, dans `%APPDATA%/JHR/Cadres`.
La géométrie des profils choisis est enregistrée dans les cadres, sans chemin
personnel, pour pouvoir rééditer un cadre existant sur un autre PC.
Le plugin public n’inclut ni votre gabarit ni les bibliothèques du fabricant.

Tous les objets du cadre sont **calque 0, couleur et épaisseur DuCalque**.
Les textes et lignes de repère sont séparés du bloc de composant réutilisable.
Le bloc principal contient ses dimensions dans son nom : `CADRE_…_1200x2000_…`.

## Périmètre de cette première version

- Cadres rectangulaires plans XY, avec un profil commun au pourtour.
- Divisions droites et coupes strictement verticales/horizontales.
- Menuiserie acier : cadre utilisant une section Jansen choisie. L’association
  dormant/ouvrant, les joints, feuillures, drainage et quincaillerie ne sont pas
  assemblés automatiquement.
- Porte tôlée : dormant, jeu, ossature du vantail et tôle sur une/deux faces.
  La tôle est représentée à l’extérieur de l’ossature. Les paumelles, serrure
  et seuil sont à compléter ; aucun sens d’ouverture n’est déduit.
- Une coupe traversant un angle à 45° est refusée : déplacer sa ligne hors
  de l’angle ou choisir des assemblages droits. Les coupes obliques ne sont
  pas encore calculées.
- Les coupes sont les contours des éléments traversés. Les éléments seulement
  vus derrière le plan de coupe, les hachures et les cotations ne sont pas ajoutés.
- Effacer le bloc principal ne supprime pas automatiquement ses vues séparées.
  Les supprimer explicitement si vous retirez tout l’ensemble.

## Agent IA

Lire [CADRES_AGENT_API.md](CADRES_AGENT_API.md). Les appels JSON permettent de
chercher les sections, lire les paramètres, vérifier et modifier un cadre sans
ouvrir l’interface. Les réponses sont compactes et les écritures contrôlent
le dessin et la révision. Le transport Python commun accepte `--tool frame`.
