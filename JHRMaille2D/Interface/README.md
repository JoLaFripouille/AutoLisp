# JHR Panneaux et Cadres 1.5.1 — JHRMAILLE en plugin .NET

Créer et modifier des panneaux 2D : **filet inox losange**, **grillage serrurier carré**,
**métal déployé** et **tôle perforée**. Le calcul, les transactions et la fenêtre fonctionnent
entièrement en .NET, sans charger de moteur de dessin LISP.

## Cadres — nouveau dans 1.5.1

**JHRCADRE** : cadres rectangulaires en tubes, sections Jansen, laminés et portes tôlées.
Définir les lignes de coupe directement dans l’aperçu. **JHRCADREEDIT** ou double-clic
pour modifier le cadre et ses coupes. Réutilisation de bibliothèques locales, API JSON.
[Mode d’emploi et limites](CADRES.md) · [API agent](CADRES_AGENT_API.md).

## Installer

Paquet autonome pour **AutoCAD / Advance Steel 2027 Windows x64, .NET 10**.
Copier `JHR.Panneaux.bundle` dans `%APPDATA%/Autodesk/ApplicationPlugins`, ou lancer
`InstallPlugin.ps1` depuis ce dossier. Il sauvegarde un éventuel paquet existant.
Redémarrer AutoCAD après avoir enregistré ses plans pour contrôler l’autoload.
Pour la session courante : **NETLOAD** → `JhrMaillePlugin_v1_5_1_final.dll`.
Le plugin ne change pas les réglages de sécurité. Un avertissement de sécurité doit être
traité par l’utilisateur selon les règles de son installation.

Le LISP et l’ancienne DLL ne sont plus nécessaires au plugin. Si le LISP est utilisé pour
ses commandes CLI, recharger la version fournie : `JHRMAILLE` donne priorité au plugin.
`JHRMAILLECLI` reste l’ancien mode de secours. Ne pas charger simultanément plusieurs
versions de DLL lors d’une installation normale.

## Créer / modifier

1. **JHRMAILLE** ou **JHRPANNEAU**.
2. Sélectionner la polyligne fermée intérieure, puis extérieure du cadre.
   Pour un grillage à limite unique, choisir **Unique** à la deuxième sélection.
3. Choisir l’outil et ses paramètres dans la colonne de gauche.
4. L’aperçu entier se met à jour dans le dessin ; les contours sont identifiés temporairement
   en bleu (intérieur) et orange (extérieur).
5. **Appliquer** valide. **Annuler**, Échap dans la fenêtre ou la croix abandonne.

**Double-clic sur un panneau JHR**, ou **JHRPANNEAUEDIT**, rouvre ses paramètres.
Changer d’outil remplace son remplissage en conservant son emplacement et ses contours.
Le filet exige toujours **deux contours** : si le grillage n’avait qu’un contour,
choisir **Filet inox losange** demande seulement la polyligne extérieure manquante.
Le premier contour reste conservé ; une sélection invalide est refusée et Échap
revient à l’outil précédent. Aucun filet à contour unique n’est validé.
Le double-clic ouvre uniquement l’éditeur JHR pour ces panneaux ; les autres blocs
gardent leur éditeur AutoCAD. Le réglage général de double-clic est restauré.
Les paramètres des quatre outils sont conservés séparément. Une seule annulation AutoCAD
restaure la modification. Modifier une copie ne change pas les autres copies.
Les contours d’origine ne sont pas recolorés. La création du panneau ne redessine pas le cadre.
Les géométries de contour sont enregistrées dans le panneau ; un déplacement ultérieur de
la polyligne source ne le met pas à jour. **Resélectionner les contours** permet de le faire.

Les blocs sont nommés `PANNEAU_…_largeurxhauteur_numéro`, dimensions totales du panneau.
Tout leur contenu est sur le **calque 0 / couleur et épaisseur DuCalque**.
Les blocs restent affichables dans un AutoCAD sans plugin ; l’édition des paramètres demande la DLL.

## Réglages

