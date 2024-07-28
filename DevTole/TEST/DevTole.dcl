DevToleDialog : dialog {
    label = "Creer Tole";
    : row {
        : column {
            : edit_box {
                key = "length";
                label = "Entrer la longueur :";
                value = "0";
            }
            : edit_box {
                key = "repere";
                label = "Entrer le Repere de la tole :";
                value = "";
            }
            : edit_box {
                key = "epaisseur";
                label = "Entrer l'epaisseur de la tole :";
                value = "0";
            }
        }
    }
    ok_cancel;
}
