(defun C:SimilarText ()
  (vl-load-com)

  ;; Fonction pour obtenir le contenu du texte sélectionné
  (defun GetTextContent (ent)
    (cdr (assoc 1 (entget ent))))

  ;; Fonction pour sélectionner tous les textes similaires dans le dessin
  (defun SelectSimilarTexts (refText)
    (ssget "X" (list (cons 0 "TEXT") (cons 1 refText))))

  ;; Demander à l'utilisateur de sélectionner un texte de référence
  (princ "\nSélectionnez un texte de référence : ")
  (setq sel (entsel))
  (if sel
    (progn
      (setq ent (car sel)) ; Obtenir l'entité sélectionnée
      (setq refText (GetTextContent ent)) ; Obtenir le contenu du texte de référence

      ;; Sélectionner tous les textes similaires
      (setq ssSimilar (SelectSimilarTexts refText))
      (if ssSimilar
        (progn
          (princ (strcat "\nNombre de textes similaires trouvés : " (itoa (sslength ssSimilar))))
          ;; Afficher les coordonnées de chaque texte similaire trouvé
          (setq i 0)
          (repeat (sslength ssSimilar)
            (setq ent (ssname ssSimilar i))
            (setq entData (entget ent))
            (setq textContent (cdr (assoc 1 entData)))
            (setq textInsert (cdr (assoc 10 entData)))
            (princ (strcat "\nTexte trouvé : " textContent))
            (princ (strcat "\n  Position : (" (rtos (car textInsert) 2 2) ", " (rtos (cadr textInsert) 2 2) ")"))
            (setq i (1+ i))
          )
          ;; Mettre en surbrillance les textes similaires trouvés
          (sssetfirst nil ssSimilar)
        )
        (princ "\nAucun texte similaire trouvé.")
      )
    )
    (princ "\nAucun texte de référence sélectionné.")
  )

  (princ "\nProgramme terminé.") ;; Terminer proprement la fonction
)
