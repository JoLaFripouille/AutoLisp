# Validation .NET 1.4.4 — 11/10/2026

- Compilation .NET 10 / SDK AutoCAD 2027 : zéro erreur et avertissement.
- Interface : quatre outils, paramètres conservés, valeurs invalides refusées,
  validation/annulation/croix, modes de contours et masquage ; 202 références Jakob.
- Mathématiques : aire, inclusion, contours croisés, calcul C# du déployé,
  annulation du calcul obsolète ; génération C# reproductible.
- AutoCAD natif, DWG de test distinct : quatre outils, cadre 750,7 × 762 mm,
  trapèze, contour concave, orientation 17° et filet sans douilles.
- Comparaison à l’algorithme LISP approuvé : cinq cas (filet serti, tressé, carré,
  déployé, perforé), respectivement 952 / 1204 / 1810 / 4282 / 6160 objets dessinés.
  Toutes les coordonnées mondiales, largeurs, fermetures et cercles concordent à
  0,00001 mm ; comptages de masques identiques. Le type Continuous de l’ancien
  moteur est normalisé vers DuCalque, changement volontaire.
- API par le véritable client Python COM : list/get/check/update, patch partiel,
  révision périmée refusée, pagination catalogue, paramètre inconnu refusé,
  batch invalide entièrement annulé et mauvais DWG refusé.
- Mises à jour : handle/placement conservés, copies indépendantes, UNDO 1.
- Éditeur : aperçu dans le DWG, contours bleu/orange, conversion entre outils.
- Grillage à contour unique → filet : demande du seul contour extérieur,
  conservation de l’intérieur, aperçu puis validation ; réouverture conserve
  les deux contours et les paramètres.
- Correctif double-clic : essais réels sur grillage puis filet, sans deuxième
  fenêtre BEDIT retardée. Un bloc ordinaire ouvre toujours l’éditeur AutoCAD.
  DBLCLKEDIT est restauré à sa valeur d’origine (1 dans les essais).
- Déployé optimisé : environ 0,13 s sur le panneau d’essai ; dépend de la densité.

Les essais de géométrie portent sur les builds successifs du moteur 1.4 ; le
transport API et le correctif d’édition ont été recontrôlés avec 1.4.2.
Le bundle est installé ; le chargement automatique après redémarrage reste à
contrôler. La session courante utilise NETLOAD. Le Core Console de l’installation
échoue avant chargement du plugin : ce mode n’est pas validé.
Aucun plan client n’a reçu d’écriture de test.

## Interface 1.4.4

Compilation sans avertissement ; suite des quatre pages, masquages, contours,
valeurs invalides, validation/annulation et 202 choix Jakob entièrement passée.
Contrôle visuel dans AutoCAD : formulaire compact, catégories, champs personnalisés
simplifiés, boutons fixes, clic réel de navigation vers le grillage, aperçu du
déployé et Annuler qui restitue le filet. Révision du panneau identique après
Annuler ; DBLCLKEDIT restauré à 1. Aucun changement de dessin hors DWG de test.

## Bandeau et mises à jour 1.4.4

Compilation sans avertissement. Bandeau contrôlé à 100 %, 125 %, 150 % et trois
largeurs : aucun bouton coupé ni étiré. Suite des quatre outils et 202 références passée.
Mise à jour testée avec transport simulé : manifeste, téléchargement, hash incorrect,
fichier inattendu/dupliqué, chemin extérieur, DLL invalide, version incompatible,
annulation, hors connexion, sauvegarde, DLL chargée conservée, version déjà installée.
Un manifeste verrouillé provoque un rollback sans changer l’ancienne installation.
Le redémarrage automatique est laissé à l’utilisateur ; aucun plan n’est fermé.

## Cadres 1.5.1 — 11 octobre 2026

Compilation Release : 0 erreur, 0 avertissement. Tests isolés dans AutoCAD/Advance
Steel 2027, DWG distinct des plans client, session chargée par NETLOAD.
294 sections valides de gabarit et 625 fichiers Jansen indexés localement ; cinq
sections Jansen effectivement importées. Tous les fichiers indexés ne sont pas
des sections certifiées utilisables : import et validation à la demande.

Vraie section tube 40×40 ép.3 : deux parois longitudinales de 3 mm, vide conservé.
Rotations 0/90/180/270 et miroirs testés sur tube, IPE100 et section Jansen30011.
Création native des quatre outils avec deux coupes, modification du même handle,
placements conservés, édition indépendante d’une copie, refus de dessin erroné
et révision périmée, annulation AutoCAD unique du cadre et de ses coupes, propriétés calque0/DuCalque contrôlées. Comptages et état
d’enregistrement des autres dessins ouverts inchangés.

Interface : modification propagée, création/déplacement d’une ligne de coupe,
annulation sur les quatre familles, commande native d’édition et double-clic
sur le cadre Jansen. Aucun dialogue BEDIT concomitant observé.
Suite des panneaux existants, 202 références et tests de mise à jour passée.
Tests de mise à jour 1.5.1 : installation à partir d’une fixture 1.4.4 simulée.

Limites : rectangles, sections d’angles à45° refusées, ouvrants/quincaillerie
Jansen non assemblés. Voir CADRES.md. Autoload après redémarrage non vérifié.

Contrôle complémentaire 1.5.1 : ossature 80×80 avec dormant 40×40, divisions à 60 mm refusées et à 100 mm acceptées, testé dans AutoCAD. Double-clic d’un panneau carré existant et ouverture JHRCADRE avec tube 40×40 ép.3 vérifiés sans dialogue BEDIT.
