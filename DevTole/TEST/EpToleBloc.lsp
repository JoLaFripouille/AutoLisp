(vl-load-com)

(defun distance (pt1 pt2)
  (sqrt (+ (expt (- (car pt2) (car pt1)) 2) (expt (- (cadr pt2) (cadr pt1)) 2))))

(defun find-polyline-in-block (block)
  (vlax-for obj block
    (if (eq (vla-get-ObjectName obj) "AcDbPolyline")
      (progn
        (setq polyline obj)
        (return-from find-polyline-in-block polyline)
      )
    )
  )
  nil
)

(defun get-min-length (vla_obj)
  (setq segments (vlax-safearray->list (vlax-variant-value (vla-get-Coordinates vla_obj))))
  (setq num_points (/ (length segments) 2))
  (setq lengths '())
  (setq i 0)
  (while (< i (- num_points 1))
    (setq pt1 (list (nth (* 2 i) segments) (nth (+ 1 (* 2 i)) segments)))
    (setq pt2 (list (nth (* 2 (+ 1 i)) segments) (nth (+ 1 (* 2 (+ 1 i))) segments)))
    (setq length (distance pt1 pt2))
    (setq lengths (cons length lengths))
    (setq i (+ i 1))
  )
  (apply 'min lengths)
)

(defun c:EPAISSEURTOLE (/ ent ent_point vla_obj min_length)
  (prompt "\nSélectionnez une polyligne ou un bloc contenant une polyligne représentant la coupe de la tôle.")
  
  ; Sélectionner une entité
  (while (not (setq ent_point (entsel "\nSélectionnez une polyligne ou un bloc ou appuyez sur ESC :"))))
  (setq ent (car ent_point))

  ; Convertir l'entité sélectionnée en objet VLA
  (setq vla_obj (vlax-ename->vla-object ent))

  ; Vérifiez si c'est une polyligne ou un bloc
  (cond
    ((eq (vla-get-ObjectName vla_obj) "AcDbPolyline")
     (prompt "\nObjet sélectionné est une polyligne.")
     (setq min_length (get-min-length vla_obj))
    )
    ((eq (vla-get-ObjectName vla_obj) "AcDbBlockReference")
     (prompt "\nObjet sélectionné est un bloc.")
     (setq block (vlax-invoke vla_obj 'GetDynamicBlockProperties))
     (if block
       (setq polyline (find-polyline-in-block block))
     )
     (if polyline
       (setq min_length (get-min-length polyline))
       (prompt "\nAucune polyligne trouvée dans le bloc.")
     )
    )
    (t (prompt "\nL'objet sélectionné n'est ni une polyligne ni un bloc."))
  )

  ; Afficher la longueur minimale dans la console
  (if min_length
    (progn
      (prompt (strcat "\nL'épaisseur de la tôle est : " (rtos min_length 2 4)))
      (princ (strcat "\nL'épaisseur de la tôle est : " (rtos min_length 2 4) "\n"))
    )
  )
  (princ)
)

(princ "\nTapez EPAISSEURTOLE pour utiliser la commande.")
(princ)
