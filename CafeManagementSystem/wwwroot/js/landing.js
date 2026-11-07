/* =========================================================================
   LAURA CAFÉ MANAGEMENT SYSTEM - LANDING PAGE JAVASCRIPT
   Handles: Smooth scroll, header elevation, and mobile navigation
   ========================================================================= */

document.addEventListener('DOMContentLoaded', function () {
  // 1. Header scroll effect
  const header = document.querySelector('.landing-header');
  if (header) {
    window.addEventListener('scroll', function () {
      if (window.scrollY > 30) {
        header.classList.add('scrolled');
      } else {
        header.classList.remove('scrolled');
      }
    });
  }

  // 2. Smooth scrolling for internal anchor links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      const targetId = this.getAttribute('href');
      if (targetId && targetId !== '#') {
        const targetElement = document.querySelector(targetId);
        if (targetElement) {
          e.preventDefault();
          targetElement.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
          });

          // Close mobile offcanvas if open
          const mobileMenu = document.getElementById('landingMobileNav');
          if (mobileMenu && typeof bootstrap !== 'undefined') {
            const bsOffcanvas = bootstrap.Offcanvas.getInstance(mobileMenu);
            if (bsOffcanvas) {
              bsOffcanvas.hide();
            }
          }
        }
      }
    });
  });

  // 3. Category Filter Sync (Database Categories)
  const filterButtons = document.querySelectorAll('.landing-cat-pill');
  const drinkCards = document.querySelectorAll('.landing-drink-card');

  filterButtons.forEach(btn => {
    btn.addEventListener('click', function () {
      const filter = this.getAttribute('data-filter');

      // Update active button state
      filterButtons.forEach(b => b.classList.remove('active'));
      this.classList.add('active');

      // Filter product cards smoothly
      drinkCards.forEach(card => {
        if (filter === 'all') {
          card.classList.remove('d-none');
        } else if (filter === 'featured') {
          if (card.getAttribute('data-featured') === 'true') {
            card.classList.remove('d-none');
          } else {
            card.classList.add('d-none');
          }
        } else {
          // filter by cat-id
          if (card.getAttribute('data-cat') === filter) {
            card.classList.remove('d-none');
          } else {
            card.classList.add('d-none');
          }
        }
      });
    });
  });
});
