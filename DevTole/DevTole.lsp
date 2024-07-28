(defun c:DevTole (/ dcl_id length repere epaisseur pline plength pt1 pt2 pt3 pt4 dcl_path)
  ;; Définir le chemin en dur pour le fichier DCL
  (setq dcl_path "D:/PROJECT/LISP/AutoLisp/DevTole/DevTole.dcl")
  (princ (strcat "\nLe fichier DCL est situé dans le répertoire : " dcl_path))

  ;; Vérifier si le fichier DCL existe
  (if (findfile dcl_path)
    (progn
      (princ "\nFichier DCL trouvé.")

      ;; Charger le fichier DCL
      (setq dcl_id (load_dialog dcl_path))
      (if dcl_id
        (progn
          (princ "\nFichier DCL chargé avec succès.")
          
          ;; Obtenir le chemin du répertoire et le nom du fichier DWG
          (setq dwg-path (strcat (getvar "DWGPREFIX") (getvar "DWGNAME")))
          (princ (strcat "\nLe fichier DWG est situé dans le répertoire : " dwg-path))

          ;; Demander à l'utilisateur de sélectionner une polyligne
          (setq pline (car (entsel "\nSélectionnez une polyligne : ")))

          ;; Vérifier que l'entité sélectionnée est une polyligne
          (if (and pline (= (cdr (assoc 0 (entget pline))) "LWPOLYLINE"))
            (progn
              ;; Calculer la longueur de la polyligne
              (setq plength (vlax-curve-getDistAtParam pline (vlax-curve-getEndParam pline)))

              ;; Afficher la fenêtre de dialogue
              (if (new_dialog "DevToleDialog" dcl_id)
                (progn
                  (princ "\nFenêtre de dialogue affichée avec succès.")
                  
                  ;; Afficher la fenêtre de dialogue et récupérer les entrées utilisateur
                  (action_tile "length" "(setq length $value)")
                  (action_tile "repere" "(setq repere $value)")
                  (action_tile "epaisseur" "(setq epaisseur $value)")
                  (start_dialog)
                  (unload_dialog dcl_id)

                  ;; Convertir les valeurs entrées en nombres
                  (setq length (atof length))
                  (setq epaisseur (atof epaisseur))

                  ;; Définir les points du rectangle
                  (setq pt1 '(0 0 0)
                        pt2 (list plength 0 0)
                        pt3 (list plength length 0)
                        pt4 (list 0 length 0))

                  ;; Créer le rectangle
                  (command "._PLINE" pt1 pt2 pt3 pt4 "C")

                  ;; Créer un bloc avec le rectangle et l'épaisseur
                  (command "._-BLOCK" repere pt1
                           "._PLINE" pt1 pt2 pt3 pt4 "C"
                           "_TEXT" (list (/ plength 2) (/ length 2) 0) 0.25 0 (strcat "Epaisseur: " (rtos epaisseur 2 2))
                           ""
                  )

                  ;; Afficher un message indiquant que la tôle a été créée
                  (alert "Tole creee et ajoutee au bloc")
                )
                (princ "\nErreur lors de l'affichage de la fenêtre de dialogue.")
              )
            )
            (princ "\nVeuillez sélectionner une polyligne.")
          )
        )
        (princ (strcat "\nErreur lors du chargement du fichier DCL: " dcl_path))
      )
    )
    (princ (strcat "\nErreur: Fichier DCL non trouvé: " dcl_path))
  )
  (princ)
)
