(setq *console-history* "")  ;; Initialiser la variable globale pour l'historique de la console

(defun AddToConsoleHistory (msg)
  (setq *console-history* (strcat *console-history* msg "\n"))
  (princ msg)  ;; Afficher le message dans la console
)

(defun c:CaptureConsoleHistory ()
  ;; Démonstration de capture de messages importants
  (AddToConsoleHistory "--- Début de l'historique de la console ---")
  
  ;; Exemple de commandes capturées
  (AddToConsoleHistory "Commande 1 : Initialisation")
  (AddToConsoleHistory "Commande 2 : Charger fichier LISP")
  (AddToConsoleHistory "Commande 3 : Sélection de l'objet")
  (AddToConsoleHistory "Commande 4 : Calcul de la longueur")
  (AddToConsoleHistory "Commande 5 : Création du rectangle")
  (AddToConsoleHistory "Commande 6 : Création du bloc")
  (AddToConsoleHistory "Commande 7 : Affichage des résultats")
  
  (AddToConsoleHistory "--- Fin de l'historique ---")
  
  ;; Afficher l'historique dans la console
  (princ *console-history*)
  
  ;; Écrire l'historique dans un fichier
  (setq filename (getfiled "Enregistrer l'historique de la console sous" "console_history.txt" "txt" 1))
  (if filename
    (progn
      (setq file (open filename "w"))
      (write-line *console-history* file)
      (close file)
      (princ (strcat "\nHistorique de la console enregistré dans " filename))
    )
    (princ "\nOpération annulée par l'utilisateur.")
  )
  (princ)
)

(princ "\nEntrez 'CaptureConsoleHistory' pour capturer et afficher l'historique des commandes.\n")
(princ)
