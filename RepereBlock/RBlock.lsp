
(defun c:RBlock 
    ( / ent blkname p1 p2 isDynamicBlock blockObj blockName)
    (princ "\nDébogage : Démarrage de la commande RBlock")

;;Sélectionnerunbloc
    (setq ent 
        (car 
            (entsel "\nSélectionnez un bloc: ")))
    (if 
        (and ent 
            (eq 
                (cdr 
                    (assoc 0 
                        (entget ent))) "INSERT"))
        (progn
            (princ "\nDébogage : Bloc sélectionné.")

;;Obtenirl'objetdubloc
            (setq blockObj 
                (vlax-ename->vla-object ent))

;;Vérifiersileblocestdynamique
            (setq isDynamicBlock 
                (vlax-get-property blockObj 'IsDynamicBlock))
            (if isDynamicBlock
                (progn
                    (princ "\nDébogage : Le bloc est dynamique.")
;;Obtenirlenomréeldublocdynamique
                    (setq blockName 
                        (vla-get-EffectiveName blockObj))
)
                (progn
                    (princ "\nDébogage : Le bloc n'est pas dynamique.")
;;Obtenirlenomdublocnormal
                    (setq blockName 
                        (cdr 
                            (assoc 2 
                                (entget ent))))
)
)

            (princ 
                (strcat "\nDébogage : Nom du bloc : " blockName))

;;Demanderàl'utilisateurdespécifierlepremierpointd'attachedelalignederepère
            (setq p1 
                (getpoint "\nSpécifiez l'emplacement de la pointe de flèche de la ligne de repère: "))
            (if p1
                (princ 
                    (strcat "\nDébogage : Premier point d'attache : " 
                        (rtos 
                            (car p1) 2 2) ", " 
                        (rtos 
                            (cadr p1) 2 2)))
                (progn
                    (princ "\nDébogage : Premier point d'attache non spécifié.")
                    (exit)
)
)

;;Demanderàl'utilisateurdespécifierlesecondpointd'attachedelalignederepère
            (setq p2 
                (getpoint "\nSpécifiez l'emplacement de la ligne de guidage de la ligne de repère: "))
            (if p2
                (princ 
                    (strcat "\nDébogage : Second point d'attache : " 
                        (rtos 
                            (car p2) 2 2) ", " 
                        (rtos 
                            (cadr p2) 2 2)))
                (progn
                    (princ "\nDébogage : Second point d'attache non spécifié.")
                    (exit)
)
)

;;Créerlalignederepèreaveclenomdubloccommeannotation
            (command "_.MLEADER" p1 p2 blockName)
            (princ "\nDébogage : Ligne de repère créée avec succès.")

)
        (princ "\nDébogage : Veuillez sélectionner un bloc valide.")
)
    (princ "\nDébogage : Fin de la commande RBlock")
    (princ)
)

(princ "\nChargé avec succès. Utilisez la commande RBlock pour ajouter une ligne de repère avec le nom du bloc.\n")
(princ)
