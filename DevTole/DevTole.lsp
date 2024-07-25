(defun c:DevTole (/ pline ent plength tlength pt1 pt2 pt3 pt4)
  ;; Demander à l'utilisateur de sélectionner une polyligne
  (setq pline (car (entsel "\nSélectionnez une polyligne : ")))

  ;; Vérifier que l'entité sélectionnée est une polyligne
  (if (and pline (= (cdr (assoc 0 (entget pline))) "LWPOLYLINE"))
    (progn
      ;; Calculer la longueur de la polyligne
      (setq plength (vlax-curve-getDistAtParam pline (vlax-curve-getEndParam pline)))
      ;; Demander à l'utilisateur d'entrer la longueur de la tôle
      (setq tlength (getreal "\nEntrez la longueur de la tôle : "))
      ;; Définir les points du rectangle
      (setq pt1 '(0 0 0)
            pt2 (list plength 0 0)
            pt3 (list plength tlength 0)
            pt4 (list 0 tlength 0))
      ;; Créer le rectangle
      (command "._PLINE" pt1 pt2 pt3 pt4 "C")
    )
    (princ "\nVeuillez sélectionner une polyligne.")
  )
  (princ)
)

(princ "\nEntrez 'DevTole' pour exécuter le script.")
(princ)
