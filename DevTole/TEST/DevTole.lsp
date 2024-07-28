(vl-load-com)

(defun c:offsetout (/ dcl_id offset_value direction ent_point ent vla_obj offset_point)
  (setq dcl_id (load_dialog "offset_dialog.dcl")) ; Charger le fichier DCL
  (if (not (new_dialog "offset_dialog" dcl_id))
    (progn
      (alert "Impossible de charger le fichier DCL.")
      (exit)
    )
  )

  ; Afficher la boîte de dialogue
  (action_tile "offset_value" "(setq offset_value $value)")
  (action_tile "interior" "(setq direction -1)")
  (action_tile "exterior" "(setq direction 1)")

  (setq result (start_dialog))
  (unload_dialog dcl_id)

  (if (= result 1)
    (progn
      (setq offset_value (atof offset_value)) ; Convertir en nombre
      (prompt (strcat "\nValeur de décalage : " (rtos offset_value 2 4)))
      (prompt (strcat "\nDirection : " (itoa direction)))

      ; Sélectionner une polyligne
      (while (not (setq ent_point (entsel "\nSélectionnez une polyligne ou appuyez sur ESC :"))))
      (setq ent (car ent_point))

      (setq vla_obj (vlax-ename->vla-object ent))

      ; Vérifiez que c'est une polyligne
      (if (eq (vla-get-ObjectName vla_obj) "AcDbPolyline")
        (progn
          (prompt "\nObjet sélectionné est une polyligne.")

          ; Demander à l'utilisateur de sélectionner un point de décalage
          (setq offset_point (getpoint "\nSélectionnez un point pour le décalage : "))

          ; Vérifiez la direction choisie et appliquez le décalage
          (if (= direction 1)
            (command "._offset" (rtos offset_value 2 4) ent offset_point) ; Décalage extérieur
            (command "._offset" (rtos (- offset_value) 2 4) ent offset_point) ; Décalage intérieur
          )

          ; Vérifiez si le décalage a été appliqué
          (princ "\nDécalage appliqué avec succès.")
        )
        (princ "\nL'objet sélectionné n'est pas une polyligne.")
      )
    )
    (princ "\nOpération annulée.")
  )
  (princ)
)

(princ "\nTapez OFFSETPOLY pour utiliser la commande.")
(princ)
