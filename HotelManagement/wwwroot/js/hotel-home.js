
document.addEventListener("DOMContentLoaded", function () {
    // Noi dung cac banner. Anh co the thay sau.
    const slides = [
        {
            image: "/images/hero-1.jpg",
            eyebrow: "WELCOME TO OUR HOTEL",
            title: "TRẢI NGHIỆM NGHỈ DƯỠNG ĐẲNG CẤP",
            description:
                "Không gian tinh tế, dịch vụ tận tâm, trải nghiệm khác biệt."
        },
        {
            image: "/images/hero-2.jpg",
            eyebrow: "ACCOMMODATION",
            title: "KHÔNG GIAN RIÊNG TƯ VÀ TINH TẾ",
            description:
                "Tận hưởng những căn phòng tiện nghi trong không gian thư thái."
        },
        {
            image: "/images/hero-3.jpg",
            eyebrow: "EXCLUSIVE EXPERIENCE",
            title: "TẬN HƯỞNG NHỮNG ĐIỀU ĐẶC BIỆT",
            description:
                "Khám phá dịch vụ và tiện ích dành cho kỳ nghỉ đáng nhớ."
        }
    ];

    const heroImage = document.getElementById("heroImage");
    const heroContent = document.getElementById("heroContent");
    const heroEyebrow = document.getElementById("heroEyebrow");
    const heroTitle = document.getElementById("heroTitle");
    const heroDescription = document.getElementById("heroDescription");
    const currentSlide = document.getElementById("currentSlide");
    const tabs = document.querySelectorAll(".hero-tab");

    let activeIndex = 0;
    let slideTimer;

    function showSlide(index) {
        activeIndex = (index + slides.length) % slides.length;
        const slide = slides[activeIndex];

        // Lam mo anh trong luc doi noi dung
        heroImage.classList.add("changing");

        window.setTimeout(function () {
            heroImage.style.backgroundImage =
                `url("${slide.image}")`;

            heroEyebrow.textContent = slide.eyebrow;
            heroTitle.textContent = slide.title;
            heroDescription.textContent = slide.description;
            currentSlide.textContent =
                String(activeIndex + 1).padStart(2, "0");

            tabs.forEach(function (tab, i) {
                tab.classList.toggle("active", i === activeIndex);
                tab.setAttribute(
                    "aria-pressed",
                    i === activeIndex ? "true" : "false"
                );
            });

            heroContent.classList.remove("animate-in");

            // Kich hoat lai animation cho cac dong chu
            void heroContent.offsetWidth;
            heroContent.classList.add("animate-in");
            heroImage.classList.remove("changing");
        }, 250);

        restartSlideTimer();
    }

    function restartSlideTimer() {
        window.clearInterval(slideTimer);
        slideTimer = window.setInterval(function () {
            showSlide(activeIndex + 1);
        }, 7000);
    }

    // Bam tung muc gioi thieu
    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            showSlide(Number(tab.dataset.slide));
        });
    });

    // Nut lui va tien
    document.getElementById("prevSlide").addEventListener("click", function () {
        showSlide(activeIndex - 1);
    });

    document.getElementById("nextSlide").addEventListener("click", function () {
        showSlide(activeIndex + 1);
    });

    // Dark Mode
    const themeToggle = document.getElementById("themeToggle");
    const themeIcon = document.getElementById("themeIcon");

    function setTheme(isDark) {
        document.body.classList.toggle("dark-mode", isDark);
        themeIcon.textContent = isDark ? "☀" : "☾";
        themeToggle.setAttribute(
            "aria-label",
            isDark ? "Chuyển sang giao diện sáng" : "Chuyển sang giao diện tối"
        );
    }

    // Khoi tao theo lua chon da luu tren trinh duyet
    const savedTheme = localStorage.getItem("hotel-theme");
    setTheme(savedTheme === "dark");

    themeToggle.addEventListener("click", function () {
        const isDark = !document.body.classList.contains("dark-mode");
        setTheme(isDark);
        localStorage.setItem("hotel-theme", isDark ? "dark" : "light");
    });

    // Menu tren dien thoai
    const mobileMenu = document.getElementById("mobileMenu");
    const navLinks = document.querySelector(".nav-links");

    mobileMenu.addEventListener("click", function () {
        const isOpen = navLinks.classList.toggle("open");
        mobileMenu.setAttribute("aria-expanded", String(isOpen));
    });

    navLinks.querySelectorAll("a").forEach(function (link) {
        link.addEventListener("click", function () {
            navLinks.classList.remove("open");
            mobileMenu.setAttribute("aria-expanded", "false");
        });
    });

    // Hieu ung khi cac phan noi dung cuon vao tam nhin
    const revealElements = document.querySelectorAll(".reveal");

    if ("IntersectionObserver" in window) {
        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("visible");
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.12
        });

        revealElements.forEach(function (element) {
            observer.observe(element);
        });
    } else {
        revealElements.forEach(function (element) {
            element.classList.add("visible");
        });
    }

    // Hien thi banner dau tien
    heroImage.style.backgroundImage =
        `url("${slides[0].image}")`;
    heroContent.classList.add("animate-in");
    tabs[0].setAttribute("aria-pressed", "true");

    restartSlideTimer();
});
