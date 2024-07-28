offset_dialog : dialog {
    label = "Décalage de la Polyligne";
    : row {
        : boxed_column {
            : edit_box {
                key = "offset_value";
                label = "Épaisseur du Décalage :";
                edit_width = 10;
            }
            : radio_column {
                key = "direction";
                label = "Direction du Décalage :";
                : radio_button {
                    key = "interior";
                    label = "Intérieur";
                    value = "1";
                }
                : radio_button {
                    key = "exterior";
                    label = "Extérieur";
                    value = "-1";
                }
            }
        }
    }
    ok_cancel;
}
