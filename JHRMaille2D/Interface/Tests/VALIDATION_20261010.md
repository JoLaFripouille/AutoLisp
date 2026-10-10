# Validation JHRMAILLE 1.1.0 — 10 octobre 2026

- Compilation AutoCAD 2027 / .NET 10 : zéro erreur, zéro avertissement.
- 202 références uniques issues du catalogue Webnet 04/2026 ; nombres contrôlés par famille.
- Application et filtrage des 202 choix dans la fenêtre ; dimensions et types de jonctions conformes aux données embarquées ; retour en saisie libre et restauration du choix.
- Réglages de montage conservés : retrait 20 mm, diamètre de laçage, orientation, angles et pas des butées.
- Rouleaux : dimensions de maille de la référence Micro de base, distinctes de H × L.
- Essais natifs dans un nouveau DWG séparé : la vraie fonction LISP .NET renvoie les 11 valeurs de paramètres et les trois textes d’identification. Valider, Annuler, croix et Échap réussissent ; les fermetures conservent les paramètres et la référence précédents.
- Création native de deux panneaux avec contours conservés : Webnet sans douilles 20260-0150-060 et Micro 20261-0100-020. Le dessin de test est sauvegardé avec six objets (quatre contours et deux blocs).
- Référence, famille et matière enregistrées en XData. Modifier une dimension invalide l’identification ; modifier l’orientation la conserve.
- 1025 objets de définitions de blocs contrôlés : calque 0, couleur DuCalque et épaisseur DuCalque ; zéro écart.
- 61 fonctions existantes inchangées. Seules la transmission de l’interface et l’écriture des métadonnées dans la fonction de génération évoluent pour le catalogue.
- Tous les dessins ouverts rechargent le LISP sans variation de leurs nombres d’objets ni de leurs états de sauvegarde. Les essais de création concernent uniquement le nouveau DWG.

Les tests autonomes sont dans Program.cs. La comparaison des fonctions est effectuée sur les expressions LISP complètes, commentaires et blancs normalisés. Les pilotes natifs de la machine d’essai restent locaux ; ils ne sont pas exécutés à l’installation de la commande.

## Interface 1.2 — colonne d’outils

Ajout de la navigation à gauche et du nom Filet inox losange. Compilation sans erreur ni avertissement. Les contrôles des 202 références et des fermetures passent ; cliquer sur l’outil conserve les valeurs et la fenêtre. Contrôle visuel de la fenêtre autonome et de la vraie fenêtre AutoCAD. Valider, Annuler, croix et Échap passent dans AutoCAD. Aucun calcul de géométrie modifié ; seuls le nom de la DLL et les fonctions de chargement changent dans le LISP. Les nombres d’objets et états de sauvegarde des autres dessins restent identiques ; seul le DWG de test est enregistré après le chargement du nouveau complément.
