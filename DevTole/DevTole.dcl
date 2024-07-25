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
        }
    }
    ok_cancel;
}
