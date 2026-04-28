/**
 * КУЛЬТУРНОЕ НАСЛЕДИЕ БЕЛАРУСИ - Main JavaScript
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize AOS
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 500,
            easing: 'ease-out',
            once: true,
            offset: 200,
            delay: 0,
            anchorPlacement: 'top-bottom',
            disableMutationObserver: false
        });

        // Recalculate trigger points after fonts/images load (geometry may shift)
        window.addEventListener('load', () => AOS.refresh());
    }

    // Initialize Bootstrap tooltips
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(el => new bootstrap.Tooltip(el));

    // Navbar scroll effect
    initNavbarScroll();

    // Counter animation
    initCounterAnimation();

    // Initialize Swiper carousels
    initSwiperCarousels();

    // Favorite toggle
    initFavoriteToggle();

    // Search autocomplete
    initSearchAutocomplete();

    // Language switcher
    initLanguageSwitcher();
});

/**
 * Global toast helper (Bootstrap toast in _Layout)
 */
window.showToast = function(message, type = 'info') {
    const toastEl = document.getElementById('siteToast');
    const bodyEl = document.getElementById('siteToastBody');
    if (!toastEl || !bodyEl || typeof bootstrap === 'undefined') {
        // Fallback
        console.log(`[${type}]`, message);
        return;
    }

    bodyEl.textContent = message;

    // Map to bootstrap bg classes
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'bg-secondary');
    const bg = type === 'success' ? 'bg-success'
        : type === 'danger' || type === 'error' ? 'bg-danger'
        : type === 'warning' ? 'bg-warning'
        : type === 'info' ? 'bg-info'
        : 'bg-secondary';

    toastEl.classList.add(bg);

    // Ensure readable text for warning/info
    toastEl.classList.toggle('text-dark', bg === 'bg-warning' || bg === 'bg-info');
    toastEl.classList.toggle('text-white', !(bg === 'bg-warning' || bg === 'bg-info'));

    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2500 });
    toast.show();
};

/**
 * Toast with action button (uses #siteToast from _Layout)
 */
