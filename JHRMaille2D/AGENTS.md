# JHR Panneaux .NET

Pour manipuler des panneaux, lire seulement AGENT_API.md et utiliser l’API JSON.
Ne pas parcourir GeometryCompiled.cs ni piloter la fenêtre pour une modification automatique.
L’API fait le calcul et l’écriture dans AutoCAD ; les appels sont séquentiels.

Pour développer : ARCHITECTURE.md. Le moteur 1.4 remplace la géométrie LISP,
conserve les contours/laçages approuvés et compile les algorithmes en C#.
Pas de moteur AutoLISP à l’exécution. La sauvegarde GeometrySource est une source de maintenance.
Tout contenu de panneau : calque 0, couleur/épaisseur DuCalque ; références sur le calque courant.
L’aperçu et les couleurs de contours sont temporaires ; Annuler ne modifie pas le DWG.
Ne pas reconstituer un cadre à partir des dimensions extérieures du bloc.
Ne pas remplacer des copies d’un panneau en modifiant sa définition partagée.
Ne pas modifier SECURELOAD/TRUSTEDPATHS. Essais dans un DWG distinct du plan client.
Compiler avec .NET 10 et les références locales AutoCAD 2027, puis vérifier dans AutoCAD.
Les mailles déployées/perforées sont des représentations paramétriques génériques.
