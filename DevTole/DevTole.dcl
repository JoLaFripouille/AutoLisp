DevToleDialog : dialog {
  label = "Développeur de Tôlerie";
  : row {
    : column {
      : boxed_row {
        label = "Entrée des dimensions";
        : edit_box {
          key = "length";
          label = "Entrez la longueur :";
          edit_width = 25;
        }
        : edit_box {
          key = "repere";
          label = "Entrez le repère de la tôle :";
          edit_width = 25;
        }
        : edit_box {
          key = "epaisseur";
          label = "Entrez l'épaisseur :";
          edit_width = 25;
        }
      }
    }
  }
  ok_cancel;
}
