# Métal déployé — correction 1.3.2, 11 octobre 2026

- Compilation .NET10 / AutoCAD2027 : 0 erreur, 0 avertissement. Les tests des quatre pages et des 202 références Jakob restent valides.
- Nouvelle forme : ouvertures à plats de jonction, raccords adoucis, lanières continues. Paramètres de plat : automatique ou longueur en mm. Plus de croisement X entre traits épais.
- Comparaison exacte des coordonnées du contour dans le renderer .NET et dans AutoLISP : 30 points, écart maximal inférieur à 1e-10 mm, cas ML40 / MW20 / lanière2.
- Six panneaux natifs : auto, plat7, trapèze, concave, tourné17°, relief60×30. Toutes les sources sont conservées.
- Découpe : tous les sommets du résultat sont dans le contour cible. Le matériau est coupé au bord, sans ligne bouchant les ouvertures.
- 6 534 polylignes auditées : largeur0, calque0, couleur et épaisseur DuCalque. Les propriétés absentes dans le DXF correspondent aux valeurs par défaut et sont acceptées comme telles.
- Commande complète avec deux contours : un panneau ajouté, désignation et dimensions du nom vérifiées, puis retiré par une seule annulation.
- Plat incompatible et densité excessive : refus sans objets résiduels. Dessin client préservé ; essais dans un DWG séparé.

L’image de référence sert à la forme du motif, pas aux dimensions d’une référence fabricant. Le plat automatique et les adoucissements sont des conventions de représentation modifiables ; aucun gabarit de fabrication certifié n’est revendiqué.
