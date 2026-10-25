
document.addEventListener("DOMContentLoaded", function () {
    const modal = document.getElementById("loaiPhongModal");
    const content = document.getElementById("loaiPhongModalContent");
    const openButton = document.getElementById("openLoaiPhongModal");

    if (!modal || !content || !openButton) return;

    const popupUrl = "/LoaiPhong/Popup";

    async function loadLoaiPhong(url = popupUrl) {
        content.innerHTML = `
            <div class="type-loading">
                <i class="bi bi-arrow-repeat"></i>
                Đang tải danh sách loại phòng...
            </div>`;

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) {
                throw new Error("Không thể tải danh sách loại phòng.");
            }

            content.innerHTML = await response.text();
        } catch (error) {
            content.innerHTML = `
                <div class="type-load-error">
                    <i class="bi bi-exclamation-circle"></i>
                    Không tải được dữ liệu. Vui lòng thử lại.
                    <button type="button" id="retryLoaiPhong">
                        Thử lại
                    </button>
                </div>`;
        }
    }

    function openModal() {
        modal.classList.add("show");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("type-modal-open");
        loadLoaiPhong();
    }

    function closeModal() {
        modal.classList.remove("show");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("type-modal-open");
    }

    openButton.addEventListener("click", openModal);

    modal.querySelectorAll("[data-close-type-modal]")
        .forEach(button => button.addEventListener("click", closeModal));

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && modal.classList.contains("show")) {
            closeModal();
        }
    });

    content.addEventListener("click", function (event) {
        const retry = event.target.closest("#retryLoaiPhong");

        if (retry) loadLoaiPhong();

        const searchForm = event.target.closest("[data-type-search]");
        if (searchForm) {
            // Form search duoc xu ly trong su kien submit ben duoi.
        }
    });

    content.addEventListener("submit", async function (event) {
        const form = event.target.closest("[data-type-search]");
        if (!form) return;

        event.preventDefault();

        const params = new URLSearchParams(new FormData(form));
        await loadLoaiPhong(`${popupUrl}?${params.toString()}`);
    });
});
