DevToleDialog : dialog {
    label = "Créer Tôle";
    : row {
        : column {
            : edit_box {
                key = "length";
                label = "Entrer la longueur :";
                value = "--";
            }
            : edit_box {
                key = "repere";
                label = "Entrez le repère de la tôle :";
                value = "";
            }
            : edit_box {
                key = "epaisseur";
                label = "Entrer l'épaisseur de la tôle :";
                value = "2";
            }
        }
    }
    ok_cancel;
}

