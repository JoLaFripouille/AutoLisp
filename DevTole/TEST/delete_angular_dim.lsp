(defun c:SupprimerCotesAngulaires ()
  ;; Sélectionner toutes les cotes dans le dessin
  (setq selection (ssget "_X" '((0 . "DIMENSION"))))

  ;; Vérifier si une sélection a été faite
  (if selection
    (progn
      ;; Initialiser un compteur pour les suppressions
      (setq x 0)

      ;; Boucler sur chaque cote sélectionnée
      (repeat (sslength selection)
        ;; Obtenir les données de l'entité
        (setq ent (entget (ssname selection x)))
        
        ;; Obtenir le texte de la cote (DXF 1) et vérifier s'il contient un symbole d'angle "°"
        (setq cote-texte (cdr (assoc 1 ent)))
        
        ;; Si le texte contient "°", c'est probablement une cote angulaire
        (if (and cote-texte (wcmatch cote-texte "*°*"))
          (progn
            (entdel (ssname selection x)) ;; Supprimer l'entité
            (princ (strcat "\nCote angulaire supprimée : " cote-texte)) ;; Message de suppression
          )
        )

        ;; Incrémenter l'indice pour passer à l'objet suivant
        (setq x (+ x 1))
      )
      (princ "\nSuppression des cotes angulaires terminée.")
    )
    (princ "\nAucune cote angulaire n'a été trouvée.")
  )
  (princ)
)
