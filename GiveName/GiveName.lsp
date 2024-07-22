(defun C:GiveName ()
  (vl-load-com)

  ;; Fonction pour obtenir le rayon d'un cercle
  (defun GetCircleRadius (ent)
    (cdr (assoc 40 (entget ent))))

  ;; Fonction pour obtenir le centre d'un cercle
  (defun GetCircleCenter (ent)
    (cdr (assoc 10 (entget ent))))

  ;; Fonction pour ajouter un texte au-dessus du cercle
  (defun AddTextAboveCircle (center diameter prefix)
    (setq txt (strcat prefix " " (rtos diameter 2 2))) ; Texte à ajouter
    (setq textHeight 15) ;------------------------------------------------------ Hauteur du texte
    (setq textOffset 5.0) ;----------------------------------------------------- Décalage au-dessus du cercle
    (setq insertionPoint (list (car center) (+ (cadr center) (/ diameter 2) textOffset) 0.0)) ; Point d'insertion du texte
    (entmake
      (list
        '(0 . "TEXT")
        '(100 . "AcDbEntity")
        '(100 . "AcDbText")
        (cons 10 insertionPoint)
        (cons 40 textHeight)
        (cons 1 txt)
        '(50 . 0) ; Angle de rotation
        '(7 . "Standard") ; Style de texte
      )
    )
  )

  ;; Demander à l'utilisateur de sélectionner des cercles
  (princ "\nSélectionnez un ou plusieurs cercles : ")
  (setq ss (ssget '((0 . "CIRCLE"))))
  (if ss
    (progn
      ;; Demander le préfixe
      (setq prefix (getstring "\nEntrez le préfixe : "))

      ;; Parcourir les cercles sélectionnés et ajouter le texte
      (setq i 0)
      (repeat (sslength ss)
        (setq ent (ssname ss i))
        (setq radius (GetCircleRadius ent))
        (setq diameter (* 2 radius)) ; Calculer le diamètre
        (setq center (GetCircleCenter ent))
        (AddTextAboveCircle center diameter prefix)
        (setq i (1+ i))
      )
      (princ "\nTexte ajouté avec succès au-dessus des cercles sélectionnés.")
    )
    (princ "\nAucun cercle sélectionné.")
  )

  (princ "\nProgramme terminé.") ;; Terminer proprement la fonction
)
