// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// ใส่ใน site.js หรือ sale.js
document.addEventListener('wheel', function(e) {
    if (!e.target.closest('.scrollable')) {
        e.preventDefault();
    }
}, { passive: false });