/* CP Analítica - modo claro / oscuro.
   La preferencia se guarda en localStorage ("cp-theme": "light" | "dark").
   El tema inicial se aplica con el snippet de cada <head> (antes de
   pintar la página, para que no haya un parpadeo); este archivo solo
   conecta los botones [data-cp-theme-toggle]. Sin JavaScript la página
   queda en modo oscuro, que es el predeterminado. */
(function () {
    var CLAVE = 'cp-theme';
    var raiz = document.documentElement;

    function temaActual() {
        return raiz.getAttribute('data-theme') === 'light' ? 'light' : 'dark';
    }

    function aplicar(tema) {
        raiz.setAttribute('data-theme', tema);
        var botones = document.querySelectorAll('[data-cp-theme-toggle]');
        for (var i = 0; i < botones.length; i++) {
            botones[i].setAttribute('aria-pressed', tema === 'light' ? 'true' : 'false');
            botones[i].setAttribute('title', tema === 'light' ? 'Cambiar a modo oscuro' : 'Cambiar a modo claro');
        }
    }

    function alternar() {
        var nuevo = temaActual() === 'light' ? 'dark' : 'light';
        aplicar(nuevo);
        try { localStorage.setItem(CLAVE, nuevo); } catch (e) { /* almacenamiento bloqueado: el cambio vale solo para esta página */ }
    }

    aplicar(temaActual());

    var botones = document.querySelectorAll('[data-cp-theme-toggle]');
    for (var i = 0; i < botones.length; i++) {
        botones[i].addEventListener('click', alternar);
    }
})();
