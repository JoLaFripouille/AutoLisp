(defun c:FindIntersectionAndDraw (/ ent1 ent2 pt1 pt2 pt3 pt4 vec1 vec2 dot-product mag-v1 mag-v2 cos-theta angle angle-degrees p1 p2 p3 p4 p_int length pt_new dx dy start_x)
  ;; Fonction pour calculer l'angle entre deux vecteurs
  (defun angle-between-vectors (v1 v2)
    (setq dot-product (+ (* (car v1) (car v2)) (* (cadr v1) (cadr v2))))
    (setq mag-v1 (sqrt (+ (* (car v1) (car v1)) (* (cadr v1) (cadr v1)))))
    (setq mag-v2 (sqrt (+ (* (car v2) (car v2)) (* (cadr v2) (cadr v2)))))
    (setq cos-theta (/ dot-product (* mag-v1 mag-v2)))
    ;; Calcul de l'angle en radians à partir du cosinus
    (atan (sqrt (- 1 (* cos-theta cos-theta))) cos-theta)
  )

  (prompt "\nSélectionnez la première ligne (horizontale) : ")
  (setq ent1 (car (entsel "\nSélectionnez la ligne horizontale : ")))
  
  (if (and ent1 (= (cdr (assoc 0 (entget ent1))) "LINE"))
    (progn
      (setq pt1 (cdr (assoc 10 (entget ent1))))
      (setq pt2 (cdr (assoc 11 (entget ent1))))
      
      ;; Vérification que la ligne est horizontale
      (if (/= (cadr pt1) (cadr pt2))
        (prompt "\nLa première ligne sélectionnée n'est pas horizontale. Abandon.")
        (progn
          (prompt "\nSélectionnez la seconde ligne (en angle) : ")
          (setq ent2 (car (entsel "\nSélectionnez la ligne en angle : ")))
          
          (if (and ent2 (= (cdr (assoc 0 (entget ent2))) "LINE"))
            (progn
              (setq pt3 (cdr (assoc 10 (entget ent2))))
              (setq pt4 (cdr (assoc 11 (entget ent2))))
              
              ;; Calcul des vecteurs
              (setq vec1 (list (- (car pt2) (car pt1)) (- (cadr pt2) (cadr pt1))))
              (setq vec2 (list (- (car pt4) (car pt3)) (- (cadr pt4) (cadr pt3))))
              
              ;; Calcul de l'angle entre les deux vecteurs
              (setq angle (angle-between-vectors vec1 vec2))
              
              ;; Conversion de l'angle en degrés
              (setq angle-degrees (* angle (/ 180.0 pi)))
              
              ;; Correction de l'angle
              (setq angle-degrees (- 180 angle-degrees))
              
              ;; Affichage de l'angle avec une précision de 4 chiffres après la virgule
              (princ (strcat "\nL'angle entre les deux lignes est de " (rtos angle-degrees 2 4) " degrés."))
              
              ;; Créer un raccord entre les deux lignes avec un rayon de 0 pour trouver le point d'intersection
              (command "._fillet" "_radius" "0")
              (command "._fillet" ent1 ent2)

              ;; Obtenir le point d'intersection après le raccord
              (setq p1 (cdr (assoc 10 (entget ent1))))
              (setq p2 (cdr (assoc 11 (entget ent1))))
              (setq p3 (cdr (assoc 10 (entget ent2))))
              (setq p4 (cdr (assoc 11 (entget ent2))))
              (setq p_int (inters p1 p2 p3 p4 T))

              (if p_int
                (progn
                  (princ (strcat "\nLe point d'intersection est: " (rtos (car p_int) 2 4) ", " (rtos (cadr p_int) 2 4)))
                  ;; Demander la longueur de la nouvelle ligne
                  (setq length (getdist "\nEntrez la longueur de la nouvelle ligne perpendiculaire : "))
                  
                  ;; Calculer les composants en X et Y de la nouvelle ligne
                  (setq tan-angle (/ (sin angle) (cos angle)))  ; Calculer la tangente de l'angle
                  (setq dx (/ length tan-angle))
                  (setq dy length)
                  
                  ;; Calculer le point de départ et le point final de la ligne perpendiculaire
                  (setq start_x (+ (car p_int) dx))
                  (setq pt_new (list start_x (+ (cadr p_int) dy)))
                  
                  ;; Dessiner la ligne perpendiculaire
                  (entmakex (list (cons 0 "LINE") (cons 10 (list start_x (cadr p_int))) (cons 11 pt_new)))
                  
                  ;; Afficher les coordonnées du point de départ et du point final
                  (princ (strcat "\nCoordonnées du point de départ (x, y) : " (rtos start_x 2 4) ", " (rtos (cadr p_int) 2 4)))
                  (princ (strcat "\nCoordonnées du point final (x, y) : " (rtos (car pt_new) 2 4) ", " (rtos (cadr pt_new) 2 4)))
                )
                (prompt "\nLes lignes ne se croisent pas même après le raccord.")
              )
            )
            (prompt "\nLa seconde sélection n'est pas une ligne.")
          )
        )
      )
    )
    (prompt "\nLa première sélection n'est pas une ligne.")
  )
  (princ)
)

(princ "\nTypez 'FindIntersectionAndDraw' pour exécuter le script.")
(princ)
