let saleItems = [];

document.addEventListener('DOMContentLoaded', function () {
    // Focus ที่ช่องสแกนบาร์โค้ด
    document.getElementById('barcodeInput').focus();

    // Event สำหรับสแกนบาร์โค้ด
    document.addEventListener('DOMContentLoaded', function () {
        // เพิ่ม event listener สำหรับสแกนบาร์โค้ด
        const barcodeInput = document.getElementById('barcodeInput');
        barcodeInput.focus();

        barcodeInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchProduct(this.value);
            }
        });

        // เพิ่ม event listener สำหรับปุ่มค้นหา
        document.getElementById('searchBtn').addEventListener('click', function () {
            const barcode = document.getElementById('barcodeInput').value;
            searchProduct(barcode);
        });
    });

    // Event สำหรับรับเงิน
    document.getElementById('receivedAmount').addEventListener('input', calculateChange);

    // Event สำหรับปุ่มชำระเงิน
    document.getElementById('checkoutBtn').addEventListener('click', processPayment);

    // Event สำหรับปุ่มยกเลิก
    document.getElementById('cancelBtn').addEventListener('click', cancelSale);
});

async function searchProduct(barcode) {
    if (!barcode) return;

    try {
        const response = await fetch(`/Sale/SearchProduct?barcode=${barcode}`);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }

        const result = await response.json();
        console.log('Search result:', result); // Debug

        if (result.success) {
            addItemToCart(result.data);
            // เคลียร์ค่าในช่องค้นหา
            document.getElementById('barcodeInput').value = '';
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('เกิดข้อผิดพลาดในการค้นหาสินค้า');
    }
}

async function addProductByBarcode(barcode) {
    try {
        const response = await fetch(`/Sale/GetProductByBarcode?barcode=${barcode}`);
        const result = await response.json();

        if (result.success) {
            addItemToCart(result.product);
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('เกิดข้อผิดพลาดในการค้นหาสินค้า');
    }
}

function addItemToCart(product) {
    // ตรวจสอบว่ามีสินค้าในตะกร้าแล้วหรือไม่
    const existingItem = saleItems.find(item => item.id === product.id);

    if (existingItem) {
        existingItem.quantity += 1;
        existingItem.total = existingItem.quantity * existingItem.price;
    } else {
        saleItems.push({
            id: product.id,
            barcode: product.barcode,
            name: product.name,
            price: product.price,
            quantity: 1,
            total: product.price
        });
    }

    renderCart();
    calculateTotal();
    playBeepSound();

    // Focus กลับไปที่ช่องบาร์โค้ด
    document.getElementById('barcodeInput').focus();
}

function renderCart() {
    const tbody = document.getElementById('saleItems');
    tbody.innerHTML = '';

    saleItems.forEach((item, index) => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${item.name}</td>
            <td class="text-end">${item.price.toFixed(2)}</td>
            <td>
                <input type="number" 
                       min="1" 
                       value="${item.quantity}" 
                       class="form-control form-control-sm text-center"
                       onchange="updateQuantity(${index}, this.value)">
            </td>
            <td class="text-end">${item.total.toFixed(2)}</td>
            <td>
                <button class="btn btn-sm btn-danger" onclick="removeItem(${index})">
                    ❌
                </button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}
function updateQuantity(index, quantity) {
    quantity = parseInt(quantity);
    if (quantity < 1) quantity = 1;

    saleItems[index].quantity = quantity;
    saleItems[index].total = quantity * saleItems[index].price;

    renderCart();
    calculateTotal();
}

function removeItem(index) {
    saleItems.splice(index, 1);
    renderCart();
    calculateTotal();
}

function calculateTotal() {
    const subtotal = saleItems.reduce((sum, item) => sum + item.total, 0);
    const vat = subtotal * 0.07;
    const total = subtotal + vat;

    document.getElementById('subtotal').textContent = subtotal.toFixed(2);
    document.getElementById('vat').textContent = vat.toFixed(2);
    document.getElementById('total').textContent = total.toFixed(2);

    calculateChange();
}

function calculateChange() {
    const total = parseFloat(document.getElementById('total').textContent);
    const received = parseFloat(document.getElementById('receivedAmount').value) || 0;
    const change = received - total;

    document.getElementById('changeAmount').value = change >= 0 ? change.toFixed(2) : '0.00';
    document.getElementById('checkoutBtn').disabled = change < 0;
}

function cancelSale() {
    if (saleItems.length > 0) {
        if (confirm('ต้องการยกเลิกรายการขายนี้?')) {
            saleItems = [];
            renderCart();
            calculateTotal();
            document.getElementById('receivedAmount').value = '';
            document.getElementById('changeAmount').value = '';
            document.getElementById('barcodeInput').focus();
        }
    }
}

function playBeepSound() {
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

$(document).ready(function () {
    loadCategories();  // โหลดหมวดหมู่ทันทีเมื่อเปิดหน้า
});

function loadCategories() {
    $.ajax({
        url: '/Sale/GetCategories',
        method: 'GET',
        success: function (response) {
            if (response.success) {
                // สร้าง HTML สำหรับปุ่มหมวดหมู่
                let html = response.categories.map(category => `
                    <button class="btn btn-outline-primary w-100 text-start mb-2" 
                            onclick="loadProductsByCategory(${category.id})">
                        <div class="d-flex justify-content-between align-items-center">
                            <span>${category.name}</span>
                            <span class="badge bg-primary rounded-pill">${category.productCount || 0}</span>
                        </div>
                    </button>
                `).join('');

                $('.d-flex.flex-column.gap-2').html(html);

                // โหลดสินค้าของหมวดหมู่แรก (ถ้ามี)
                if (response.categories.length > 0) {
                    loadProductsByCategory(response.categories[0].id);
                }
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading categories:', error);
        }
    });
}

function loadProductsByCategory(categoryId) {
    $.ajax({
        url: '/Sale/GetProductsByCategory',
        method: 'GET',
        data: { categoryId: categoryId },
        success: function (response) {
            if (response.success) {
                let html = `
                    <div class="row row-cols-2 row-cols-md-3 row-cols-lg-4 g-3">
                        ${response.products.map(product => `
                            <div class="col">
                                <div class="card h-100 shadow-sm">
                                    <div class="card-body">
                                        <h6 class="card-title">${product.name}</h6>
                                        <p class="card-text small text-muted">${product.barcode}</p>
                                        <div class="d-flex justify-content-between align-items-center">
                                            <h5 class="mb-0 text-primary">฿${product.price.toFixed(2)}</h5>
                                            <button class="btn btn-primary btn-sm" 
                                                    onclick="addToCart(${JSON.stringify(product)})">
                                                เพิ่ม
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                `;
                $('#categoryProducts').html(html);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading products:', error);
        }
    });
}