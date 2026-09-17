// Changing a selection only submits the preview handler; it never confirms a movement.
document.querySelectorAll('[data-lens-form]').forEach(form => {
    form.querySelectorAll('[data-selection]').forEach(select => {
        select.addEventListener('change', () => {
            if (select.dataset.selection === 'product') {
                const basis = form.querySelector('[name="Input.Base100"]');
                if (basis) basis.value = '';
            }
            if (select.dataset.selection !== 'stock') {
                const stock = form.querySelector('[name="Input.StockId"]');
                if (stock) stock.value = '';
            }
            form.requestSubmit();
        });
    });
});
