DevToleDialog : dialog {
    label = "Créer Tôle";
    : row {
        : column {
            : edit_box {
                key = "length";
                label = "Entrer la longueur :";
                value = "0";
            }
            : edit_box {
                key = "repere";
                label = "Entrer le Repère de la tôle :";
                value = "";
            }
        }
    }
    ok_cancel;
}