| Outil | Paramètres |
|---|---|
| Filet inox losange | 202 références Jakob : Micro, standard, sans douilles, Duplex et rouleaux ; ou saisie libre, diagonales centre à centre, Ø câble, câble de laçage, retrait, rotation et accessoires |
| Grillage serrurier | Ouverture libre carrée, Ø fil, retrait, rotation, un ou deux contours, traits cachés sous cadre |
| Métal déployé | ML, MW, largeur projetée de lanière, épaisseur, plat de jonction, aplati / relief |
| Tôle perforée | Ø / côté du trou, entraxe, bordure pleine, épaisseur, R/U, R/T60 ou C/U |

Le laçage du filet conserve les retours adaptés à la section du cadre et les compléments d’angle.
Le retrait de maille est strictement respecté. Les losanges principaux restent complets.
Les fils du grillage utilisent deux bords, avec extrémités fermées et interruption aux croisements.
Le masquage peut être **uniquement sous les fils** (ouvertures transparentes) ou sur toute
la surface du grillage ; les masques restent derrière les fils et hors du cadre.
Les formes simples rectangulaires, trapézoïdales et concaves sont acceptées ; les formes
croisées et retraits qui divisent le panneau sont refusés. Réduire la densité ou diviser un
panneau si une limite de calcul est atteinte.

Les remplissages déployés/perforés et les accessoires sont des représentations 2D
paramétriques. Ils ne remplacent pas une validation de résistance ou de fixation.
Les anciens panneaux LISP sont importables si leurs contours sources existent ; les
paramètres non stockés dans l’ancien format sont signalés et doivent être vérifiés.

## Agent IA

[API courte](AGENT_API.md) : requêtes JSON, handles, patches, réponses compactes, contrôles
sans écriture et lots atomiques. Aucun pilotage de fenêtre ni lecture de tout le modèle.
[Transport Python](Tools/agent_client.py) pour l’AutoCAD actif via COM.
[Architecture et compilation](ARCHITECTURE.md). [Ancien usage LISP](../JHR_Maille_2D.lsp).

Les essais sont décrits dans [VALIDATION_NET.md](VALIDATION_NET.md).
L’autoload après redémarrage reste à vérifier sur cette installation ; les fonctions ont
été testées dans une session AutoCAD 2027 chargée par NETLOAD, dans un DWG séparé.

## Interface 1.4.4

Navigation sombre avec pictogrammes, sections de réglages, champs plus lisibles,
boutons arrondis et commandes fixes en bas. En saisie libre, les champs fabricant
sans objet sont masqués ; choisir une gamme Jakob les affiche automatiquement.
L’inspecteur reste redimensionnable et les réglages supplémentaires défilent.
Les aperçus, annulations et paramètres de panneau conservent leur fonctionnement.

## Mises à jour GitHub — à partir de 1.4.4

Dans le bandeau de JHRMAILLE, cliquer **Mises à jour**, ou lancer **JHRPANNEAUUPDATE**.
La fenêtre vérifie la version publiée. Si une version compatible plus récente existe,
cliquer **Installer**. Enregistrer les plans et redémarrer AutoCAD pour l’utiliser.
La version chargée reste active jusqu’à la fermeture ; le plugin ne ferme aucun plan.

La vérification est déclenchée uniquement à l’ouverture de cette fenêtre ou par **Vérifier**.
Elle contacte le dépôt public JoLaFripouille/AutoLisp, sans envoyer de dessin, de paramètres
ou de données de chantier. Hors connexion, le plugin reste utilisable.
Le paquet est contrôlé (taille, SHA-256, version de DLL, compatibilité AutoCAD 2027).
Une sauvegarde du bundle précédent est créée sous `%APPDATA%/Autodesk/JHR-Panneaux-Backups`.
La DLL chargée n’est jamais écrasée ; le descriptif de démarrage est remplacé en dernier.
Les versions antérieures à 1.4.4 nécessitent une installation manuelle unique de ce bundle.

Pour publier une version : incrémenter version/nom de DLL, compiler, actualiser le bundle,
puis lancer `python Tools/publish_package.py CHEMIN_BUNDLE CHEMIN_UPDATES`.
Publier ensemble le ZIP et `Updates/latest.json` ; ne pas remplacer un paquet déjà publié.