window.showToastAction = function(message, actionLabel, actionHref, type = 'info') {
    const toastEl = document.getElementById('siteToast');
    const bodyEl = document.getElementById('siteToastBody');
    if (!toastEl || !bodyEl || typeof bootstrap === 'undefined') {
        console.log(`[${type}]`, message, actionLabel ? `(${actionLabel}: ${actionHref})` : '');
        return;
    }

    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'bg-secondary');
    const bg = type === 'success' ? 'bg-success'
        : type === 'danger' || type === 'error' ? 'bg-danger'
        : type === 'warning' ? 'bg-warning'
        : type === 'info' ? 'bg-info'
        : 'bg-secondary';
    toastEl.classList.add(bg);

    toastEl.classList.toggle('text-dark', bg === 'bg-warning' || bg === 'bg-info');
    toastEl.classList.toggle('text-white', !(bg === 'bg-warning' || bg === 'bg-info'));

    const safeMessage = String(message ?? '');
    const safeLabel = String(actionLabel ?? '');
    const safeHref = String(actionHref ?? '');

    if (safeLabel && safeHref) {
        bodyEl.innerHTML = `
            <div class="d-flex align-items-center gap-2">
                <span>${escapeHtml(safeMessage)}</span>
                <a class="btn btn-sm btn-light ms-auto" href="${escapeAttr(safeHref)}">${escapeHtml(safeLabel)}</a>
            </div>
        `;
    } else {
        bodyEl.textContent = safeMessage;
    }

    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 3500 });
    toast.show();

    function escapeHtml(s) {
        return s.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
    function escapeAttr(s) {
        return escapeHtml(s).replace(/`/g, '&#96;');
    }
};

window.showConfirmModal = function(options = {}) {
    const modalEl = document.getElementById('siteConfirmModal');
    const titleEl = document.getElementById('siteConfirmTitle');
    const bodyEl = document.getElementById('siteConfirmBody');
    const okBtn = document.getElementById('siteConfirmOkBtn');
    const cancelBtn = document.getElementById('siteConfirmCancelBtn');
    if (!modalEl || !titleEl || !bodyEl || !okBtn || !cancelBtn || typeof bootstrap === 'undefined') {
        const fallbackMessage = options.message || '';
        return Promise.resolve(window.confirm(fallbackMessage));
    }

    const {
        title = 'Подтверждение',
        message = '',
        confirmText = 'Подтвердить',
        cancelText = 'Отмена',
        confirmClass = 'btn-primary'
    } = options;

    titleEl.textContent = title;
    bodyEl.textContent = message;
    okBtn.textContent = confirmText;
    cancelBtn.textContent = cancelText;
    okBtn.className = `btn ${confirmClass}`;

    return new Promise((resolve) => {
        const instance = bootstrap.Modal.getOrCreateInstance(modalEl);
        let settled = false;

        const cleanup = () => {
            modalEl.removeEventListener('hidden.bs.modal', handleHidden);
            okBtn.removeEventListener('click', handleConfirm);
        };

        const handleHidden = () => {
            if (!settled) {
                settled = true;
                cleanup();
                resolve(false);
            }
        };

        const handleConfirm = () => {
            if (settled) return;
            settled = true;
            cleanup();
            resolve(true);
            instance.hide();
        };

        modalEl.addEventListener('hidden.bs.modal', handleHidden, { once: true });
        okBtn.addEventListener('click', handleConfirm);
        instance.show();
    });
};

/**
 * Navbar scroll effect
 */
function initNavbarScroll() {
    const header = document.querySelector('.site-header');
    let lastScroll = 0;

    window.addEventListener('scroll', () => {
        const currentScroll = window.pageYOffset;

        if (currentScroll > 100) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }

        lastScroll = currentScroll;
    });
}

/**
 * Counter animation using Intersection Observer
 */
function initCounterAnimation() {
    const counters = document.querySelectorAll('.stat-number[data-count]');

    if (!counters.length) return;

    const animateCounter = (counter) => {
        const target = parseInt(counter.dataset.count);
        const duration = 2000;
        const step = target / (duration / 16);
        let current = 0;

        const updateCounter = () => {
            current += step;
            if (current < target) {
                counter.textContent = Math.floor(current).toLocaleString();
                requestAnimationFrame(updateCounter);
            } else {
                counter.textContent = target.toLocaleString();
            }
        };

        updateCounter();
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                animateCounter(entry.target);
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.5 });

    counters.forEach(counter => observer.observe(counter));
}

/**
 * Initialize Swiper carousels
 */
function initSwiperCarousels() {
    // Featured objects carousel
    const featuredSwiper = new Swiper('.featured-carousel .swiper', {
        slidesPerView: 1,
        spaceBetween: 20,
        loop: true,
        autoplay: {
            delay: 5000,
            disableOnInteraction: false
        },
        pagination: {
            el: '.featured-carousel .swiper-pagination',
            clickable: true
        },
        navigation: {
            nextEl: '.featured-carousel .swiper-button-next',
            prevEl: '.featured-carousel .swiper-button-prev'
        },
        breakpoints: {
            640: { slidesPerView: 2 },
            768: { slidesPerView: 3 },
            1024: { slidesPerView: 4 }
        }
    });

    // Quiz cards carousel
    const quizSwiper = new Swiper('.quiz-carousel .swiper', {
        slidesPerView: 1,
        spaceBetween: 20,
        loop: true,
        pagination: {
            el: '.quiz-carousel .swiper-pagination',
            clickable: true
        },
        navigation: {
            nextEl: '.quiz-carousel .swiper-button-next',
            prevEl: '.quiz-carousel .swiper-button-prev'
        },
        breakpoints: {
            640: { slidesPerView: 2 },
            1024: { slidesPerView: 3 }
        }
    });
}

/**
 * Favorite toggle
 */
function initFavoriteToggle() {
    window.toggleFavorite = async function(objectId, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }

        // Check if user is authenticated
        const isAuthenticated = document.body.dataset.authenticated === 'true';
        if (!isAuthenticated) {
            window.showToast('Войдите в систему, чтобы добавить в избранное', 'warning');
            return;
        }

        const button = event?.target.closest('.btn-favorite');
        const icon = button?.querySelector('i');

        try {
            const response = await fetch('/Object/ToggleFavorite', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `objectId=${objectId}`
            });

            const data = await response.json();

            if (data.success) {
                if (data.isFavorite) {
                    button?.classList.add('active');
                    window.showToast('Добавлено в избранное', 'success');
                } else {
                    button?.classList.remove('active');
                    window.showToast('Удалено из избранного', 'info');
                }
            }
        } catch (error) {
            console.error('Error toggling favorite:', error);
            window.showToast('Произошла ошибка', 'danger');
        }
    };
}

/**
 * Search autocomplete
 */
function initSearchAutocomplete() {
    const searchInputs = document.querySelectorAll('input[name="q"]');

    searchInputs.forEach(input => {
        let timeout;

        input.addEventListener('input', (e) => {
            clearTimeout(timeout);
            const query = e.target.value.trim();

            if (query.length < 2) { hideAutocomplete(); return; }

            timeout = setTimeout(async () => {
                try {
                    const response = await fetch(`/Search/Autocomplete?q=${encodeURIComponent(query)}`);
                    const results = await response.json();
                    displayAutocompleteResults(input, results);
                } catch (error) {
                    console.error('Search error:', error);
                }
            }, 300);
        });

        input.addEventListener('blur', () => {
            setTimeout(() => {
                hideAutocomplete();
            }, 200);
        });
    });
}

function displayAutocompleteResults(input, results) {
    hideAutocomplete();

    if (!results.length) return;

    const container = document.createElement('div');
    container.className = 'autocomplete-dropdown';
    container.style.cssText = `
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: white;
        border: 1px solid #E0D8CC;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        z-index: 1000;
        max-height: 300px;
        overflow-y: auto;
    `;

    results.forEach(result => {
        const item = document.createElement('a');
        item.href = `/Object/Detail?slug=${result.slug}`;
        item.className = 'autocomplete-item';
        item.style.cssText = `
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 10px 16px;
            color: inherit;
            text-decoration: none;
            border-bottom: 1px solid #E0D8CC;
        `;
        item.innerHTML = `
            <span class="category-icon" style="background: ${getCategoryColor(result.iconClass)}; width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white;">
                <i class="${result.iconClass || 'bi bi-building'}"></i>
            </span>
            <div>
                <div style="font-weight: 500;">${result.name}</div>
                <div style="font-size: 0.85rem; color: #6B6B6B;">${result.category || ''}</div>
            </div>
        `;
        container.appendChild(item);
    });

    input.parentElement.style.position = 'relative';
    input.parentElement.appendChild(container);
}

function hideAutocomplete() {
    document.querySelectorAll('.autocomplete-dropdown').forEach(el => el.remove());
}

function getCategoryColor(iconClass) {
    const colors = {
        'icon-castle': '#8B1A2A',
        'icon-church': '#3B5E3F',
        'icon-estate': '#7A5C1E',
        'icon-cathedral': '#2A4A7F',
        'icon-monastery': '#5B3A7A',
        'icon-hillfort': '#4A6741',
        'icon-mosque': '#1A6B5E',
        'icon-synagogue': '#8B6B1A',
        'icon-manor': '#6B1A5B'
    };
    return colors[iconClass] || '#555555';
}

/**
 * Language switcher
 */
function initLanguageSwitcher() {
    document.querySelectorAll('.lang-switcher a, .lang-switcher-footer a').forEach(link => {
        link.addEventListener('click', (e) => {
            // Store preference
            const lang = link.getAttribute('href').split('=').pop();
            localStorage.setItem('preferredLang', lang);
        });
    });
}

/**
 * Notification system
 */
function showNotification(message, type = 'info') {
    // Backward-compat: keep old API but show Bootstrap toast
    const mapped = type === 'error' ? 'danger' : type;
    window.showToast(message, mapped);
}

function createNotificationContainer() {
    const container = document.createElement('div');
    container.id = 'notification-container';
    container.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        z-index: 9999;
        display: flex;
        flex-direction: column;
        gap: 10px;
    `;
    document.body.appendChild(container);
    return container;
}

/**
 * Star rating
 */
window.initStarRating = function(containerId, objectId, currentRating) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const stars = container.querySelectorAll('i');
    const ratingInput = container.querySelector('input[type="hidden"]');

    stars.forEach((star, index) => {
        star.addEventListener('click', async () => {
            const value = index + 1;
            ratingInput.value = value;
            updateStars(stars, value);

            try {
                const response = await fetch('/Object/SetRating', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `objectId=${objectId}&value=${value}`
                });

                const data = await response.json();
                if (data.success) {
                    showNotification('Ваша оценка учтена', 'success');
                }
            } catch (error) {
                console.error('Rating error:', error);
            }
        });

        star.addEventListener('mouseenter', () => {
            updateStars(stars, index + 1, true);
        });

        star.addEventListener('mouseleave', () => {
            updateStars(stars, currentRating);
        });
    });

    function updateStars(stars, value, hover = false) {
        stars.forEach((star, index) => {
            if (index < value) {
                star.className = 'bi bi-star-fill';
                if (!hover) star.style.color = '#C68B2A';
            } else {
                star.className = 'bi bi-star';
                if (!hover) star.style.color = '#E0D8CC';
            }
        });
    }
};

