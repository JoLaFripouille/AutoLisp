(defun c:DevTole (/ dcl_id length repere pline plength pt1 pt2 pt3 pt4)
  ;; Demander à l'utilisateur de sélectionner une polyligne
  (setq pline (car (entsel "\nSélectionnez une polyligne : ")))

  ;; Vérifier que l'entité sélectionnée est une polyligne
  (if (and pline (= (cdr (assoc 0 (entget pline))) "LWPOLYLINE"))
    (progn
      ;; Calculer la longueur de la polyligne
      (setq plength (vlax-curve-getDistAtParam pline (vlax-curve-getEndParam pline)))

      ;; Charger le fichier DCL
      (setq dcl_id (load_dialog "D:/PROJECT/LISP/AutoLisp/DevTole/DevTole.dcl"))

      ;; Vérifier si le fichier DCL est chargé avec succès
      (if (not (null dcl_id))
        (progn
          (princ "\nFichier DCL chargé avec succès.")
          ;; Afficher la fenêtre de dialogue
          (if (new_dialog "DevToleDialog" dcl_id)
            (progn
              (princ "\nFenêtre de dialogue affichée avec succès.")
              ;; Afficher la fenêtre de dialogue et récupérer les entrées utilisateur
              (action_tile "length" "(setq length $value)")
              (action_tile "repere" "(setq repere $value)")
              (start_dialog)
              (unload_dialog dcl_id)

              ;; Convertir la longueur entrée en nombre
              (setq length (atof length))

              ;; Définir les points du rectangle
              (setq pt1 '(0 0 0)
                    pt2 (list plength 0 0)
                    pt3 (list plength length 0)
                    pt4 (list 0 length 0))

              ;; Créer le rectangle
              (command "._PLINE" pt1 pt2 pt3 pt4 "C")

              ;; Créer un bloc avec le rectangle
              (command "._-BLOCK" repere pt1 "._PLINE" pt1 pt2 pt3 pt4 "C" "")

              ;; Afficher un message indiquant que la tôle a été créée
              (alert "Tole creee et ajoutee au bloc")
            )
            (princ "\nErreur lors de l'affichage de la fenêtre de dialogue.")
          )
        )
        (princ "\nErreur lors du chargement du fichier DCL.")
      )
    )
    (princ "\nVeuillez sélectionner une polyligne.")
  )
  (princ)
)

(princ "\nEntrez 'DevTole' pour exécuter le script.")
(princ)
