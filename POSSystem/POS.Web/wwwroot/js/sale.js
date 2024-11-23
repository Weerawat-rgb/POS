class POSManager {
    constructor() {
        this.items = [];
        this.initializeEventListeners();
        this.loadProducts();
    }

    initializeEventListeners() {
        $('#barcodeInput').on('keypress', (e) => {
            if (e.which === 13) {
                this.addProductByBarcode($(e.target).val());
                $(e.target).val('').focus();
            }
        });

        $('#received').on('input', () => this.calculateChange());
        $('#paymentBtn').on('click', () => this.processSale());
        $('#cancelBtn').on('click', () => this.clearSale());

        $(document).on('keydown', (e) => {
            if (e.key === 'F4') {
                e.preventDefault();
                this.clearSale();
            }
            if (e.key === 'F5') {
                e.preventDefault();
                this.processSale();
            }
        });
    }

    async loadProducts() {
        try {
            const response = await fetch('/Sale/GetProducts');
            const products = await response.json();
            this.renderProductGrid(products);
        } catch (error) {
            console.error('Error loading products:', error);
        }
    }

    renderProductGrid(products) {
        const grid = $('#productGrid');
        grid.empty();

        products.forEach(product => {
            grid.append(`
                <div class="col-md-3 mb-3">
                    <div class="card product-card" onclick="posManager.addProductById(${product.id})">
                        <div class="card-body text-center">
                            <h5 class="card-title mb-2">${product.name}</h5>
                            <p class="card-text text-primary mb-0">
                                <strong>${product.price.toFixed(2)}</strong> บาท
                            </p>
                            <small class="text-muted">คงเหลือ: ${product.stock}</small>
                        </div>
                    </div>
                </div>
            `);
        });
    }

    async addProductByBarcode(barcode) {
        try {
            const response = await fetch(`/Sale/GetProductByBarcode?barcode=${barcode}`);
            if (!response.ok) {
                alert('ไม่พบสินค้า');
                return;
            }

            const product = await response.json();
            this.addItemToCart(product);
        } catch (error) {
            console.error('Error:', error);
            alert('เกิดข้อผิดพลาด');
        }
    }

    addItemToCart(product) {
        const existingItem = this.items.find(item => item.id === product.id);

        if (existingItem) {
            existingItem.quantity += 1;
            existingItem.total = existingItem.quantity * existingItem.price;
        } else {
            this.items.push({
                id: product.id,
                name: product.name,
                price: product.price,
                quantity: 1,
                total: product.price
            });
        }

        this.renderCart();
        this.updateTotals();
        this.playBeepSound();
    }

    renderCart() {
        const tbody = $('#saleItems');
        tbody.empty();

        this.items.forEach((item, index) => {
            tbody.append(`
                <tr>
                    <td>${item.name}</td>
                    <td class="text-end">${item.price.toFixed(2)}</td>
                    <td class="text-center">
                        <input type="number" class="quantity-input form-control form-control-sm"
                               value="${item.quantity}" min="1"
                               onchange="posManager.updateQuantity(${index}, this.value)">
                    </td>
                    <td class="text-end">${item.total.toFixed(2)}</td>
                    <td class="text-center">
                        <span class="remove-item" onclick="posManager.removeItem(${index})">
                            ❌
                        </span>
                    </td>
                </tr>
            `);
        });
    }

    updateQuantity(index, quantity) {
        quantity = parseInt(quantity);
        if (quantity < 1) quantity = 1;

        this.items[index].quantity = quantity;
        this.items[index].total = this.items[index].price * quantity;

        this.renderCart();
        this.updateTotals();
    }

    removeItem(index) {
        this.items.splice(index, 1);
        this.renderCart();
        this.updateTotals();
    }

    updateTotals() {
        const subtotal = this.items.reduce((sum, item) => sum + item.total, 0);
        const vat = subtotal * 0.07;
        const total = subtotal + vat;

        $('#subtotal').text(subtotal.toFixed(2));
        $('#vat').text(vat.toFixed(2));
        $('#total').text(total.toFixed(2));

        this.calculateChange();
    }

    calculateChange() {
        const total = parseFloat($('#total').text()) || 0;
        const received = parseFloat($('#received').val()) || 0;
        const change = received - total;

        $('#change').val(change >= 0 ? change.toFixed(2) : '0.00');
        $('#paymentBtn').prop('disabled', change < 0);
    }

    playBeepSound() {
        // สร้างเสียง beep
        const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioCtx.createOscillator();
        const gainNode = audioCtx.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioCtx.destination);

        oscillator.type = 'sine';
        oscillator.frequency.value = 800;
        gainNode.gain.value = 0.1;

        oscillator.start();
        setTimeout(() => oscillator.stop(), 100);
    }

    clearSale() {
        this.items = [];
        this.renderCart();
        this.updateTotals();
        $('#received').val('');
        $('#change').val('');
        $('#barcodeInput').focus();
    }


    async processSale() {
        if (this.items.length === 0) {
            alert('กรุณาเพิ่มสินค้า');
            return;
        }

        const total = parseFloat($('#total').text());
        const received = parseFloat($('#received').val()) || 0;

        if (received < total) {
            alert('จำนวนเงินไม่พอ');
            return;
        }

        const sale = {
            total: parseFloat($('#subtotal').text()),
            vat: parseFloat($('#vat').text()),
            grandTotal: total,
            paymentMethod: 'CASH',
            saleDetails: this.items.map(item => ({
                productId: item.id,
                quantity: item.quantity,
                unitPrice: item.price,
                total: item.total
            }))
        };

        try {
            const response = await fetch('/Sale/ProcessSale', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(sale)
            });

            const result = await response.json();

            if (result.success) {
                const change = parseFloat($('#change').val());

                alert(`ขายสำเร็จ!\n\nยอดรวม: ${total.toFixed(2)} บาท\nรับเงิน: ${received.toFixed(2)} บาท\nเงินทอน: ${change.toFixed(2)} บาท`);

                // เปิดหน้าพิมพ์ใบเสร็จ
                if (confirm('ต้องการพิมพ์ใบเสร็จหรือไม่?')) {
                    window.open(`/Sale/PrintReceipt/${result.saleId}`, '_blank');
                }

                // เคลียร์รายการขาย
                this.clearSale();
            } else {
                alert('เกิดข้อผิดพลาด: ' + result.message);
            }

        } catch (error) {
            console.error('Error:', error);
            alert('ไม่สามารถบันทึกการขายได้');
        }
    }

    // เพิ่มฟังก์ชันสำหรับเสียง beep
    playBeepSound() {
        try {
            const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioCtx.createOscillator();
            const gainNode = audioCtx.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioCtx.destination);

            oscillator.type = 'sine';
            oscillator.frequency.value = 800;
            gainNode.gain.value = 0.1;

            oscillator.start();
            setTimeout(() => {
                oscillator.stop();
                audioCtx.close();
            }, 100);
        } catch (error) {
            console.error('Error playing beep:', error);
        }
    }

    // เพิ่มฟังก์ชันคีย์ลัด
    initializeKeyboardShortcuts() {
        $(document).on('keydown', (e) => {
            // F4 = ยกเลิก
            if (e.key === 'F4') {
                e.preventDefault();
                this.clearSale();
            }
            // F5 = ชำระเงิน
            if (e.key === 'F5') {
                e.preventDefault();
                this.processSale();
            }
            // Enter ที่ช่องรับเงิน = ชำระเงิน
            if (e.key === 'Enter' && document.activeElement.id === 'received') {
                e.preventDefault();
                this.processSale();
            }
        });
    }}