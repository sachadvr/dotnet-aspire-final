window.performLogout = function() {
    const form = document.getElementById('logoutForm');
    if (!form) {
        // Fallback : navigation directe
        window.location.replace('/logout');
        return;
    }
    
    // Récupérer le token antiforgery
    const antiforgeryToken = form.querySelector('input[name="__RequestVerificationToken"]');
    const token = antiforgeryToken ? antiforgeryToken.value : '';
    
    // Faire une requête POST avec fetch
    fetch('/logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: new URLSearchParams({
            '__RequestVerificationToken': token
        }),
        credentials: 'include'
    })
    .then(response => {
        // Après la déconnexion, forcer une navigation complète
        // Utiliser replace pour éviter que Blazor ne tente de se reconnecter
        window.location.replace('/');
    })
    .catch(error => {
        console.error('Erreur lors de la déconnexion:', error);
        // En cas d'erreur, rediriger quand même
        window.location.replace('/');
    });
};
