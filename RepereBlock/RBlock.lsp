Les raccourcis clavier vont changer … Le jeudi 1 août 2024, les raccourcis clavier de Drive seront modifiés pour que vous puissiez naviguer avec les premières lettresEn savoir plus
(defun c:RBlock ( / ent blkname p1 p2)
  (princ "\nDébogage : Démarrage de la commande RBlock")
  ;; Sélectionner un bloc
  (setq ent (car (entsel "\nSélectionnez un bloc: ")))
  (if (and ent (eq (cdr (assoc 0 (entget ent))) "INSERT"))
    (progn
      (princ "\nDébogage : Bloc sélectionné.")
      ;; Obtenir le nom du bloc
      (setq blkname (cdr (assoc 2 (entget ent))))
      (princ (strcat "\nDébogage : Nom du bloc : " blkname))
      ;; Demander à l'utilisateur de spécifier le premier point d'attache de la ligne de repère
      (setq p1 (getpoint "\nSpécifiez l'emplacement de la pointe de flèche de la ligne de repère: "))
      (princ (strcat "\nDébogage : Premier point d'attache : " (rtos (car p1) 2 2) ", " (rtos (cadr p1) 2 2)))
      ;; Demander à l'utilisateur de spécifier le second point d'attache de la ligne de repère
      (setq p2 (getpoint "\nSpécifiez l'emplacement de la ligne de guidage de la ligne de repère: "))
      (princ (strcat "\nDébogage : Second point d'attache : " (rtos (car p2) 2 2) ", " (rtos (cadr p2) 2 2)))
      ;; Créer la ligne de repère avec le nom du bloc comme annotation
      (command "_.MLEADER" p1 p2 blkname )
      (princ "\nDébogage : Ligne de repère créée avec succès.")
    )
    (prompt "\nDébogage : Veuillez sélectionner un bloc valide.")
  )
  (princ "\nDébogage : Fin de la commande RBlock")
  (princ)
)

(princ "\nChargé avec succès. Utilisez la commande RBlock pour ajouter une ligne de repère avec le nom du bloc.\n")
(princ)