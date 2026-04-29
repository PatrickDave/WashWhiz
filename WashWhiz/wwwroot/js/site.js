// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function setRole(role) {
    // 1. Get the hidden input first to check if we are on the Login page
    const selectedRole = document.getElementById('selectedRole');

    // SAFETY CHECK: If this doesn't exist, stop immediately
    if (!selectedRole) return;

    // 2. Now get the rest of the elements
    const btnUser = document.getElementById('btnUser');
    const btnAdmin = document.getElementById('btnAdmin');
    const roleTitle = document.getElementById('roleTitle');
    const identityLabel = document.getElementById('identityLabel');
    const identityInput = document.getElementById('identityInput');
    const registerLink = document.getElementById('registerLink');

    // 3. Update the hidden value and Title
    selectedRole.value = role;
    if (roleTitle) roleTitle.innerText = role;

    // 4. Handle the Switch
    if (role === 'Admin') {
        // Update Buttons
        if (btnAdmin) btnAdmin.classList.add('active-role', 'shadow-sm');
        if (btnUser) btnUser.classList.remove('active-role', 'shadow-sm');

        // Update Input Field for Admin
        if (identityLabel) identityLabel.innerText = "Username";
        if (identityInput) {
            identityInput.name = "username";
            identityInput.type = "text";
            identityInput.placeholder = "Enter username";
        }

        // Hide Register Link
        if (registerLink) registerLink.style.display = "none";

    } else {
        // Update Buttons
        if (btnUser) btnUser.classList.add('active-role', 'shadow-sm');
        if (btnAdmin) btnAdmin.classList.remove('active-role', 'shadow-sm');

        // Update Input Field for User
        if (identityLabel) identityLabel.innerText = "Email Address";
        if (identityInput) {
            identityInput.name = "email";
            identityInput.type = "email";
            identityInput.placeholder = "Enter your email";
        }

        // Show Register Link
        if (registerLink) registerLink.style.display = "block";
    }
}