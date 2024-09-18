; additionne cotes sélectionnés
(defun c:LGI (/ CALC SEL SOM SOMF VAL VALF X)  
(setq sel (ssget '((0 . "DIMENSION")))
     x 0
     som 0
     somF 0
     calc 0)  
(repeat (sslength sel)
 (setq val  (cdr (assoc 42 (entget (ssname sel x))))
valF (atof (cdr (assoc 1 (entget (ssname sel x)))))
som (+ som val)		
)
 (if (not (equal valF 0.0))
   (setq somF (+ somF valF)
  calc (+ calc val))
   )
 (setq x (+ 1 x))
 )
 
 (alert (strcat "\n Addition des cotes Sélectionnées (réelle) : " (rtos som) " ."))
 (if (not (equal calc 0))(alert (strcat "\n Addition des cotes Sélectionnées (Marquée) : " (rtos (+ (- som calc) somF)) " .")))
	 
 (princ)
 )