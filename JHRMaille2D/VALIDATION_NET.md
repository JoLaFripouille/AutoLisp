# Validation .NET 1.4.2 — 11/10/2026

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
