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

});

async function searchProduct(barcode) {
    if (!barcode) return;

    try {
        const response = await fetch(`/Sale/SearchProducts?searchTerm=${searchTerm || ''}`);
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
        dataType: 'json',
        success: function (response) {
            if (response.success) {
                const categoryList = document.getElementById('category-list');
                categoryList.innerHTML = response.categories.map(category => `
                    <button class="category-button rounded-pill border-0 w-100 text-start" 
                            onclick="loadProductsByCategory(${category.id})">
                        <span class="category-name">${category.name}</span>
                        <span class="badge rounded-pill category-count-badge">${category.productCount || 0}</span>
                    </button>
                `).join('');

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

function handleCategoryClick(categoryId) {
    // ลบ active state จากทุกปุ่ม
    $('.btn-outline-primary').removeClass('active');
    // เพิ่ม active state ให้ปุ่มที่ถูกคลิก
    $(`button[onclick*="${categoryId}"]`).addClass('active');
}


async function loadProductsByCategory(categoryId) {
    handleCategoryClick(categoryId);
    // setActiveCategory(categoryId);
    try {
        const loadingDiv = document.getElementById('loading');
        if (loadingDiv) loadingDiv.style.display = 'block';

        const response = await fetch(`/Sale/GetProductsByCategory/${categoryId}`);

        if (!response.ok) {
            throw new Error('เกิดข้อผิดพลาดในการโหลดข้อมูล: ' + response.status);
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || 'เกิดข้อผิดพลาดในการโหลดข้อมูล');
        }

        const categoryProducts = document.getElementById('categoryProducts');
        if (!categoryProducts) return;

        const productsHtml = result.products.map(product => `
            <div class="col">
                <div class="card border-0 shadow-sm product-card h-100 ${product.status === false ? 'opacity-75' : ''}"
                    style="transition: all 0.2s; cursor: pointer;"
                    onclick="${product.status === false ? 'void(0)' : `addToCart(${product.id})`}">
                    
                    <div class="position-relative">
                        ${product.imageBase64 ?
                `<img src="data:${product.imageType};base64,${product.imageBase64}"
                                    class="card-img-top p-2" 
                                    style="height: 140px; object-fit: contain;"
                                    alt="${product.name}">` :
                `<div class="d-flex align-items-center justify-content-center bg-light"
                                    style="height: 140px;">
                                    <i class="bi bi-image text-muted" style="font-size: 2rem;"></i>
                                </div>`
            }
                        ${product.status === false ?
                `<div class="position-absolute top-0 end-0 m-2">
                                <span class="badge bg-danger">ปิดการขายชั่วคราว</span>
                             </div>` :
                ''
            }
                    </div>
        
                    <div class="card-body p-2 d-flex flex-column justify-content-center align-items-center text-center">
                        <h6 class="card-title text-truncate mb-1 w-100" style="font-size: 0.9rem;">
                            ${product.name}
                        </h6>
                        <div class="pt-2">
                            <h6 class="mb-0" style="color: #4CAF50; font-size: 0.95rem;">
                                ฿${product.price.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </h6>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');

        categoryProducts.innerHTML = `
            <div class="row row-cols-4 row-cols-xl-5 row-cols-xxl-6 g-2">
                ${result.products.length === 0 ?
                '<div class="col-12 text-center p-3">ไม่พบสินค้าในหมวดหมู่นี้</div>' :
                productsHtml}
            </div>`;

    } catch (error) {
        console.error('เกิดข้อผิดพลาด:', error);
        const errorDiv = document.getElementById('error-message');
        if (errorDiv) {
            errorDiv.textContent = error.message;
            errorDiv.style.display = 'block';
        }
    } finally {
        const loadingDiv = document.getElementById('loading');
        if (loadingDiv) loadingDiv.style.display = 'none';
    }
}



// function displayProducts(products) {
//     const productsHtml = products.map(product => `
//         <div class="col">
//             <div class="card border-0 shadow-sm product-card h-100 ${product.status === false ? 'opacity-75' : ''}"
//                 style="transition: all 0.2s; cursor: pointer;"
//                 onclick="${product.status === false ? 'void(0)' : `addToCart(${product.id})`}">

//                 <div class="position-relative">
//                     ${product.imageBase64 ?
//                         `<img src="data:${product.imageType};base64,${product.imageBase64}"
//                                 class="card-img-top p-2" 
//                                 style="height: 140px; object-fit: contain;"
//                                 alt="${product.name}">` :
//                         `<div class="d-flex align-items-center justify-content-center bg-light"
//                                 style="height: 140px;">
//                                 <i class="bi bi-image text-muted" style="font-size: 2rem;"></i>
//                             </div>`
//                     }
//                     ${product.status === false ? 
//                         `<div class="position-absolute top-0 end-0 m-2">
//                             <span class="badge bg-danger">ปิดการขายชั่วคราว</span>
//                          </div>` : 
//                         ''
//                     }
//                 </div>

//                 <div class="card-body p-2 d-flex flex-column justify-content-center align-items-center text-center">
//                     <h6 class="card-title text-truncate mb-1 w-100" style="font-size: 0.9rem;">
//                         ${product.name}
//                     </h6>
//                     <div class="pt-2">
//                         <h6 class="mb-0" style="color: #4CAF50; font-size: 0.95rem;">
//                             ฿${product.price.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
//                         </h6>
//                     </div>
//                 </div>
//             </div>
//         </div>
//     `).join('');

//     return productsHtml;
// }

// // ใช้ฟังก์ชัน displayProducts ในการโหลดครั้งแรก
// $(document).ready(function () {
//     const initialProducts = Html.Raw(Json.Serialize(ViewBag.Products));
//     const productsContainer = document.getElementById('categoryProducts');
//     if (productsContainer && initialProducts) {
//         productsContainer.innerHTML = displayProducts(initialProducts);
//     }
// });


function clearError() {
    const errorDiv = document.getElementById('error-message');
    if (errorDiv) {
        errorDiv.textContent = '';
        errorDiv.style.display = 'none';
    }
}

function displayError(message) {
    const errorDiv = document.getElementById('error-message');
    if (errorDiv) {
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
    }
}


document.addEventListener('DOMContentLoaded', function() {
    const categoryList = document.querySelector('.category-list');
    
    // ตั้งค่า passive listeners เพื่อเพิ่มประสิทธิภาพ
    categoryList.addEventListener('touchstart', function() {}, { passive: true });
    categoryList.addEventListener('touchmove', function() {}, { passive: true });
    categoryList.addEventListener('touchend', function() {}, { passive: true });
    
    // ป้องกันการ scroll ของหน้าเว็บเมื่อ scroll ใน category list
    categoryList.addEventListener('touchmove', function(e) {
        if (this.scrollHeight > this.clientHeight) {
            e.stopPropagation();
        }
    }, { passive: false });
});

// function addToCart(productId) {
//     $.ajax({
//         url: '/Home/AddToCart',
//         type: 'POST',
//         data: { productId: productId },
//         success: function(response) {
//             if(response.success) {
//                 $("#cartCount").text(response.cartCount);
//                 Swal.fire({
//                     title: 'สำเร็จ',
//                     text: 'เพิ่มสินค้าลงตะกร้าเรียบร้อย',
//                     icon: 'success',
//                     timer: 1500,
//                     showConfirmButton: false
//                 });
//             }
//         }
//     });
// }


function addToCart(productId) {
    $.ajax({
        url: `/Sale/GetProductById`,
        type: 'GET',
        data: { id: productId },
        success: function (product) {
            const cartTableBody = document.getElementById('cartTableBody');

            // ตรวจสอบว่าสินค้านี้มีในตะกร้าแล้วหรือไม่
            const existingRow = document.querySelector(`tr[data-product-id="${productId}"]`);

            if (existingRow) {
                // เพิ่มจำนวน
                const quantityInput = existingRow.querySelector('input[type="number"]');
                quantityInput.value = parseInt(quantityInput.value) + 1;

                // อัพเดทยอดรวม
                updateTotal(existingRow);

                // เพิ่ม highlight
                existingRow.classList.remove('highlight-cart-row');
                void existingRow.offsetWidth;
                existingRow.classList.add('highlight-cart-row');

                // เลื่อนไปที่แถวนั้น
                existingRow.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

            } else {
                // สร้างแถวใหม่
                const newRow = document.createElement('tr');
                newRow.dataset.productId = productId;

                newRow.innerHTML = `
                    <td>${product.name}</td>
                    <td class="text-end">฿${product.price.toFixed(2)}</td>
                    <td class="text-center">
                        <input type="number" class="form-control form-control-sm text-center p-1" 
                            value="1" min="1" style="width: 60px;" 
                            onchange="updateQuantity(this, ${productId}, ${product.price})">
                    </td>
                    <td class="text-end">฿${product.price.toFixed(2)}</td>
                    <td class="text-center">
                        <button class="btn btn-sm text-danger" onclick="removeFromCart(${productId})">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                `;

                // เพิ่มแถวใหม่ที่บนสุด
                cartTableBody.insertBefore(newRow, cartTableBody.firstChild);

                // เพิ่ม highlight
                newRow.classList.add('highlight-cart-row');

                // เลื่อนไปบนสุด
                const container = cartTableBody.closest('.overflow-auto');
                if (container) {
                    container.scrollTop = 0;
                }
            }

            // อัพเดทยอดรวมทั้งหมด
            updateCartTotal();
        },
        error: function (xhr, status, error) {
            console.error('Error fetching product:', error);
            alert('ไม่สามารถเพิ่มสินค้าได้');
        }
    });
}

function updateTotal(row) {
    const price = parseFloat(row.cells[1].textContent.replace(/[^0-9.-]+/g, ''));
    const quantity = parseInt(row.querySelector('input[type="number"]').value);
    const total = price * quantity;
    row.querySelector('td:nth-child(4)').textContent = `฿${total.toFixed(2)}`;
    updateCartTotal();
}

function updateCartTotal() {
    const rows = document.querySelectorAll('#cartTableBody tr');
    let total = 0;

    // คำนวณยอดรวมจากทุกรายการ
    rows.forEach(row => {
        const priceText = row.querySelector('td:nth-child(4)').textContent; // ช่องราคารวม
        const price = parseFloat(priceText.replace(/[^0-9.-]+/g, '')); // แปลงเป็นตัวเลข
        if (!isNaN(price)) {
            total += price;
        }
    });

    // อัพเดทยอดรวมที่แสดงในบิล
    const formattedTotal = `฿${total.toFixed(2)}`;

    // อัพเดทที่หน้าบิล
    const cartTotalDisplay = document.querySelector('h4.mb-0');
    if (cartTotalDisplay) {
        cartTotalDisplay.textContent = formattedTotal;
    }
}

function updateQuantity(input, productId, price) {
    const row = input.closest('tr');
    const quantity = parseInt(input.value);

    if (quantity < 1) {
        input.value = 1;
        updateTotal(row);
    } else {
        updateTotal(row);
    }
}

function removeFromCart(productId) {
    const row = document.querySelector(`tr[data-product-id="${productId}"]`);
    if (row) {
        row.remove();
        updateCartTotal();
    }
}

$(document).ready(function () {
    // เรียก loadCategories เพื่อโหลดหมวดหมู่และสินค้าแรก
    loadCategories();
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

// function displayProducts(products) {
//     clearError();

//     const productList = document.getElementById('product-list');
//     productList.innerHTML = '';

//     if (!products || products.length === 0) {
//         productList.innerHTML = '<div class="no-products">ไม่พบสินค้าในหมวดหมู่นี้</div>';
//         return;
//     }

//     products.forEach(product => {
//         const productItem = document.createElement('div');
//         productItem.className = 'product-item';
//         productItem.innerHTML = `
//             <h3>${product.name}</h3>
//             <p>ราคา: ${product.price.toLocaleString()} บาท</p>
//         `;
//         productList.appendChild(productItem);
//     });
// }

function displayCategories(categories) {
    clearError();

    // เช็คว่ามี element นี้ใน HTML หรือไม่
    const categoryList = document.getElementById('category-list');
    if (!categoryList) {
        console.error('ไม่พบ element ที่มี id="category-list"');
        return;
    }

    categoryList.innerHTML = '';

    if (!categories || categories.length === 0) {
        categoryList.innerHTML = '<div class="no-categories">ไม่พบหมวดหมู่สินค้า</div>';
        return;
    }

    categories.forEach(category => {
        const button = document.createElement('button');
        button.className = 'category-button rounded-pill border-0 w-100 text-start';
        button.innerHTML = `
            <span class="category-name">${category.name}</span>
            <span class="badge rounded-pill category-count-badge">${category.productCount}</span>
        `;
        button.addEventListener('click', () => {
            loadProductsByCategory(category.id);
        });
        categoryList.appendChild(button);
    });
}


function clearError() {
    const errorDiv = document.getElementById('error-message');
    if (errorDiv) {
        errorDiv.textContent = '';
        errorDiv.style.display = 'none';
    }
}


// Helper function to show loading state
function showLoadingState() {
    const productContainer = document.getElementById('productContainer');
    if (productContainer) {
        productContainer.innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>';
    }
}

// Helper function to hide loading state
function hideLoadingState() {
    // Implementation depends on your loading UI
}

// // Helper function to display products
// function displayProducts(products) {
//     const productContainer = document.getElementById('productContainer');
//     if (!productContainer) return;

//     if (products.length === 0) {
//         productContainer.innerHTML = '<div class="alert alert-info">ไม่พบสินค้าในหมวดหมู่นี้</div>';
//         return;
//     }

//     const productsHTML = products.map(product => `
//         <div class="col-md-4 mb-4">
//             <div class="card h-100">
//                 <img src="${product.imageUrl}" class="card-img-top" alt="${product.name}">
//                 <div class="card-body">
//                     <h5 class="card-title">${product.name}</h5>
//                     <p class="card-text">${product.price.toLocaleString('th-TH', { style: 'currency', currency: 'THB' })}</p>
//                     <button onclick="addToCart(${product.id})" class="btn btn-primary">เพิ่มลงตะกร้า</button>
//                 </div>
//             </div>
//         </div>
//     `).join('');

//     productContainer.innerHTML = `<div class="row">${productsHTML}</div>`;
// }

// Helper function to update active category button
function updateActiveCategoryButton(categoryId) {
    // Remove active class from all buttons
    document.querySelectorAll('.category-button').forEach(button => {
        button.classList.remove('active');
    });

    // Add active class to selected button
    const activeButton = document.querySelector(`[onclick="loadProductsByCategory(${categoryId})"]`);
    if (activeButton) {
        activeButton.classList.add('active');
    }
}

// Helper function to show error message
function showErrorMessage(message) {
    const productContainer = document.getElementById('productContainer');
    if (productContainer) {
        productContainer.innerHTML = `<div class="alert alert-danger">${message}</div>`;
    }
}

function showPaymentMethods() {
    // แสดง modal หรือ drawer สำหรับเลือกวิธีการชำระเงิน
    $('#paymentMethodsModal').modal('show');
}

function updateDateTime() {
    const now = new Date();
    const options = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    document.getElementById('currentDateTime').textContent =
        now.toLocaleString('th-TH', options);
}

setInterval(updateDateTime, 1000);
updateDateTime();

// เรียกครั้งแรกทันทีเมื่อโหลดหน้า
document.addEventListener('DOMContentLoaded', updateDateTime);

$(document).ready(function () {
    $('#barcodeInput').keypress(function (e) {
        if (e.which == 13) {  // Enter key
            searchProducts();
        }
    });

    window.selectProduct = function (product) {
        addToCart(product);
        clearSearch();
    };

});
// เพิ่ม debounce function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

$(document).ready(function () {
    let currentIndex = -1;
    let searchResults = [];

    const debouncedSearch = debounce(function (value) {
        if (value.length >= 1) {
            $.get('/Sale/SearchProducts', { searchTerm: value })
                .done(function (response) {
                    if (response.success) {
                        searchResults = response.products || [];

                        if (searchResults.length === 0) {
                            $('#searchResults').html('<div class="alert alert-info">ไม่พบสินค้า</div>');
                            currentIndex = -1;
                            return;
                        }

                        if (searchResults.length === 1) {
                            addToCart(searchResults[0].id);
                            clearSearch();
                        } else {
                            currentIndex = -1;
                            showSearchResults(searchResults);
                        }
                    } else {
                        $('#searchResults').html(`<div class="alert alert-danger">${response.message || 'เกิดข้อผิดพลาดในการค้นหา'}</div>`);
                        currentIndex = -1;
                    }
                })
                .fail(function (jqXHR, textStatus, errorThrown) {
                    $('#searchResults').html(`<div class="alert alert-danger">เกิดข้อผิดพลาดในการเชื่อมต่อ: ${errorThrown || 'Unknown error'}</div>`);
                    currentIndex = -1;
                });
        } else {
            clearSearch();
        }
    }, 300);

    // แยกการจัดการ keydown เป็นฟังก์ชันใหม่
    function handleKeydown(e) {
        const items = $('.search-result-item');
        const totalItems = items.length;

        if (totalItems === 0) return;

        switch (e.key) {
            case 'ArrowUp':
                e.preventDefault();
                currentIndex = currentIndex <= 0 ? totalItems - 1 : currentIndex - 1;
                highlightItem(items);
                break;

            case 'ArrowDown':
                e.preventDefault();
                currentIndex = currentIndex >= totalItems - 1 ? 0 : currentIndex + 1;
                highlightItem(items);
                break;

            case 'Enter':
                e.preventDefault();
                if (currentIndex >= 0 && currentIndex < searchResults.length) {
                    selectAndAddToCart(searchResults[currentIndex].id, currentIndex);
                }
                break;

            case 'Escape':
                e.preventDefault();
                clearSearch();
                break;
        }
    }

    // เพิ่ม event listener สำหรับ keydown
    $('#barcodeInput').on('keydown', handleKeydown);

    $('#barcodeInput').on('input', function () {
        const value = $(this).val()?.trim() ?? "";
        debouncedSearch(value);
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('#searchContainer').length) {
            clearSearch();
        }
    });
});

function showSearchResults(products) {
    const html = products.map((product, index) => {
        return `
        <div class="search-result-item" data-index="${index}">
            <div class="d-flex align-items-center p-3">
                <div class="flex-grow-1">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="d-flex align-items-center gap-2">
                            <div class="fw-medium" style="color: #2E7D32;">
                                ${product.name}
                                ${!product.status ?
                '<span class="ms-2 badge bg-danger-subtle text-danger rounded-pill">ปิดขายชั่วคราว</span>' :
                ''}
                            </div>
                        </div>
                        <div class="fw-bold" style="color: #4CAF50;">
                            ฿${product.price.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </div>
                    </div>
                </div>
            </div>
        </div>
        `;
    }).join('');

    $('#searchResults').html(`
        <div class="search-results-container">
            ${html}
        </div>
    `);

    // Add click handlers
    $('.search-result-item').on('click', function () {
        const index = $(this).data('index');
        addToCart(searchResults[index].id);
        clearSearch();
    });
}

function highlightItem(items) {
    items.removeClass('active');
    const selectedItem = items.eq(currentIndex);
    if (selectedItem.length) {
        selectedItem.addClass('active');
        selectedItem[0].scrollIntoView({
            behavior: 'smooth',
            block: 'nearest'
        });
    }
}

function clearSearch() {
    $('#barcodeInput').val('');
    $('#searchResults').empty();
    currentIndex = -1;
    searchResults = [];
}

function selectAndAddToCart(productId, index) {
    addToCart(productId);
    clearSearch();
}

function handleMouseOver(index) {
    currentSelection = index;
    highlightSelection($('#searchResults .list-group-item'));
}
const style = `
            <style>
                #searchResults {
                    max-height: 300px;
                    overflow-y: auto;
                }
                .list-group-item.active {
                    background-color: #e9ecef;
                    border-color: #dee2e6;
                    color: #000;
                }
                .list-group-item:hover {
                    background-color: #e9ecef;
                }
            </style>
                `;
$('head').append(style);


let currentSelection = -1; // track selected item
// จัดการ keyboard events
$('#barcodeInput').on('keydown', function (e) {
    const results = $('#searchResults .list-group-item');
    const maxIndex = results.length - 1;

    switch (e.key) {
        case 'ArrowDown':
            e.preventDefault();
            if (results.length > 0) {
                currentSelection = Math.min(currentSelection + 1, maxIndex);
                highlightSelection(results);
            }
            break;

        case 'ArrowUp':
            e.preventDefault();
            if (results.length > 0) {
                currentSelection = Math.max(currentSelection - 1, 0);
                highlightSelection(results);
            }
            break;

        case 'Enter':
            e.preventDefault();
            if (currentSelection >= 0 && currentSelection <= maxIndex) {
                // เลือกสินค้าที่ highlight อยู่
                const selectedProduct = JSON.parse(
                    results.eq(currentSelection).attr('data-product')
                );
                selectProduct(selectedProduct);
            }
            break;

        case 'Escape':
            clearSearch();
            break;
    }
});

function highlightSelection(results) {
    results.removeClass('active');
    if (currentSelection >= 0) {
        const selected = results.eq(currentSelection);
        selected.addClass('active');
        // scroll if needed
        const container = $('#searchResults');
        const position = selected.position().top;
        const scrollTop = container.scrollTop();
        const containerHeight = container.height();

        if (position < 0) {
            container.scrollTop(scrollTop + position);
        } else if (position + selected.outerHeight() > containerHeight) {
            container.scrollTop(scrollTop + position - containerHeight + selected.outerHeight());
        }
    }
}

// อัพเดทยอดรวมของแต่ละรายการ
function updateRowTotal(productId) {
    const row = $(`#cart-row-${productId}`);
    const price = parseFloat(row.find('td:nth-child(2)').text());
    const qty = parseInt(row.find('.qty-input').val());
    const total = price * qty;
    row.find('.row-total').text(total.toFixed(2));
    updateCartTotal();
}

// ลบสินค้าออกจากตะกร้า
function removeFromCart(productId) {
    $(`#cart-row-${productId}`).remove();
    updateCartTotal();
}

// เคลียร์การค้นหา
function clearSearch() {
    $('#barcodeInput').val('').focus();
    $('#searchResults').empty();
}

// ป้องกันการ scroll ด้วย mouse wheel บนหน้าหลัก
document.addEventListener('wheel', function (e) {
    if (!e.target.closest('.scrollable')) {
        e.preventDefault();
    }
}, { passive: false });

// ป้องกันการ scroll ด้วย spacebar
document.addEventListener('keydown', function (e) {
    if (e.code === 'Space' && !e.target.closest('.scrollable')) {
        e.preventDefault();
    }
});
