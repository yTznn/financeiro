// wwwroot/js/alertas.js
// Carrega depois do SweetAlert2 (temos que ter Swal no window)

// Alerta "padrão" (verde/azul dependendo do sucesso)
window.SwalDefault = Swal.mixin({
    buttonsStyling: true,
    confirmButtonColor: '#0d6efd',   // azul Bootstrap
    cancelButtonColor:  '#6c757d',
    timer: 2000,                    // 2 s; remova se não quiser fechar sozinho
    timerProgressBar: true
});

// Alerta "delete / ação perigosa"
window.SwalDelete = Swal.mixin({
    buttonsStyling: true,
    icon: 'warning',
    confirmButtonColor: '#d33',     // vermelho
    cancelButtonColor:  '#6c757d'
});