(defun C:REPDXF (/ prefix suffix blockName maxSuffix blockList blkName insertionPoint filter selection)
  ;; Demande à l'utilisateur de fournir le préfixe du nom de bloc, avec "TL" comme valeur par défaut
  (setq prefix (getstring (strcat "\nEntrez le préfixe du nom de bloc (par défaut: TL): ")))
  (if (= prefix "") (setq prefix "TL"))  ;; Si aucun préfixe n'est donné, utilise "TL"

  ;; Demande à l'utilisateur de fournir le suffixe du nom de bloc, avec "_" comme valeur par défaut
  (setq suffix (getstring (strcat "\nEntrez le suffixe du nom de bloc (par défaut: _): ")))
  (if (= suffix "") (setq suffix "_"))  ;; Si aucun suffixe n'est donné, utilise "_"

  ;; Initialisation de variables
  (setq maxSuffix 0)  ;; pour stocker le plus grand numéro trouvé
  (setq blockList (vla-get-blocks (vla-get-activedocument (vlax-get-acad-object)))) ;; liste des blocs

  ;; Parcours des blocs existants pour trouver ceux qui commencent par le préfixe
  (vlax-for blk blockList
    (setq blkName (strcase (vla-get-name blk)))  ;; nom du bloc en majuscules pour éviter les erreurs de casse
    (if (wcmatch blkName (strcat (strcase prefix) "*"))  ;; vérifie si le bloc commence par le préfixe
      (progn
        ;; extrait le suffixe numérique du bloc et le compare
        (setq numSuffix (atoi (substr blkName (+ 1 (strlen prefix)))))
        (if (> numSuffix maxSuffix)
          (setq maxSuffix numSuffix)
        )
      )
    )
  )

  ;; Incrémente le numéro suffixe de 1 pour le nouveau bloc
  (setq blockName (strcat prefix (itoa (+ maxSuffix 1)) suffix))

  ;; Définir le filtre de sélection pour autoriser uniquement certains types d'objets
  (setq filter '((0 . "LINE,CIRCLE,LWPOLYLINE,POLYLINE,SPLINE,ARC,ELLIPSE")))

  ;; Demande à l'utilisateur de sélectionner les objets pour créer le nouveau bloc, avec un filtre appliqué
  (princ (strcat "\nSélectionnez les objets (lignes, cercles, polylignes, splines, arcs ou ellipses) pour créer le bloc " blockName ": "))
  (setq selection (ssget "_:L" filter))

  ;; Vérifie si une sélection a été faite
  (if selection
    (progn
      ;; Demande un point d'insertion pour le nouveau bloc
      (setq insertionPoint (getpoint "\nIndiquez le point d'insertion pour le nouveau bloc: "))

      ;; Crée le bloc avec les objets sélectionnés, en utilisant le point d'insertion donné
      (command "_.-block" blockName insertionPoint selection "")

      ;; Insère immédiatement le bloc à la même position que le point d'insertion
      (command "_.insert" blockName insertionPoint "1" "1" "0")

      ;; Affiche un message confirmant la création et l'insertion du bloc
      (princ (strcat "\nLe bloc " blockName " a été créé et inséré avec succès aux coordonnées du point d'insertion."))
    )
    (princ "\nAucune sélection valide effectuée, annulation de la commande.")
  )

  ;; Termine proprement la fonction
  (princ)
)
