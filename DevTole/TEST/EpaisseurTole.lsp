(vl-load-com)

(defun distance (pt1 pt2)
  (sqrt (+ (expt (- (car pt2) (car pt1)) 2) (expt (- (cadr pt2) (cadr pt1)) 2))))

(defun c:EPAISSEURTOLE (/ ent ent_point vla_obj segments lengths min_length pt1 pt2 length num_points i)
  (prompt "\nSélectionnez une polyligne représentant la coupe de la tôle.")
  
  ; Sélectionner une polyligne
  (while (not (setq ent_point (entsel "\nSélectionnez une polyligne ou appuyez sur ESC :"))))
  (setq ent (car ent_point))

  ; Convertir la polyligne sélectionnée en objet VLA
  (setq vla_obj (vlax-ename->vla-object ent))

  ; Vérifiez que c'est une polyligne
  (if (eq (vla-get-ObjectName vla_obj) "AcDbPolyline")
    (progn
      (prompt "\nObjet sélectionné est une polyligne.")

      ; Initialiser la liste des longueurs des segments
      (setq lengths '())

      ; Récupérer les coordonnées des sommets de la polyligne
      (setq segments (vlax-safearray->list (vlax-variant-value (vla-get-Coordinates vla_obj))))
      (setq num_points (/ (vl-list-length segments) 2))
      
      ; Parcourir les segments de la polyligne
      (setq i 0)
      (while (< i (- num_points 1))
        (setq pt1 (list (nth (* 2 i) segments) (nth (+ 1 (* 2 i)) segments)))
        (setq pt2 (list (nth (* 2 (+ 1 i)) segments) (nth (+ 1 (* 2 (+ 1 i))) segments)))
        (setq length (distance pt1 pt2))
        (setq lengths (cons length lengths))
        (setq i (+ i 1))
      )

      ; Trouver la longueur minimale
      (setq min_length (apply 'min lengths))

      ; Afficher la longueur minimale dans la console
      (princ (strcat "\nL'épaisseur de la tôle est : " (rtos min_length 2 4) "\n"))
    )
    (prompt "\nL'objet sélectionné n'est pas une polyligne.")
  )
  (princ)
)

(princ "\nTapez EPAISSEURTOLE pour utiliser la commande.")
(princ)