/**
 * Image gallery lightbox
 */
window.initLightgallery = function(galleryId) {
    if (typeof lightGallery !== 'undefined') {
        lightGallery(document.getElementById(galleryId), {
            selector: '.gallery-item',
            thumbnail: true,
            zoom: true
        });
    }
};

/**
 * Form validation
 */
function validateForm(formId) {
    const form = document.getElementById(formId);
    if (!form) return false;

    const inputs = form.querySelectorAll('input[required], textarea[required]');
    let isValid = true;

    inputs.forEach(input => {
        if (!input.value.trim()) {
            input.classList.add('is-invalid');
            isValid = false;
        } else {
            input.classList.remove('is-invalid');
            input.classList.add('is-valid');
        }
    });

    return isValid;
}

/**
 * Debounce utility
 */
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

/**
 * Format date
 */
function formatDate(date, locale = 'ru') {
    return new Date(date).toLocaleDateString(locale, {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

/**
 * Map helpers
 */
function initMap(elementId, markers) {
    if (typeof L === 'undefined') {
        console.error('Leaflet not loaded');
        return null;
    }

    const map = L.map(elementId, { attributionControl: false }).setView([53.9, 27.6], 7);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: ''
    }).addTo(map);

    if (markers && markers.length) {
        markers.forEach(marker => {
            const customIcon = L.divIcon({
                className: `custom-marker marker-${marker.categorySlug}`,
                html: `<div style="background: ${marker.colorHex || '#8B1A2A'}; width: 24px; height: 24px; border-radius: 50%; border: 2px solid white;"></div>`,
                iconSize: [24, 24],
                iconAnchor: [12, 12]
            });

            L.marker([marker.lat, marker.lng], { icon: customIcon })
                .addTo(map)
                .bindPopup(`
                    <div style="min-width: 200px;">
                        ${marker.imageUrl ? `<img src="${marker.imageUrl}" style="width: 100%; height: 120px; object-fit: cover; border-radius: 8px;">` : ''}
                        <h6 style="margin: 8px 0 4px;">${marker.name}</h6>
                        <p style="font-size: 0.85rem; color: #666; margin: 0;">${marker.shortDesc || ''}</p>
                        <a href="/Object/${marker.slug}" class="btn btn-sm btn-primary" style="margin-top: 8px;">Подробнее</a>
                    </div>
                `);
        });
    }

    return map;
}
